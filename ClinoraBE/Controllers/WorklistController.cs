using ClinoraBE.Model.DAL;
using ClinoraBE.Model.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinoraBE.Controllers
{
    [ApiController]
    [Route("api/worklist")]
    [Authorize]
    public class WorklistController : ControllerBase
    {
        private readonly ClinoraContext _db;

        public WorklistController(ClinoraContext db)
        {
            _db = db;
        }

        [HttpGet("collection")]
        public async Task<ActionResult<List<CollectionGroupDto>>> Collection([FromQuery] int? visitId)
        {
            var query =
                from vs in _db.VisitService.AsNoTracking()
                join v in _db.Visit.AsNoTracking() on vs.VisitId equals v.Id
                join p in _db.Patient.AsNoTracking() on v.PatientId equals p.Id
                join s in _db.Service.AsNoTracking() on vs.ServiceId equals s.Id
                join g in _db.SampleGroup.AsNoTracking() on s.SampleGroupId equals g.Id into gj
                from g in gj.DefaultIfEmpty()
                join st in _db.SampleType.AsNoTracking() on s.SampleTypeId equals st.Id into stj
                from st in stj.DefaultIfEmpty()
                join c in _db.Container.AsNoTracking() on s.ContainerId equals c.Id into cj
                from c in cj.DefaultIfEmpty()
                join bc in _db.BottleColor.AsNoTracking() on s.BottleColorId equals bc.Id into bcj
                from bc in bcj.DefaultIfEmpty()
                where vs.Status == VisitServiceStatuses.AwaitingCollection
                      && s.Pathway == ServicePathways.Laboratory
                select new CollectionLineDto
                {
                    VisitServiceId = vs.Id,
                    VisitId = v.Id,
                    VisitNumber = v.VisitNumber,
                    PatientName = p.FullName,
                    Phone = p.Phone,
                    ServiceId = s.Id,
                    ServiceName = s.Name,
                    Urgent = vs.Urgent,
                    Status = vs.Status,
                    SampleGroupId = s.SampleGroupId,
                    SampleGroupCode = g != null ? g.Code : null,
                    SampleGroupName = g != null ? g.Name : null,
                    SampleTypeId = s.SampleTypeId,
                    SampleTypeName = st != null ? st.Name : null,
                    ContainerId = s.ContainerId,
                    ContainerName = c != null ? c.Name : null,
                    BottleColorId = s.BottleColorId,
                    BottleColorName = bc != null ? bc.Name : null,
                    CanSendOut = s.CanSendOut,
                    DefaultOutsideLabId = s.DefaultOutsideLabId
                };

            if (visitId is int vid)
            {
                query = query.Where(x => x.VisitId == vid);
            }

            var lines = await query.OrderByDescending(x => x.VisitId).ThenBy(x => x.SampleGroupName).ThenBy(x => x.ServiceName).ToListAsync();

            var groups = lines
                .GroupBy(l => new { l.VisitId, l.VisitNumber, l.PatientName, l.SampleGroupId, Key = l.SampleGroupId?.ToString() ?? $"solo-{l.VisitServiceId}" })
                .Select(g =>
                {
                    var first = g.First();
                    return new CollectionGroupDto
                    {
                        VisitId = g.Key.VisitId,
                        VisitNumber = g.Key.VisitNumber,
                        PatientName = g.Key.PatientName,
                        SampleGroupId = g.Key.SampleGroupId,
                        GroupKey = $"{g.Key.VisitId}:{g.Key.Key}",
                        GroupLabel = first.SampleGroupName ?? first.ServiceName,
                        SampleTypeName = first.SampleTypeName,
                        ContainerName = first.ContainerName,
                        BottleColorName = first.BottleColorName,
                        CanSendOut = g.Any(x => x.CanSendOut),
                        Lines = g.ToList()
                    };
                })
                .ToList();

            return Ok(groups);
        }

        [HttpGet("results")]
        public async Task<ActionResult<List<ResultWorkItemDto>>> Results()
        {
            var items = await (
                from vs in _db.VisitService.AsNoTracking()
                join v in _db.Visit.AsNoTracking() on vs.VisitId equals v.Id
                join p in _db.Patient.AsNoTracking() on v.PatientId equals p.Id
                join s in _db.Service.AsNoTracking() on vs.ServiceId equals s.Id
                join sample in _db.LabSample.AsNoTracking() on vs.SampleId equals sample.Id into sj
                from sample in sj.DefaultIfEmpty()
                join tr in _db.TestResult.AsNoTracking() on vs.Id equals tr.VisitServiceId into trj
                from tr in trj.DefaultIfEmpty()
                where vs.Status == VisitServiceStatuses.InLab
                orderby vs.Urgent descending, v.CreatedAt
                select new ResultWorkItemDto
                {
                    VisitServiceId = vs.Id,
                    VisitId = v.Id,
                    VisitNumber = v.VisitNumber,
                    PatientName = p.FullName,
                    ServiceName = s.Name,
                    Status = vs.Status,
                    Urgent = vs.Urgent,
                    Barcode = sample != null ? sample.Barcode : null,
                    IsSendOut = vs.IsSendOut,
                    ExistingResultText = tr != null ? tr.ResultText : null
                }
            ).Take(100).ToListAsync();

            // Also include SentOut that were received into InLab via receive endpoint.
            // Items awaiting entry after outside receive are set to InLab.
            return Ok(items);
        }

        [HttpGet("finalize")]
        public async Task<ActionResult<List<ResultWorkItemDto>>> FinalizeQueue()
        {
            var items = await (
                from vs in _db.VisitService.AsNoTracking()
                join v in _db.Visit.AsNoTracking() on vs.VisitId equals v.Id
                join p in _db.Patient.AsNoTracking() on v.PatientId equals p.Id
                join s in _db.Service.AsNoTracking() on vs.ServiceId equals s.Id
                join tr in _db.TestResult.AsNoTracking() on vs.Id equals tr.VisitServiceId into trj
                from tr in trj.DefaultIfEmpty()
                where vs.Status == VisitServiceStatuses.ResultEntered
                      || vs.Status == VisitServiceStatuses.Imaged
                orderby v.CreatedAt
                select new ResultWorkItemDto
                {
                    VisitServiceId = vs.Id,
                    VisitId = v.Id,
                    VisitNumber = v.VisitNumber,
                    PatientName = p.FullName,
                    ServiceName = s.Name,
                    Status = vs.Status,
                    Urgent = vs.Urgent,
                    IsSendOut = vs.IsSendOut,
                    ExistingResultText = tr != null ? tr.ResultText : null
                }
            ).Take(100).ToListAsync();

            return Ok(items);
        }

        [HttpGet("radiology")]
        public async Task<ActionResult<List<RadiologyWorkItemDto>>> Radiology()
        {
            var items = await (
                from vs in _db.VisitService.AsNoTracking()
                join v in _db.Visit.AsNoTracking() on vs.VisitId equals v.Id
                join p in _db.Patient.AsNoTracking() on v.PatientId equals p.Id
                join s in _db.Service.AsNoTracking() on vs.ServiceId equals s.Id
                where s.Pathway == ServicePathways.Radiology
                      && (vs.Status == VisitServiceStatuses.AwaitingImaging || vs.Status == VisitServiceStatuses.Imaged)
                orderby vs.Urgent descending, v.CreatedAt
                select new RadiologyWorkItemDto
                {
                    VisitServiceId = vs.Id,
                    VisitId = v.Id,
                    VisitNumber = v.VisitNumber,
                    PatientName = p.FullName,
                    ServiceName = s.Name,
                    Status = vs.Status,
                    Urgent = vs.Urgent,
                    ReadyBy = vs.ReadyBy
                }
            ).Take(100).ToListAsync();

            return Ok(items);
        }
    }
}
