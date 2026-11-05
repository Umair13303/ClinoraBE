using ClinoraBE.Infrastructure;
using ClinoraBE.Model.DAL;
using ClinoraBE.Model.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinoraBE.Controllers
{
    [ApiController]
    [Route("api/samples")]
    [Authorize]
    [StationAuthorize("Phlebotomy")]
    public class SamplesController : ControllerBase
    {
        private readonly ClinoraContext _db;

        public SamplesController(ClinoraContext db)
        {
            _db = db;
        }

        [HttpPost("collect")]
        public async Task<ActionResult<SampleBarcodeDto>> Collect([FromBody] CollectSampleRequest request)
        {
            if (request.VisitServiceIds == null || request.VisitServiceIds.Count == 0)
            {
                return BadRequest(new { message = "Select at least one test to collect." });
            }

            var visit = await _db.Visit.FirstOrDefaultAsync(v => v.Id == request.VisitId);
            if (visit == null)
            {
                return BadRequest(new { message = "Visit not found." });
            }

            var branchId = BranchContext.ResolveBranchId(Request, User);
            if (!BranchContext.Matches(visit.BranchId, branchId))
            {
                return BadRequest(new { message = "Visit not found for this branch." });
            }

            if (visit.VisitStatus == VisitStatuses.Cancelled)
            {
                return BadRequest(new { message = "Cannot collect samples for a cancelled visit." });
            }

            var lines = await _db.VisitService
                .Where(vs => vs.VisitId == request.VisitId && request.VisitServiceIds.Contains(vs.Id))
                .ToListAsync();

            if (lines.Count != request.VisitServiceIds.Count)
            {
                return BadRequest(new { message = "One or more visit services were not found on this visit." });
            }

            if (lines.Any(l => l.Status != VisitServiceStatuses.AwaitingCollection))
            {
                return BadRequest(new { message = "Only awaiting-collection tests can be collected." });
            }

            if (request.IsSendOut && request.OutsideLabId is null)
            {
                return BadRequest(new { message = "Choose an outside lab for send-out samples." });
            }

            var barcode = $"S-{visit.VisitNumber.Replace("V-", "")}-{DateTime.Now:HHmmss}-{Random.Shared.Next(10, 99)}";
            var sample = new LabSample
            {
                VisitId = request.VisitId,
                SampleGroupId = request.SampleGroupId,
                SampleTypeId = request.SampleTypeId,
                ContainerId = request.ContainerId,
                BottleColorId = request.BottleColorId,
                Barcode = barcode,
                PhlebotomistName = string.IsNullOrWhiteSpace(request.PhlebotomistName) ? null : request.PhlebotomistName.Trim(),
                CollectedAt = DateTime.Now,
                IsSendOut = request.IsSendOut,
                OutsideLabId = request.IsSendOut ? request.OutsideLabId : null
            };

            _db.LabSample.Add(sample);
            await _db.SaveChangesAsync();

            foreach (var line in lines)
            {
                line.SampleId = sample.Id;
                line.IsSendOut = request.IsSendOut;
                line.OutsideLabId = request.IsSendOut ? request.OutsideLabId : null;
                line.Status = request.IsSendOut ? VisitServiceStatuses.SentOut : VisitServiceStatuses.InLab;
                _db.SampleTest.Add(new SampleTest { SampleId = sample.Id, VisitServiceId = line.Id });
            }

            await _db.SaveChangesAsync();
            return Ok(await BuildBarcodeDto(sample.Id));
        }

        [HttpPost("{id:int}/reject")]
        public async Task<ActionResult> Reject(int id, [FromBody] RejectSampleRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Reason))
            {
                return BadRequest(new { message = "Enter a reject reason." });
            }

            var sample = await _db.LabSample.FirstOrDefaultAsync(s => s.Id == id);
            if (sample == null)
            {
                return NotFound(new { message = "Sample not found." });
            }

            if (sample.SampleStatus == "Rejected")
            {
                return BadRequest(new { message = "Sample already rejected." });
            }

            var visit = await _db.Visit.AsNoTracking().FirstOrDefaultAsync(v => v.Id == sample.VisitId);
            var branchId = BranchContext.ResolveBranchId(Request, User);
            if (visit == null || !BranchContext.Matches(visit.BranchId, branchId))
            {
                return NotFound(new { message = "Sample not found for this branch." });
            }

            if (await _db.OutsideLabShipmentItem.AsNoTracking().AnyAsync(i => i.SampleId == id))
            {
                return BadRequest(new { message = "Cannot reject a sample that is already on a shipment." });
            }

            var links = await _db.SampleTest.Where(st => st.SampleId == id).ToListAsync();
            var lineIds = links.Select(l => l.VisitServiceId).ToList();
            var lines = await _db.VisitService.Where(vs => lineIds.Contains(vs.Id)).ToListAsync();
            if (lines.Any(l => l.Status is VisitServiceStatuses.Finalized or VisitServiceStatuses.ResultEntered or VisitServiceStatuses.SentOut))
            {
                return BadRequest(new { message = "Cannot reject after result entry, send-out, or finalize." });
            }

            sample.SampleStatus = "Rejected";
            sample.RejectReason = request.Reason.Trim();
            sample.RejectedAt = DateTime.Now;
            sample.RejectedByName = string.IsNullOrWhiteSpace(request.RejectedByName) ? null : request.RejectedByName.Trim();

            foreach (var line in lines)
            {
                line.Status = VisitServiceStatuses.Rejected;
                line.RejectReason = sample.RejectReason;
                line.RejectedAt = sample.RejectedAt;
                line.RejectedByName = sample.RejectedByName;
                line.SampleId = null;

                if (request.Redraw)
                {
                    _db.VisitService.Add(new VisitService
                    {
                        VisitId = line.VisitId,
                        ServiceId = line.ServiceId,
                        Fee = line.Fee,
                        Discount = line.Discount,
                        Urgent = line.Urgent,
                        StaffId = line.StaffId,
                        StaffName = line.StaffName,
                        ReadyBy = line.ReadyBy,
                        Status = VisitServiceStatuses.AwaitingCollection,
                        IsSendOut = false
                    });
                }
            }

            await _db.SaveChangesAsync();
            return Ok(new { message = request.Redraw ? "Sample rejected. Redraw queued." : "Sample rejected." });
        }

        [HttpGet("{id:int}/barcode")]
        public async Task<ActionResult<SampleBarcodeDto>> Barcode(int id)
        {
            var dto = await BuildBarcodeDto(id);
            if (dto == null)
            {
                return NotFound(new { message = "Sample not found." });
            }

            return Ok(dto);
        }

        [HttpGet("recent")]
        public async Task<ActionResult<List<SampleBarcodeDto>>> Recent()
        {
            var branchId = BranchContext.ResolveBranchId(Request, User);
            var visitIds = await BranchContext.VisitIdsForBranchAsync(_db, branchId);

            var ids = await _db.LabSample.AsNoTracking()
                .Where(s => visitIds.Contains(s.VisitId) && s.SampleStatus != "Rejected")
                .OrderByDescending(s => s.CollectedAt)
                .Take(30)
                .Select(s => s.Id)
                .ToListAsync();

            var list = new List<SampleBarcodeDto>();
            foreach (var id in ids)
            {
                var dto = await BuildBarcodeDto(id);
                if (dto != null)
                {
                    list.Add(dto);
                }
            }

            return Ok(list);
        }

        [HttpGet("send-out-pending")]
        public async Task<ActionResult<List<SendOutSampleDto>>> SendOutPending()
        {
            var branchId = BranchContext.ResolveBranchId(Request, User);
            var visitIds = await BranchContext.VisitIdsForBranchAsync(_db, branchId);

            var shippedSampleIds = await _db.OutsideLabShipmentItem.AsNoTracking()
                .Select(i => i.SampleId)
                .Distinct()
                .ToListAsync();

            var samples = await (
                from s in _db.LabSample.AsNoTracking()
                join v in _db.Visit.AsNoTracking() on s.VisitId equals v.Id
                join p in _db.Patient.AsNoTracking() on v.PatientId equals p.Id
                join ol in _db.OutsideLab.AsNoTracking() on s.OutsideLabId equals ol.Id into olj
                from ol in olj.DefaultIfEmpty()
                where s.IsSendOut
                      && s.SampleStatus != "Rejected"
                      && !shippedSampleIds.Contains(s.Id)
                      && visitIds.Contains(v.Id)
                orderby s.CollectedAt descending
                select new { s, v, p, OutsideLabName = ol != null ? ol.Name : null }
            ).Take(50).ToListAsync();

            var result = new List<SendOutSampleDto>();
            foreach (var row in samples)
            {
                var tests = await (
                    from st in _db.SampleTest.AsNoTracking()
                    join vs in _db.VisitService.AsNoTracking() on st.VisitServiceId equals vs.Id
                    join svc in _db.Service.AsNoTracking() on vs.ServiceId equals svc.Id
                    where st.SampleId == row.s.Id
                    select svc.Name
                ).ToListAsync();

                result.Add(new SendOutSampleDto
                {
                    SampleId = row.s.Id,
                    Barcode = row.s.Barcode,
                    VisitNumber = row.v.VisitNumber,
                    PatientName = row.p.FullName,
                    OutsideLabId = row.s.OutsideLabId,
                    OutsideLabName = row.OutsideLabName,
                    Tests = tests
                });
            }

            return Ok(result);
        }

        private async Task<SampleBarcodeDto?> BuildBarcodeDto(int sampleId)
        {
            var row = await (
                from s in _db.LabSample.AsNoTracking()
                join v in _db.Visit.AsNoTracking() on s.VisitId equals v.Id
                join p in _db.Patient.AsNoTracking() on v.PatientId equals p.Id
                join st in _db.SampleType.AsNoTracking() on s.SampleTypeId equals st.Id
                join c in _db.Container.AsNoTracking() on s.ContainerId equals c.Id
                join bc in _db.BottleColor.AsNoTracking() on s.BottleColorId equals bc.Id
                join g in _db.SampleGroup.AsNoTracking() on s.SampleGroupId equals g.Id into gj
                from g in gj.DefaultIfEmpty()
                join ol in _db.OutsideLab.AsNoTracking() on s.OutsideLabId equals ol.Id into olj
                from ol in olj.DefaultIfEmpty()
                where s.Id == sampleId
                select new
                {
                    Sample = s,
                    v.VisitNumber,
                    PatientName = p.FullName,
                    SampleTypeName = st.Name,
                    ContainerName = c.Name,
                    BottleColorName = bc.Name,
                    SampleGroupName = g != null ? g.Name : null,
                    OutsideLabName = ol != null ? ol.Name : null
                }
            ).FirstOrDefaultAsync();

            if (row == null)
            {
                return null;
            }

            var tests = await (
                from st in _db.SampleTest.AsNoTracking()
                join vs in _db.VisitService.AsNoTracking() on st.VisitServiceId equals vs.Id
                join svc in _db.Service.AsNoTracking() on vs.ServiceId equals svc.Id
                where st.SampleId == sampleId
                select svc.Name
            ).ToListAsync();

            return new SampleBarcodeDto
            {
                SampleId = row.Sample.Id,
                Barcode = row.Sample.Barcode,
                VisitNumber = row.VisitNumber,
                PatientName = row.PatientName,
                SampleTypeName = row.SampleTypeName,
                ContainerName = row.ContainerName,
                BottleColorName = row.BottleColorName,
                SampleGroupName = row.SampleGroupName,
                IsSendOut = row.Sample.IsSendOut,
                OutsideLabName = row.OutsideLabName,
                CollectedAt = row.Sample.CollectedAt,
                Tests = tests
            };
        }
    }
}
