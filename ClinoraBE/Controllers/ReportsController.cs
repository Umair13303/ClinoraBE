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
    public class ReportsController : ControllerBase
    {
        private readonly ClinoraContext _db;

        public ReportsController(ClinoraContext db)
        {
            _db = db;
        }

        [HttpPost("finalize")]
        public async Task<ActionResult<ReportDto>> Finalize([FromBody] FinalizeRequest request)
        {
            if (request.VisitServiceIds == null || request.VisitServiceIds.Count == 0)
            {
                return BadRequest(new { message = "Select at least one test to finalize." });
            }

            var lines = await _db.VisitService
                .Where(vs => vs.VisitId == request.VisitId && request.VisitServiceIds.Contains(vs.Id))
                .ToListAsync();

            if (lines.Count != request.VisitServiceIds.Count)
            {
                return BadRequest(new { message = "One or more lines were not found on this visit." });
            }

            if (lines.Any(l => l.Status is not (VisitServiceStatuses.ResultEntered or VisitServiceStatuses.Imaged)))
            {
                return BadRequest(new { message = "Only result-entered or imaged tests can be finalized." });
            }

            var byName = string.IsNullOrWhiteSpace(request.FinalizedByName) ? "Doctor" : request.FinalizedByName.Trim();
            var now = DateTime.Now;
            foreach (var line in lines)
            {
                line.Status = VisitServiceStatuses.Finalized;
                line.FinalizedAt = now;
                line.FinalizedByName = byName;
            }

            var serviceIds = lines.Select(l => l.ServiceId).Distinct().ToList();
            var pathways = await _db.Service.AsNoTracking()
                .Where(s => serviceIds.Contains(s.Id))
                .Select(s => s.Pathway)
                .Distinct()
                .ToListAsync();
            var pathway = pathways.Count == 1
                ? pathways[0]
                : pathways.Contains(ServicePathways.Radiology) && pathways.Contains(ServicePathways.Laboratory)
                    ? "Mixed"
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

            var dto = await BuildReportDto(report.Id);
            return Ok(dto);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<ReportDto>> Get(int id)
        {
            var dto = await BuildReportDto(id);
            if (dto == null)
            {
                return NotFound(new { message = "Report not found." });
            }

            return Ok(dto);
        }

        [HttpGet("by-visit/{visitId:int}")]
        public async Task<ActionResult<List<ReportDto>>> ByVisit(int visitId)
        {
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

            var lines = await (
                from vs in _db.VisitService.AsNoTracking()
                join s in _db.Service.AsNoTracking() on vs.ServiceId equals s.Id
                join tr in _db.TestResult.AsNoTracking() on vs.Id equals tr.VisitServiceId into trj
                from tr in trj.DefaultIfEmpty()
                where vs.VisitId == visit.Id
                      && vs.Status == VisitServiceStatuses.Finalized
                      && vs.FinalizedAt == report.FinalizedAt
                select new ReportLineDto
                {
                    ServiceName = s.Name,
                    ResultText = tr != null ? tr.ResultText : null,
                    Units = tr != null ? tr.Units : null,
                    ReferenceRange = tr != null ? tr.ReferenceRange : null,
                    Urgent = vs.Urgent
                }
            ).ToListAsync();

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
                Lines = lines
            };
        }
    }
}
