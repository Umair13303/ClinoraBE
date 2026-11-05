using System.Globalization;
using System.Security.Claims;
using ClinoraBE.Infrastructure;
using ClinoraBE.Model.DAL;
using ClinoraBE.Model.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinoraBE.Controllers
{
    [ApiController]
    [Route("api/visits")]
    [Authorize]
    [StationAuthorize("FrontDesk")]
    public class VisitsController : ControllerBase
    {
        private readonly ClinoraContext _db;
        private readonly IWebHostEnvironment _env;

        public VisitsController(ClinoraContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        [HttpGet]
        public async Task<ActionResult<List<VisitSummaryDto>>> GetToday()
        {
            var start = DateTime.Today;
            var end = start.AddDays(1);

            var branchId = BranchContext.ResolveBranchId(Request, User);
            var query =
                from v in _db.Visit.AsNoTracking()
                join p in _db.Patient.AsNoTracking() on v.PatientId equals p.Id
                where v.CreatedAt >= start && v.CreatedAt < end
                select new { v, p };

            if (branchId is int bid)
            {
                query = query.Where(x => x.v.BranchId == bid || x.v.BranchId == null);
            }

            var visits = await query
                .OrderByDescending(x => x.v.CreatedAt)
                .Take(50)
                .Select(x => new VisitSummaryDto
                {
                    Id = x.v.Id,
                    VisitNumber = x.v.VisitNumber,
                    TokenNo = x.v.TokenNo,
                    VisitStatus = x.v.VisitStatus,
                    PatientId = x.p.Id,
                    PatientName = x.p.FullName,
                    ReferredBy = string.IsNullOrWhiteSpace(x.v.ReferredBy) ? "—" : x.v.ReferredBy,
                    PanelName = x.p.IsPanel && !string.IsNullOrWhiteSpace(x.p.PanelName) ? x.p.PanelName : "—",
                    Net = x.v.Net,
                    CreatedAt = x.v.CreatedAt
                })
                .ToListAsync();

            return Ok(visits);
        }

        [HttpPost]
        public async Task<ActionResult<VisitReceiptDto>> Create([FromBody] CreateVisitRequest request)
        {
            if (request.Services == null || request.Services.Count == 0)
            {
                return BadRequest(new { message = "Add at least one service." });
            }

            if (!request.PaymentReceived)
            {
                return BadRequest(new { message = "Mark Payment received before creating the visit." });
            }

            var serviceIds = request.Services.Select(s => s.ServiceId).Distinct().ToList();
            if (serviceIds.Count != request.Services.Count)
            {
                return BadRequest(new { message = "Each service can only be added once." });
            }

            var catalog = await _db.Service
                .Where(s => s.Status && serviceIds.Contains(s.Id))
                .ToDictionaryAsync(s => s.Id);

            if (catalog.Count != serviceIds.Count)
            {
                return BadRequest(new { message = "One or more services are unknown." });
            }

            var lines = new List<(VisitServiceLineRequest Req, Service Service, decimal Discount)>();
            foreach (var line in request.Services)
            {
                var service = catalog[line.ServiceId];
                var discount = Math.Clamp(line.Discount, 0, service.Fee);
                if (!string.IsNullOrWhiteSpace(line.ReadyBy)
                    && !TimeOnly.TryParseExact(line.ReadyBy.Trim(), "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
                {
                    return BadRequest(new { message = $"Enter the time {service.Name} will be ready." });
                }

                lines.Add((line, service, discount));
            }

            var fees = lines.Sum(l => l.Service.Fee);
            var lineDiscounts = lines.Sum(l => l.Discount);
            var afterLines = fees - lineDiscounts;
            var percent = Math.Clamp(request.PercentOff, 0, 100);
            var fixedOff = Math.Max(request.VisitOff, 0);
            var visitDiscount = Math.Min(afterLines, Math.Round(afterLines * percent / 100m, MidpointRounding.AwayFromZero) + fixedOff);
            var net = afterLines - visitDiscount;
            var amountReceived = Math.Max(request.AmountReceived, 0);

            if (amountReceived < net)
            {
                return BadRequest(new { message = "Amount received must cover the amount due." });
            }

            Patient patient;
            if (request.PatientId is int existingId)
            {
                var found = await _db.Patient.FirstOrDefaultAsync(p => p.Id == existingId && p.Status);
                if (found == null)
                {
                    return BadRequest(new { message = "Selected patient was not found." });
                }

                patient = found;
                if (request.IsPanel && string.IsNullOrWhiteSpace(request.PanelName) && string.IsNullOrWhiteSpace(patient.PanelName))
                {
                    return BadRequest(new { message = "Panel name is required for panel patients." });
                }

                if (request.IsPanel && !string.IsNullOrWhiteSpace(request.PanelName))
                {
                    patient.IsPanel = true;
                    patient.PanelName = request.PanelName.Trim();
                }

                ApplyOptionalIdentity(patient, request);
                if (!string.IsNullOrWhiteSpace(request.PhotoBase64))
                {
                    patient.PhotoPath = await SavePatientPhotoAsync(patient.Id, request.PhotoBase64);
                }
            }
            else
            {
                var fullName = (request.FullName ?? string.Empty).Trim();
                var phone = (request.Phone ?? string.Empty).Trim();
                var gender = (request.Gender ?? string.Empty).Trim();
                var age = request.Age ?? -1;

                if (fullName.Length < 2)
                {
                    return BadRequest(new { message = "Enter the patient full name." });
                }

                if (phone.Length < 7)
                {
                    return BadRequest(new { message = "Enter a valid phone number." });
                }

                if (string.IsNullOrWhiteSpace(gender))
                {
                    return BadRequest(new { message = "Select gender." });
                }

                if (age < 0 || age > 120)
                {
                    return BadRequest(new { message = "Enter a valid age." });
                }

                if (request.IsPanel && string.IsNullOrWhiteSpace(request.PanelName))
                {
                    return BadRequest(new { message = "Panel name is required for panel patients." });
                }

                var duplicate = await _db.Patient.FirstOrDefaultAsync(p => p.Phone == phone && p.Status);
                if (duplicate != null)
                {
                    return BadRequest(new { message = $"{duplicate.FullName} is already registered with this phone. Select that patient." });
                }

                patient = new Patient
                {
                    FullName = fullName,
                    Phone = phone,
                    Age = age,
                    Gender = gender,
                    IsPanel = request.IsPanel,
                    PanelName = request.IsPanel ? request.PanelName?.Trim() : null,
                    Status = true,
                    CreatedOn = DateTime.UtcNow,
                    CreatedBy = ReadIntClaim(ClaimTypes.NameIdentifier),
                    BranchId = ReadIntClaim("BranchId"),
                    CompanyId = ReadIntClaim("CompanyId")
                };
                ApplyOptionalIdentity(patient, request);
                _db.Patient.Add(patient);
                await _db.SaveChangesAsync();

                if (!string.IsNullOrWhiteSpace(request.PhotoBase64))
                {
                    patient.PhotoPath = await SavePatientPhotoAsync(patient.Id, request.PhotoBase64);
                    await _db.SaveChangesAsync();
                }
            }

            var branchId = BranchContext.ResolveBranchId(Request, User) ?? ReadIntClaim("BranchId") ?? patient.BranchId;
            var today = DateTime.Today;
            var tokenBase = await _db.Visit.CountAsync(v => v.CreatedAt >= today && (branchId == null || v.BranchId == branchId));
            var nextNumber = await _db.Visit.CountAsync() + 1;
            var visit = new Visit
            {
                VisitNumber = $"V-{nextNumber:0000}",
                PatientId = patient.Id,
                TokenNo = tokenBase + 1,
                VisitStatus = VisitStatuses.Active,
                PercentOff = percent,
                VisitOff = fixedOff,
                Fees = fees,
                Discounts = lineDiscounts + visitDiscount,
                Net = net,
                AmountReceived = amountReceived,
                PaymentReceived = true,
                IsReferred = request.IsReferred,
                ReferredBy = request.IsReferred ? request.ReferredBy?.Trim() : null,
                SlipNo = request.IsReferred ? request.SlipNo?.Trim() : null,
                Notes = request.IsReferred ? request.Notes?.Trim() : null,
                BranchId = branchId,
                CompanyId = ReadIntClaim("CompanyId") ?? patient.CompanyId,
                CreatedBy = ReadIntClaim(ClaimTypes.NameIdentifier),
                CreatedAt = DateTime.Now
            };

            if (patient.BranchId == null && branchId != null)
            {
                patient.BranchId = branchId;
            }

            _db.Visit.Add(visit);
            await _db.SaveChangesAsync();

            foreach (var line in lines)
            {
                var pathway = string.IsNullOrWhiteSpace(line.Service.Pathway)
                    ? line.Service.Category
                    : line.Service.Pathway;
                var status = pathway switch
                {
                    ServicePathways.Consultation => VisitServiceStatuses.Completed,
                    ServicePathways.Radiology => VisitServiceStatuses.AwaitingImaging,
                    _ => VisitServiceStatuses.AwaitingCollection
                };

                _db.VisitService.Add(new VisitService
                {
                    VisitId = visit.Id,
                    ServiceId = line.Service.Id,
                    Fee = line.Service.Fee,
                    Discount = line.Discount,
                    Urgent = line.Req.Urgent,
                    StaffId = line.Req.StaffId,
                    StaffName = string.IsNullOrWhiteSpace(line.Req.StaffName) ? null : line.Req.StaffName.Trim(),
                    ReadyBy = string.IsNullOrWhiteSpace(line.Req.ReadyBy) ? null : line.Req.ReadyBy.Trim(),
                    Status = status,
                    IsSendOut = false,
                    OutsideLabId = null
                });
            }

            await _db.SaveChangesAsync();

            return Ok(new VisitReceiptDto
            {
                Id = visit.Id,
                VisitNumber = visit.VisitNumber,
                TokenNo = visit.TokenNo,
                PatientName = patient.FullName,
                Net = visit.Net,
                AmountReceived = visit.AmountReceived,
                CreatedAt = visit.CreatedAt
            });
        }

        [HttpPost("{id:int}/cancel")]
        public async Task<ActionResult> Cancel(int id)
        {
            var visit = await _db.Visit.FirstOrDefaultAsync(v => v.Id == id);
            if (visit == null)
            {
                return NotFound(new { message = "Visit not found." });
            }

            var branchId = BranchContext.ResolveBranchId(Request, User);
            if (!BranchContext.Matches(visit.BranchId, branchId))
            {
                return NotFound(new { message = "Visit not found for this branch." });
            }

            if (visit.VisitStatus == VisitStatuses.Cancelled)
            {
                return BadRequest(new { message = "Visit is already cancelled." });
            }

            var lines = await _db.VisitService.Where(vs => vs.VisitId == id).ToListAsync();
            if (lines.Any(l => l.Status is VisitServiceStatuses.Finalized or VisitServiceStatuses.ResultEntered or VisitServiceStatuses.InLab or VisitServiceStatuses.SentOut or VisitServiceStatuses.Performed or VisitServiceStatuses.Imaged))
            {
                return BadRequest(new { message = "Cannot cancel after lab / imaging work has started. Reject samples instead." });
            }

            visit.VisitStatus = VisitStatuses.Cancelled;
            foreach (var line in lines.Where(l => l.Status is VisitServiceStatuses.AwaitingCollection or VisitServiceStatuses.AwaitingImaging or VisitServiceStatuses.Completed))
            {
                if (line.Status != VisitServiceStatuses.Completed)
                {
                    line.Status = VisitServiceStatuses.Cancelled;
                }
            }

            await _db.SaveChangesAsync();
            return Ok(new { message = "Visit cancelled.", visitNumber = visit.VisitNumber });
        }

        [HttpGet("{id:int}/slips")]
        public async Task<ActionResult<VisitSlipsDto>> GetSlips(int id)
        {
            var visit = await _db.Visit.AsNoTracking().FirstOrDefaultAsync(v => v.Id == id);
            if (visit == null)
            {
                return NotFound(new { message = "Visit not found." });
            }

            var patient = await _db.Patient.AsNoTracking().FirstOrDefaultAsync(p => p.Id == visit.PatientId);
            if (patient == null)
            {
                return NotFound(new { message = "Patient not found." });
            }

            var lines = await (
                from vs in _db.VisitService.AsNoTracking()
                join s in _db.Service.AsNoTracking() on vs.ServiceId equals s.Id
                join st in _db.SampleType.AsNoTracking() on s.SampleTypeId equals st.Id into stj
                from st in stj.DefaultIfEmpty()
                join c in _db.Container.AsNoTracking() on s.ContainerId equals c.Id into cj
                from c in cj.DefaultIfEmpty()
                join bc in _db.BottleColor.AsNoTracking() on s.BottleColorId equals bc.Id into bcj
                from bc in bcj.DefaultIfEmpty()
                where vs.VisitId == id
                select new VisitSlipLineDto
                {
                    ServiceName = s.Name,
                    Pathway = s.Pathway,
                    Category = s.Category,
                    Fee = vs.Fee,
                    Urgent = vs.Urgent,
                    ReadyBy = vs.ReadyBy,
                    SampleTypeName = st != null ? st.Name : null,
                    ContainerName = c != null ? c.Name : null,
                    BottleColorName = bc != null ? bc.Name : null,
                    Status = vs.Status
                }
            ).ToListAsync();

            return Ok(new VisitSlipsDto
            {
                VisitId = visit.Id,
                VisitNumber = visit.VisitNumber,
                TokenNo = visit.TokenNo,
                CreatedAt = visit.CreatedAt,
                PatientName = patient.FullName,
                Phone = patient.Phone,
                Age = patient.Age,
                Gender = patient.Gender,
                ReferredBy = visit.ReferredBy,
                Net = visit.Net,
                AmountReceived = visit.AmountReceived,
                Lines = lines
            });
        }

        private static void ApplyOptionalIdentity(Patient patient, CreateVisitRequest request)
        {
            if (!string.IsNullOrWhiteSpace(request.Cnic))
            {
                patient.Cnic = request.Cnic.Trim();
            }

            if (!string.IsNullOrWhiteSpace(request.PassportNo))
            {
                patient.PassportNo = request.PassportNo.Trim();
            }
        }

        private async Task<string?> SavePatientPhotoAsync(int patientId, string photoBase64)
        {
            var raw = photoBase64.Trim();
            var comma = raw.IndexOf(',');
            if (raw.StartsWith("data:", StringComparison.OrdinalIgnoreCase) && comma > 0)
            {
                raw = raw[(comma + 1)..];
            }

            byte[] bytes;
            try
            {
                bytes = Convert.FromBase64String(raw);
            }
            catch (FormatException)
            {
                return null;
            }

            if (bytes.Length == 0 || bytes.Length > 2_000_000)
            {
                return null;
            }

            var webRoot = _env.WebRootPath;
            if (string.IsNullOrWhiteSpace(webRoot))
            {
                webRoot = Path.Combine(_env.ContentRootPath, "wwwroot");
            }

            var folder = Path.Combine(webRoot, "uploads", "patients");
            Directory.CreateDirectory(folder);

            var fileName = $"{patientId}-{Guid.NewGuid():N}.jpg";
            var fullPath = Path.Combine(folder, fileName);
            await System.IO.File.WriteAllBytesAsync(fullPath, bytes);
            return $"/uploads/patients/{fileName}";
        }

        private int? ReadIntClaim(string type)
        {
            var value = User.FindFirstValue(type);
            return int.TryParse(value, out var id) && id > 0 ? id : null;
        }
    }
}
