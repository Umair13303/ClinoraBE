using ClinoraBE.Infrastructure;
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
        [StationAuthorize("Phlebotomy")]
        public async Task<ActionResult<List<CollectionGroupDto>>> Collection([FromQuery] int? visitId)
        {
            var branchId = BranchContext.ResolveBranchId(Request, User);
            var visitIds = await BranchContext.VisitIdsForBranchAsync(_db, branchId);

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
                      && v.VisitStatus == VisitStatuses.Active
                      && visitIds.Contains(v.Id)
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
        [StationAuthorize("Lab")]
        public async Task<ActionResult<List<ResultWorkItemDto>>> Results()
        {
            var branchId = BranchContext.ResolveBranchId(Request, User);
            var visitIds = await BranchContext.VisitIdsForBranchAsync(_db, branchId);

            var items = await (
                from vs in _db.VisitService.AsNoTracking()
                join v in _db.Visit.AsNoTracking() on vs.VisitId equals v.Id
                join p in _db.Patient.AsNoTracking() on v.PatientId equals p.Id
                join s in _db.Service.AsNoTracking() on vs.ServiceId equals s.Id
                join sample in _db.LabSample.AsNoTracking() on vs.SampleId equals sample.Id into sj
                from sample in sj.DefaultIfEmpty()
                where (vs.Status == VisitServiceStatuses.InLab || vs.Status == VisitServiceStatuses.ResultEntered)
                      && visitIds.Contains(v.Id)
                      && v.VisitStatus == VisitStatuses.Active
                orderby vs.Urgent descending, v.CreatedAt
                select new ResultWorkItemDto
                {
                    VisitServiceId = vs.Id,
                    VisitId = v.Id,
                    ServiceId = s.Id,
                    VisitNumber = v.VisitNumber,
                    PatientName = p.FullName,
                    ServiceName = s.Name,
                    Status = vs.Status,
                    ReportProfile = string.IsNullOrWhiteSpace(s.ReportProfile) ? ReportProfiles.Standard : s.ReportProfile,
                    Urgent = vs.Urgent,
                    Barcode = sample != null ? sample.Barcode : null,
                    IsSendOut = vs.IsSendOut,
                    IsAbnormal = vs.IsAbnormal,
                    IsCritical = vs.IsCritical
                }
            ).Take(100).ToListAsync();

            await AttachExistingResultsAsync(items);
            return Ok(items);
        }

        [HttpGet("finalize")]
        [StationAuthorize("Doctor")]
        public async Task<ActionResult<List<ResultWorkItemDto>>> FinalizeQueue()
        {
            var branchId = BranchContext.ResolveBranchId(Request, User);
            var visitIds = await BranchContext.VisitIdsForBranchAsync(_db, branchId);

            var items = await (
                from vs in _db.VisitService.AsNoTracking()
                join v in _db.Visit.AsNoTracking() on vs.VisitId equals v.Id
                join p in _db.Patient.AsNoTracking() on v.PatientId equals p.Id
                join s in _db.Service.AsNoTracking() on vs.ServiceId equals s.Id
                where (vs.Status == VisitServiceStatuses.ResultEntered
                      || vs.Status == VisitServiceStatuses.Imaged
                      || vs.Status == VisitServiceStatuses.Finalized)
                      && visitIds.Contains(v.Id)
                      && v.VisitStatus == VisitStatuses.Active
                orderby v.CreatedAt descending
                select new ResultWorkItemDto
                {
                    VisitServiceId = vs.Id,
                    VisitId = v.Id,
                    ServiceId = s.Id,
                    VisitNumber = v.VisitNumber,
                    PatientName = p.FullName,
                    ServiceName = s.Name,
                    Status = vs.Status,
                    ReportProfile = string.IsNullOrWhiteSpace(s.ReportProfile) ? ReportProfiles.Standard : s.ReportProfile,
                    Urgent = vs.Urgent,
                    IsSendOut = vs.IsSendOut,
                    IsAbnormal = vs.IsAbnormal,
                    IsCritical = vs.IsCritical
                }
            ).Take(150).ToListAsync();

            await AttachExistingResultsAsync(items);
            return Ok(items);
        }

        private async Task AttachExistingResultsAsync(List<ResultWorkItemDto> items)
        {
            if (items.Count == 0)
            {
                return;
            }

            var vsIds = items.Select(i => i.VisitServiceId).ToList();
            var results = await _db.TestResult.AsNoTracking()
                .Where(r => vsIds.Contains(r.VisitServiceId) && r.IsCurrent)
                .OrderBy(r => r.Id)
                .ToListAsync();
            var byVs = results.GroupBy(r => r.VisitServiceId).ToDictionary(g => g.Key, g => g.ToList());

            foreach (var item in items)
            {
                if (!byVs.TryGetValue(item.VisitServiceId, out var list) || list.Count == 0)
                {
                    continue;
                }

                item.ExistingResults = list.Select(r => new ExistingResultDto
                {
                    ParameterName = r.ParameterName,
                    ResultText = r.ResultText,
                    Units = r.Units,
                    ReferenceRange = r.ReferenceRange,
                    IsAbnormal = r.IsAbnormal,
                    IsCritical = r.IsCritical
                }).ToList();

                var primary = list[0];
                item.ExistingResultText = primary.ResultText;
                item.ExistingUnits = primary.Units;
                item.ExistingReferenceRange = primary.ReferenceRange;
                item.ExistingParameterName = primary.ParameterName;
            }
        }

        [HttpGet("summary")]
        public async Task<ActionResult<WorklistSummaryDto>> Summary()
        {
            var branchId = BranchContext.ResolveBranchId(Request, User);
            var visitIds = await BranchContext.VisitIdsForBranchAsync(_db, branchId);
            var activeVisitIds = await _db.Visit.AsNoTracking()
                .Where(v => visitIds.Contains(v.Id) && v.VisitStatus == VisitStatuses.Active)
                .Select(v => v.Id)
                .ToListAsync();

            var lines = _db.VisitService.AsNoTracking().Where(vs => activeVisitIds.Contains(vs.VisitId));

            var overdueQuery =
                from sh in _db.OutsideLabShipment.AsNoTracking()
                join item in _db.OutsideLabShipmentItem.AsNoTracking() on sh.Id equals item.ShipmentId
                join vs in _db.VisitService.AsNoTracking() on item.VisitServiceId equals vs.Id
                where sh.Status == "Sent"
                      && sh.ExpectedAt != null
                      && sh.ExpectedAt < DateTime.Now
                      && activeVisitIds.Contains(vs.VisitId)
                select sh.Id;

            var overdue = await overdueQuery.Distinct().CountAsync();

            return Ok(new WorklistSummaryDto
            {
                AwaitingCollection = await lines.CountAsync(vs => vs.Status == VisitServiceStatuses.AwaitingCollection),
                InLab = await lines.CountAsync(vs => vs.Status == VisitServiceStatuses.InLab),
                SentOut = await lines.CountAsync(vs => vs.Status == VisitServiceStatuses.SentOut),
                AwaitingFinalize = await lines.CountAsync(vs =>
                    vs.Status == VisitServiceStatuses.ResultEntered || vs.Status == VisitServiceStatuses.Imaged),
                Radiology = await lines.CountAsync(vs =>
                    vs.Status == VisitServiceStatuses.AwaitingImaging || vs.Status == VisitServiceStatuses.Imaged),
                OutsideOverdue = overdue
            });
        }

        [HttpGet("radiology")]
        [StationAuthorize("Radiology")]
        public async Task<ActionResult<List<RadiologyWorkItemDto>>> Radiology()
        {
            var branchId = BranchContext.ResolveBranchId(Request, User);
            var visitIds = await BranchContext.VisitIdsForBranchAsync(_db, branchId);

            var items = await (
                from vs in _db.VisitService.AsNoTracking()
                join v in _db.Visit.AsNoTracking() on vs.VisitId equals v.Id
                join p in _db.Patient.AsNoTracking() on v.PatientId equals p.Id
                join s in _db.Service.AsNoTracking() on vs.ServiceId equals s.Id
                join tr in _db.TestResult.AsNoTracking().Where(r => r.IsCurrent) on vs.Id equals tr.VisitServiceId into trj
                from tr in trj.DefaultIfEmpty()
                where s.Pathway == ServicePathways.Radiology
                      && (vs.Status == VisitServiceStatuses.AwaitingImaging || vs.Status == VisitServiceStatuses.Imaged)
                      && visitIds.Contains(v.Id)
                      && v.VisitStatus == VisitStatuses.Active
                orderby vs.Urgent descending, v.CreatedAt
                select new RadiologyWorkItemDto
                {
                    VisitServiceId = vs.Id,
                    VisitId = v.Id,
                    ServiceId = s.Id,
                    VisitNumber = v.VisitNumber,
                    PatientName = p.FullName,
                    ServiceName = s.Name,
                    Status = vs.Status,
                    Urgent = vs.Urgent,
                    ReadyBy = vs.ReadyBy,
                    ExistingFindings = tr != null ? tr.ResultText : null,
                    TemplateName = tr != null ? tr.ParameterName : null
                }
            ).Take(100).ToListAsync();

            return Ok(items);
        }
    }
}
