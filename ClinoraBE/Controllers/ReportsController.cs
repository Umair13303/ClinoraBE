using ClinoraBE.Infrastructure;
using ClinoraBE.Model.DAL;
using ClinoraBE.Model.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinoraBE.Controllers
{
    [ApiController]
    [Route("api/reports")]
    [Authorize]
    [StationAuthorize("FrontDesk", "Doctor", "Radiology")]
    public class ReportsController : ControllerBase
    {
        private readonly ClinoraContext _db;

        public ReportsController(ClinoraContext db)
        {
            _db = db;
        }

        [HttpPost("finalize")]
        [StationAuthorize("Doctor")]
        public async Task<ActionResult<ReportDto>> Finalize([FromBody] FinalizeRequest request)
        {
            if (request.VisitServiceIds == null || request.VisitServiceIds.Count == 0)
            {
                return BadRequest(new { message = "Select at least one test to finalize." });
            }

            var branchId = BranchContext.ResolveBranchId(Request, User);
            if (!await BranchContext.VisitAllowedAsync(_db, request.VisitId, branchId))
            {
                return NotFound(new { message = "Visit not found for this branch." });
            }

            var lines = await _db.VisitService
                .Where(vs => vs.VisitId == request.VisitId && request.VisitServiceIds.Contains(vs.Id))
                .ToListAsync();

            if (lines.Count != request.VisitServiceIds.Count)
            {
                return BadRequest(new { message = "One or more lines were not found on this visit." });
            }

            // Skip VS that still have live report lines (radiologist-signed / not yet amended).
            // Amend deletes ReportLines so lab ResultEntered becomes eligible again.
            var requestedIds = lines.Select(l => l.Id).ToList();
            var alreadyReportedIds = await _db.ReportLine.AsNoTracking()
                .Where(rl => requestedIds.Contains(rl.VisitServiceId))
                .Select(rl => rl.VisitServiceId)
                .Distinct()
                .ToListAsync();
            var serviceIds = lines.Select(l => l.ServiceId).Distinct().ToList();
            var services = await _db.Service.AsNoTracking()
                .Where(s => serviceIds.Contains(s.Id))
                .ToDictionaryAsync(s => s.Id);

            // Doctor finalize is lab-only; radiology stays on radiologist sign.
            lines = lines
                .Where(l =>
                    l.Status == VisitServiceStatuses.ResultEntered
                    && !alreadyReportedIds.Contains(l.Id)
                    && services.TryGetValue(l.ServiceId, out var svc)
                    && svc.Pathway != ServicePathways.Radiology
                    && svc.Pathway != ServicePathways.Vaccination
                    && svc.Pathway != ServicePathways.Physical)
                .ToList();

            if (lines.Count == 0)
            {
                return BadRequest(new { message = "Nothing left to finalize — selected tests are already reported, radiology, vaccination, physical, or not ready." });
            }

            var byName = string.IsNullOrWhiteSpace(request.FinalizedByName) ? "Doctor" : request.FinalizedByName.Trim();
            var now = DateTime.Now;
            foreach (var line in lines)
            {
                line.Status = VisitServiceStatuses.Finalized;
                line.FinalizedAt = now;
                line.FinalizedByName = byName;
            }

            var pathways = lines
                .Select(l => services.TryGetValue(l.ServiceId, out var svc) ? svc.Pathway : ServicePathways.Laboratory)
                .Distinct()
                .ToList();
            var pathway = pathways.Count == 1
                ? pathways[0]
                : (string.IsNullOrWhiteSpace(request.Pathway) ? ServicePathways.Laboratory : request.Pathway.Trim());

            var report = new Report
            {
                VisitId = request.VisitId,
                Pathway = pathway,
                FinalizedByName = byName,
                FinalizedAt = now,
                PrintCount = 0
            };
            _db.Report.Add(report);
            await _db.SaveChangesAsync();

            var lineIds = lines.Select(l => l.Id).ToList();
            var results = await _db.TestResult.AsNoTracking()
                .Where(r => lineIds.Contains(r.VisitServiceId) && r.IsCurrent)
                .OrderBy(r => r.Id)
                .ToListAsync();
            var resultsByVs = results.GroupBy(r => r.VisitServiceId).ToDictionary(g => g.Key, g => g.ToList());

            foreach (var line in lines)
            {
                services.TryGetValue(line.ServiceId, out var service);
                var profile = string.IsNullOrWhiteSpace(service?.ReportProfile)
                    ? ReportProfiles.Standard
                    : service!.ReportProfile;
                var serviceName = service?.Name ?? ("Service " + line.ServiceId);
                resultsByVs.TryGetValue(line.Id, out var lineResults);
                lineResults ??= [];

                if (lineResults.Count == 0)
                {
                    _db.ReportLine.Add(new ReportLine
                    {
                        ReportId = report.Id,
                        VisitServiceId = line.Id,
                        ServiceName = serviceName,
                        ReportProfile = profile,
                        Urgent = line.Urgent,
                        IsAbnormal = line.IsAbnormal,
                        IsCritical = line.IsCritical
                    });
                    continue;
                }

                foreach (var tr in lineResults)
                {
                    _db.ReportLine.Add(new ReportLine
                    {
                        ReportId = report.Id,
                        VisitServiceId = line.Id,
                        ServiceName = serviceName,
                        ParameterName = tr.ParameterName,
                        ResultText = tr.ResultText,
                        Units = tr.Units,
                        ReferenceRange = tr.ReferenceRange,
                        ReportProfile = profile,
                        Urgent = line.Urgent,
                        IsAbnormal = tr.IsAbnormal,
                        IsCritical = tr.IsCritical
                    });
                }
            }

            await _db.SaveChangesAsync();
            return Ok(await BuildReportDto(report.Id));
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<ReportDto>> Get(int id)
        {
            var dto = await BuildReportDto(id);
            if (dto == null)
            {
                return NotFound(new { message = "Report not found." });
            }

            var branchId = BranchContext.ResolveBranchId(Request, User);
            if (!await BranchContext.VisitAllowedAsync(_db, dto.VisitId, branchId))
            {
                return NotFound(new { message = "Report not found for this branch." });
            }

            return Ok(dto);
        }

        [HttpGet("by-visit/{visitId:int}")]
        public async Task<ActionResult<List<ReportDto>>> ByVisit(int visitId)
        {
            var branchId = BranchContext.ResolveBranchId(Request, User);
            if (!await BranchContext.VisitAllowedAsync(_db, visitId, branchId))
            {
                return NotFound(new { message = "Visit not found for this branch." });
            }

            var ids = await _db.Report.AsNoTracking()
                .Where(r => r.VisitId == visitId)
                .OrderByDescending(r => r.FinalizedAt)
                .Select(r => r.Id)
                .ToListAsync();

            var list = new List<ReportDto>();
            foreach (var id in ids)
            {
                var dto = await BuildReportDto(id);
                if (dto != null)
                {
                    list.Add(dto);
                }
            }

            return Ok(list);
        }

        /// <summary>
        /// Report collection desk: today's finalized reports for the branch.
        /// </summary>
        [HttpGet("today")]
        public async Task<ActionResult<List<ReportDto>>> Today()
        {
            var branchId = BranchContext.ResolveBranchId(Request, User);
            var visitIds = await BranchContext.VisitIdsForBranchAsync(_db, branchId);
            var start = DateTime.Today;
            var end = start.AddDays(1);

            var reportIds = await (
                from r in _db.Report.AsNoTracking()
                join v in _db.Visit.AsNoTracking() on r.VisitId equals v.Id
                where visitIds.Contains(v.Id)
                      && r.FinalizedAt >= start
                      && r.FinalizedAt < end
                orderby r.FinalizedAt descending
                select r.Id
            ).Take(100).ToListAsync();

            return Ok(await BuildReportList(reportIds));
        }

        /// <summary>
        /// Report collection desk: find finalized reports by visit number, phone, or patient name.
        /// </summary>
        [HttpGet("lookup")]
        public async Task<ActionResult<List<ReportDto>>> Lookup([FromQuery] string? q)
        {
            var query = (q ?? string.Empty).Trim();
            if (query.Length < 2)
            {
                return BadRequest(new { message = "Enter visit number, phone, or patient name." });
            }

            var branchId = BranchContext.ResolveBranchId(Request, User);
            var visitIds = await BranchContext.VisitIdsForBranchAsync(_db, branchId);
            var needle = query.ToLowerInvariant();
            var compact = needle.Replace("v-", "", StringComparison.Ordinal).Replace("-", "", StringComparison.Ordinal);

            var reportIds = await (
                from r in _db.Report.AsNoTracking()
                join v in _db.Visit.AsNoTracking() on r.VisitId equals v.Id
                join p in _db.Patient.AsNoTracking() on v.PatientId equals p.Id
                where visitIds.Contains(v.Id)
                      && (
                          v.VisitNumber.ToLower().Contains(needle)
                          || v.VisitNumber.ToLower().Replace("v-", "").Contains(compact)
                          || p.FullName.ToLower().Contains(needle)
                          || (p.Phone != null && p.Phone.ToLower().Contains(needle))
                      )
                orderby r.FinalizedAt descending
                select r.Id
            ).Take(30).ToListAsync();

            return Ok(await BuildReportList(reportIds));
        }

        private async Task<List<ReportDto>> BuildReportList(List<int> reportIds)
        {
            var list = new List<ReportDto>();
            foreach (var id in reportIds)
            {
                var dto = await BuildReportDto(id);
                if (dto != null)
                {
                    list.Add(dto);
                }
            }

            return list;
        }

        [HttpPost("{id:int}/printed")]
        public async Task<ActionResult> MarkPrinted(int id)
        {
            var report = await _db.Report.FirstOrDefaultAsync(r => r.Id == id);
            if (report == null)
            {
                return NotFound(new { message = "Report not found." });
            }

            report.PrintCount += 1;
            await _db.SaveChangesAsync();
            return Ok(new { printCount = report.PrintCount });
        }

        private async Task<ReportDto?> BuildReportDto(int reportId)
        {
            var report = await _db.Report.AsNoTracking().FirstOrDefaultAsync(r => r.Id == reportId);
            if (report == null)
            {
                return null;
            }

            var visit = await _db.Visit.AsNoTracking().FirstAsync(v => v.Id == report.VisitId);
            var patient = await _db.Patient.AsNoTracking().FirstAsync(p => p.Id == visit.PatientId);

            var snap = await _db.ReportLine.AsNoTracking()
                .Where(l => l.ReportId == reportId)
                .OrderBy(l => l.Id)
                .Select(l => new ReportLineDto
                {
                    ServiceName = l.ServiceName,
                    ParameterName = l.ParameterName,
                    ResultText = l.ResultText,
                    Units = l.Units,
                    ReferenceRange = l.ReferenceRange,
                    ReportProfile = string.IsNullOrWhiteSpace(l.ReportProfile) ? ReportProfiles.Standard : l.ReportProfile,
                    Urgent = l.Urgent,
                    IsAbnormal = l.IsAbnormal,
                    IsCritical = l.IsCritical
                })
                .ToListAsync();

            // Fallback for older reports created before ReportLine existed.
            if (snap.Count == 0)
            {
                snap = await (
                    from vs in _db.VisitService.AsNoTracking()
                    join s in _db.Service.AsNoTracking() on vs.ServiceId equals s.Id
                    join tr in _db.TestResult.AsNoTracking().Where(r => r.IsCurrent) on vs.Id equals tr.VisitServiceId
                    where vs.VisitId == visit.Id
                          && vs.Status == VisitServiceStatuses.Finalized
                          && vs.FinalizedAt == report.FinalizedAt
                    orderby tr.Id
                    select new ReportLineDto
                    {
                        ServiceName = s.Name,
                        ParameterName = tr.ParameterName,
                        ResultText = tr.ResultText,
                        Units = tr.Units,
                        ReferenceRange = tr.ReferenceRange,
                        ReportProfile = string.IsNullOrWhiteSpace(s.ReportProfile) ? ReportProfiles.Standard : s.ReportProfile,
                        Urgent = vs.Urgent,
                        IsAbnormal = tr.IsAbnormal,
                        IsCritical = tr.IsCritical
                    }
                ).ToListAsync();
            }

            return new ReportDto
            {
                Id = report.Id,
                VisitId = visit.Id,
                VisitNumber = visit.VisitNumber,
                PatientName = patient.FullName,
                Phone = patient.Phone,
                Age = patient.Age,
                Gender = patient.Gender,
                Pathway = report.Pathway,
                FinalizedByName = report.FinalizedByName,
                FinalizedAt = report.FinalizedAt,
                PrintCount = report.PrintCount,
                Lines = snap
            };
        }
    }
}
