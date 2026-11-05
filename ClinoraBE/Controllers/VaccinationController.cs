using ClinoraBE.Infrastructure;
using ClinoraBE.Model.DAL;
using ClinoraBE.Model.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinoraBE.Controllers
{
    [ApiController]
    [Route("api/vaccination")]
    [Authorize]
    [StationAuthorize("Vaccination", "FrontDesk", "Doctor", "Admin")]
    public class VaccinationController : ControllerBase
    {
        private readonly ClinoraContext _db;

        public VaccinationController(ClinoraContext db)
        {
            _db = db;
        }

        [HttpGet("worklist")]
        public async Task<ActionResult<List<VaccinationWorkItemDto>>> Worklist()
        {
            var branchId = BranchContext.ResolveBranchId(Request, User);
            var visitIds = await BranchContext.VisitIdsForBranchAsync(_db, branchId);
            var today = DateTime.Today;

            var rows = await (
                from vs in _db.VisitService.AsNoTracking()
                join v in _db.Visit.AsNoTracking() on vs.VisitId equals v.Id
                join p in _db.Patient.AsNoTracking() on v.PatientId equals p.Id
                join s in _db.Service.AsNoTracking() on vs.ServiceId equals s.Id
                where s.Pathway == ServicePathways.Vaccination
                      && visitIds.Contains(v.Id)
                      && v.VisitStatus == VisitStatuses.Active
                      && (
                          vs.Status == VisitServiceStatuses.AwaitingAdministration
                          || (vs.Status == VisitServiceStatuses.Administered
                              && vs.FinalizedAt != null
                              && vs.FinalizedAt >= today)
                      )
                orderby vs.Urgent descending, v.CreatedAt
                select new
                {
                    vs.Id,
                    vs.VisitId,
                    vs.ServiceId,
                    v.VisitNumber,
                    PatientName = p.FullName,
                    p.Phone,
                    p.Age,
                    p.Gender,
                    ServiceName = s.Name,
                    vs.Status,
                    vs.Urgent,
                    v.CreatedAt
                }
            ).Take(120).ToListAsync();

            var ids = rows.Select(r => r.Id).ToList();
            var records = await _db.VaccinationRecord.AsNoTracking()
                .Where(r => ids.Contains(r.VisitServiceId))
                .ToDictionaryAsync(r => r.VisitServiceId);

            var items = rows.Select(r =>
            {
                records.TryGetValue(r.Id, out var rec);
                return new VaccinationWorkItemDto
                {
                    VisitServiceId = r.Id,
                    VisitId = r.VisitId,
                    ServiceId = r.ServiceId,
                    VisitNumber = r.VisitNumber,
                    PatientName = r.PatientName,
                    Phone = r.Phone,
                    Age = r.Age,
                    Gender = r.Gender,
                    ServiceName = r.ServiceName,
                    Status = r.Status,
                    Urgent = r.Urgent,
                    CreatedAt = r.CreatedAt,
                    Record = rec == null ? null : MapRecord(rec)
                };
            }).ToList();

            return Ok(items);
        }

        [HttpPost("administer")]
        public async Task<ActionResult<VaccinationWorkItemDto>> Administer([FromBody] AdministerVaccinationRequest request)
        {
            var byName = (request.AdministeredByName ?? string.Empty).Trim();
            if (byName.Length < 2)
            {
                return BadRequest(new { message = "Enter who administered the vaccine." });
            }

            var line = await _db.VisitService.FirstOrDefaultAsync(vs => vs.Id == request.VisitServiceId);
            if (line == null)
            {
                return NotFound(new { message = "Visit service not found." });
            }

            var branchId = BranchContext.ResolveBranchId(Request, User);
            var visit = await _db.Visit.AsNoTracking().FirstOrDefaultAsync(v => v.Id == line.VisitId);
            if (visit == null || !BranchContext.Matches(visit.BranchId, branchId) || visit.VisitStatus != VisitStatuses.Active)
            {
                return NotFound(new { message = "Visit not found for this branch." });
            }

            var service = await _db.Service.AsNoTracking().FirstOrDefaultAsync(s => s.Id == line.ServiceId);
            if (service == null || service.Pathway != ServicePathways.Vaccination)
            {
                return BadRequest(new { message = "Service is not a vaccination." });
            }

            if (line.Status is not (VisitServiceStatuses.AwaitingAdministration or VisitServiceStatuses.Administered))
            {
                return BadRequest(new { message = $"Cannot administer from status {line.Status}." });
            }

            var now = request.AdministeredAt ?? DateTime.Now;
            var existing = await _db.VaccinationRecord.FirstOrDefaultAsync(r => r.VisitServiceId == line.Id);
            if (existing == null)
            {
                existing = new VaccinationRecord { VisitServiceId = line.Id };
                _db.VaccinationRecord.Add(existing);
            }

            existing.BatchNo = TrimOrNull(request.BatchNo);
            existing.LotNo = TrimOrNull(request.LotNo);
            existing.ExpiryDate = request.ExpiryDate?.Date;
            existing.DoseNumber = TrimOrNull(request.DoseNumber);
            existing.Site = TrimOrNull(request.Site);
            existing.Route = TrimOrNull(request.Route);
            existing.AdministeredAt = now;
            existing.AdministeredByName = byName;
            existing.NextDueAt = request.NextDueAt?.Date;
            existing.Notes = TrimOrNull(request.Notes);

            line.Status = VisitServiceStatuses.Administered;
            line.FinalizedAt = now;
            line.FinalizedByName = byName;
            line.ResultEnteredAt = now;

            await _db.SaveChangesAsync();

            var patient = await _db.Patient.AsNoTracking().FirstAsync(p => p.Id == visit.PatientId);
            return Ok(new VaccinationWorkItemDto
            {
                VisitServiceId = line.Id,
                VisitId = line.VisitId,
                ServiceId = line.ServiceId,
                VisitNumber = visit.VisitNumber,
                PatientName = patient.FullName,
                Phone = patient.Phone,
                Age = patient.Age,
                Gender = patient.Gender,
                ServiceName = service.Name,
                Status = line.Status,
                Urgent = line.Urgent,
                CreatedAt = visit.CreatedAt,
                Record = MapRecord(existing)
            });
        }

        [HttpGet("{visitServiceId:int}/card")]
        public async Task<ActionResult<VaccinationCardDto>> Card(int visitServiceId)
        {
            var branchId = BranchContext.ResolveBranchId(Request, User);
            var row = await (
                from vs in _db.VisitService.AsNoTracking()
                join v in _db.Visit.AsNoTracking() on vs.VisitId equals v.Id
                join p in _db.Patient.AsNoTracking() on v.PatientId equals p.Id
                join s in _db.Service.AsNoTracking() on vs.ServiceId equals s.Id
                where vs.Id == visitServiceId && s.Pathway == ServicePathways.Vaccination
                select new { vs, v, p, s }
            ).FirstOrDefaultAsync();

            if (row == null || !BranchContext.Matches(row.v.BranchId, branchId))
            {
                return NotFound(new { message = "Vaccination line not found." });
            }

            var record = await _db.VaccinationRecord.AsNoTracking()
                .FirstOrDefaultAsync(r => r.VisitServiceId == visitServiceId);
            if (record == null)
            {
                return BadRequest(new { message = "Vaccine has not been administered yet." });
            }

            return Ok(new VaccinationCardDto
            {
                VisitNumber = row.v.VisitNumber,
                PatientName = row.p.FullName,
                Phone = row.p.Phone,
                Age = row.p.Age,
                Gender = row.p.Gender,
                VaccineName = row.s.Name,
                Record = MapRecord(record)
            });
        }

        private static VaccinationRecordDto MapRecord(VaccinationRecord r) => new()
        {
            Id = r.Id,
            VisitServiceId = r.VisitServiceId,
            BatchNo = r.BatchNo,
            LotNo = r.LotNo,
            ExpiryDate = r.ExpiryDate,
            DoseNumber = r.DoseNumber,
            Site = r.Site,
            Route = r.Route,
            AdministeredAt = r.AdministeredAt,
            AdministeredByName = r.AdministeredByName,
            NextDueAt = r.NextDueAt,
            Notes = r.Notes
        };

        private static string? TrimOrNull(string? value)
        {
            var t = (value ?? string.Empty).Trim();
            return t.Length == 0 ? null : t;
        }
    }
}
