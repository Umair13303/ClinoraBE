using ClinoraBE.Infrastructure;
using ClinoraBE.Model.DAL;
using ClinoraBE.Model.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinoraBE.Controllers
{
    [ApiController]
    [Route("api/medical-certificate")]
    [Authorize]
    [StationAuthorize("MedicalCertificate", "Doctor", "FrontDesk", "Admin")]
    public class MedicalCertificateController : ControllerBase
    {
        private static readonly HashSet<string> CompleteStatuses = new(StringComparer.OrdinalIgnoreCase)
        {
            VisitServiceStatuses.Finalized,
            VisitServiceStatuses.Administered,
            VisitServiceStatuses.Examined,
            VisitServiceStatuses.Completed,
            VisitServiceStatuses.ResultEntered,
            VisitServiceStatuses.Imaged
        };

        private readonly ClinoraContext _db;

        public MedicalCertificateController(ClinoraContext db)
        {
            _db = db;
        }

        [HttpGet("package")]
        public async Task<ActionResult<CertificatePackageDto>> Package()
        {
            var names = WafidPackageNames();
            var services = await _db.Service.AsNoTracking()
                .Where(s => s.Status && names.Contains(s.Name))
                .OrderBy(s => s.Name)
                .Select(s => new ServiceDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    Category = s.Category,
                    Fee = s.Fee,
                    NormalMinutes = s.NormalMinutes,
                    UrgentMinutes = s.UrgentMinutes,
                    Pathway = string.IsNullOrWhiteSpace(s.Pathway) ? ServicePathways.Laboratory : s.Pathway,
                    ReportProfile = string.IsNullOrWhiteSpace(s.ReportProfile) ? ReportProfiles.Standard : s.ReportProfile,
                    SampleTypeId = s.SampleTypeId,
                    ContainerId = s.ContainerId,
                    BottleColorId = s.BottleColorId,
                    SampleGroupId = s.SampleGroupId,
                    CanSendOut = s.CanSendOut,
                    DefaultOutsideLabId = s.DefaultOutsideLabId
                })
                .ToListAsync();

            // Prefer stable Wafid order
            services = names
                .Select(n => services.FirstOrDefault(s => s.Name == n))
                .Where(s => s != null)
                .Cast<ServiceDto>()
                .ToList();

            return Ok(new CertificatePackageDto
            {
                Code = "WAFID",
                Name = "Wafid / Gulf medical",
                Services = services
            });
        }

        [HttpGet("worklist")]
        public async Task<ActionResult<List<MedicalCertificateWorkItemDto>>> Worklist()
        {
            var branchId = BranchContext.ResolveBranchId(Request, User);
            var visitIds = await BranchContext.VisitIdsForBranchAsync(_db, branchId);

            var visits = await (
                from v in _db.Visit.AsNoTracking()
                join p in _db.Patient.AsNoTracking() on v.PatientId equals p.Id
                where visitIds.Contains(v.Id)
                      && v.VisitStatus == VisitStatuses.Active
                      && v.Purpose == VisitPurposes.MedicalCertificate
                orderby v.CreatedAt descending
                select new { v, p }
            ).Take(80).ToListAsync();

            var ids = visits.Select(x => x.v.Id).ToList();
            var certs = await _db.MedicalCertificate.AsNoTracking()
                .Where(c => ids.Contains(c.VisitId))
                .ToDictionaryAsync(c => c.VisitId);
            var lines = await (
                from vs in _db.VisitService.AsNoTracking()
                join s in _db.Service.AsNoTracking() on vs.ServiceId equals s.Id
                where ids.Contains(vs.VisitId)
                select new { vs.VisitId, vs.Status, Pathway = s.Pathway }
            ).ToListAsync();

            var result = new List<MedicalCertificateWorkItemDto>();
            foreach (var row in visits)
            {
                certs.TryGetValue(row.v.Id, out var cert);
                var visitLines = lines.Where(l => l.VisitId == row.v.Id).ToList();
                var total = visitLines.Count;
                var done = visitLines.Count(l => IsComplete(l.Status, l.Pathway));
                result.Add(new MedicalCertificateWorkItemDto
                {
                    VisitId = row.v.Id,
                    VisitNumber = row.v.VisitNumber,
                    PatientName = row.p.FullName,
                    Phone = row.p.Phone,
                    Age = row.p.Age,
                    Gender = row.p.Gender,
                    PassportNo = row.p.PassportNo,
                    CreatedAt = row.v.CreatedAt,
                    Fitness = cert?.Fitness ?? CertificateFitness.Pending,
                    DestinationCountry = cert?.DestinationCountry,
                    IssuedAt = cert?.IssuedAt,
                    TotalLines = total,
                    CompletedLines = done,
                    ReadyToIssue = total > 0 && done == total && (cert?.Fitness == CertificateFitness.Pending || cert == null)
                });
            }

            return Ok(result);
        }

        [HttpGet("{visitId:int}")]
        public async Task<ActionResult<MedicalCertificateDetailDto>> Detail(int visitId)
        {
            var detail = await LoadDetailAsync(visitId);
            if (detail == null)
            {
                return NotFound(new { message = "Medical certificate visit not found." });
            }
            return Ok(detail);
        }

        [HttpPost("physical")]
        public async Task<ActionResult<MedicalCertificateDetailDto>> SavePhysical([FromBody] SavePhysicalExamRequest request)
        {
            var byName = (request.ExaminedByName ?? string.Empty).Trim();
            if (byName.Length < 2)
            {
                return BadRequest(new { message = "Enter examiner name." });
            }
            if (string.IsNullOrWhiteSpace(request.ExamJson) || request.ExamJson.Trim().Length < 5)
            {
                return BadRequest(new { message = "Enter physical examination details." });
            }

            var branchId = BranchContext.ResolveBranchId(Request, User);
            var visit = await _db.Visit.FirstOrDefaultAsync(v => v.Id == request.VisitId);
            if (visit == null || !BranchContext.Matches(visit.BranchId, branchId) || visit.VisitStatus != VisitStatuses.Active)
            {
                return NotFound(new { message = "Visit not found." });
            }
            if (visit.Purpose != VisitPurposes.MedicalCertificate)
            {
                return BadRequest(new { message = "Visit is not a medical certificate package." });
            }

            var physicalLine = await (
                from vs in _db.VisitService
                join s in _db.Service.AsNoTracking() on vs.ServiceId equals s.Id
                where vs.VisitId == visit.Id && s.Pathway == ServicePathways.Physical
                select vs
            ).FirstOrDefaultAsync();

            if (physicalLine == null)
            {
                return BadRequest(new { message = "No physical examination service on this visit." });
            }

            var now = DateTime.Now;
            var existing = await _db.TestResult
                .Where(r => r.VisitServiceId == physicalLine.Id && r.IsCurrent)
                .OrderByDescending(r => r.Version)
                .FirstOrDefaultAsync();

            if (existing != null)
            {
                existing.IsCurrent = false;
            }

            _db.TestResult.Add(new TestResult
            {
                VisitServiceId = physicalLine.Id,
                Source = "Physical",
                ResultText = request.ExamJson.Trim(),
                ParameterName = "Physical examination",
                EnteredByName = byName,
                EnteredAt = now,
                Version = (existing?.Version ?? 0) + 1,
                IsCurrent = true
            });

            physicalLine.Status = VisitServiceStatuses.Examined;
            physicalLine.ResultEnteredAt = now;
            physicalLine.FinalizedAt = now;
            physicalLine.FinalizedByName = byName;

            await EnsureCertificateRowAsync(visit.Id);
            await _db.SaveChangesAsync();

            var detail = await LoadDetailAsync(visit.Id);
            return Ok(detail!);
        }

        [HttpPost("issue")]
        public async Task<ActionResult<MedicalCertificateDetailDto>> Issue([FromBody] IssueMedicalCertificateRequest request)
        {
            var byName = (request.IssuedByName ?? string.Empty).Trim();
            if (byName.Length < 2)
            {
                return BadRequest(new { message = "Enter issuing doctor name." });
            }

            var fitness = (request.Fitness ?? string.Empty).Trim();
            if (fitness is not (CertificateFitness.Fit or CertificateFitness.Unfit))
            {
                return BadRequest(new { message = "Fitness must be Fit or Unfit." });
            }

            var detail = await LoadDetailAsync(request.VisitId);
            if (detail == null)
            {
                return NotFound(new { message = "Medical certificate visit not found." });
            }

            if (!detail.ReadyToIssue && detail.Fitness == CertificateFitness.Pending)
            {
                return BadRequest(new { message = "Complete physical, lab, radiology, and vaccination lines before issuing." });
            }

            var cert = await _db.MedicalCertificate.FirstOrDefaultAsync(c => c.VisitId == request.VisitId)
                       ?? await EnsureCertificateRowAsync(request.VisitId);

            cert.Fitness = fitness;
            cert.DestinationCountry = string.IsNullOrWhiteSpace(request.DestinationCountry)
                ? cert.DestinationCountry
                : request.DestinationCountry.Trim();
            cert.Remarks = string.IsNullOrWhiteSpace(request.Remarks) ? null : request.Remarks.Trim();
            cert.IssuedByName = byName;
            cert.IssuedAt = DateTime.Now;
            await _db.SaveChangesAsync();

            // Snapshot into Report so report-collection desk can print / track reprints.
            await UpsertCertificateReportAsync(request.VisitId, byName, cert.IssuedAt!.Value, fitness, cert.Remarks);

            return Ok((await LoadDetailAsync(request.VisitId))!);
        }

        private async Task UpsertCertificateReportAsync(
            int visitId,
            string issuedBy,
            DateTime issuedAt,
            string fitness,
            string? remarks)
        {
            var existing = await _db.Report.FirstOrDefaultAsync(r =>
                r.VisitId == visitId && r.Pathway == VisitPurposes.MedicalCertificate);

            Report report;
            if (existing == null)
            {
                report = new Report
                {
                    VisitId = visitId,
                    Pathway = VisitPurposes.MedicalCertificate,
                    FinalizedByName = issuedBy,
                    FinalizedAt = issuedAt,
                    PrintCount = 0
                };
                _db.Report.Add(report);
                await _db.SaveChangesAsync();
            }
            else
            {
                report = existing;
                report.FinalizedByName = issuedBy;
                report.FinalizedAt = issuedAt;
                var oldLines = await _db.ReportLine.Where(l => l.ReportId == report.Id).ToListAsync();
                _db.ReportLine.RemoveRange(oldLines);
                await _db.SaveChangesAsync();
            }

            var lines = await (
                from vs in _db.VisitService.AsNoTracking()
                join s in _db.Service.AsNoTracking() on vs.ServiceId equals s.Id
                where vs.VisitId == visitId
                orderby s.Pathway, s.Name
                select new { vs, s }
            ).ToListAsync();

            var vsIds = lines.Select(l => l.vs.Id).ToList();
            var results = await _db.TestResult.AsNoTracking()
                .Where(r => vsIds.Contains(r.VisitServiceId) && r.IsCurrent)
                .OrderByDescending(r => r.Version)
                .ToListAsync();
            var resultMap = results
                .GroupBy(r => r.VisitServiceId)
                .ToDictionary(g => g.Key, g => g.First());

            var firstVsId = lines.FirstOrDefault()?.vs.Id;
            foreach (var row in lines)
            {
                resultMap.TryGetValue(row.vs.Id, out var tr);
                var profile = string.IsNullOrWhiteSpace(row.s.ReportProfile)
                    ? ReportProfiles.Standard
                    : row.s.ReportProfile;
                var text = tr?.ResultText;
                if (row.s.Pathway == ServicePathways.Vaccination && string.IsNullOrWhiteSpace(text))
                {
                    text = "Administered";
                }

                _db.ReportLine.Add(new ReportLine
                {
                    ReportId = report.Id,
                    VisitServiceId = row.vs.Id,
                    ServiceName = row.s.Name,
                    ParameterName = tr?.ParameterName ?? (row.s.Pathway == ServicePathways.Physical ? "Physical examination" : null),
                    ResultText = text,
                    Units = tr?.Units,
                    ReferenceRange = tr?.ReferenceRange,
                    ReportProfile = profile,
                    Urgent = row.vs.Urgent,
                    IsAbnormal = row.vs.IsAbnormal,
                    IsCritical = row.vs.IsCritical
                });
            }

            // Summary line for Fit/Unfit on the desk list
            if (firstVsId.HasValue)
            {
                _db.ReportLine.Add(new ReportLine
                {
                    ReportId = report.Id,
                    VisitServiceId = firstVsId.Value,
                    ServiceName = $"Fitness: {fitness}",
                    ParameterName = "Certificate",
                    ResultText = remarks,
                    ReportProfile = ReportProfiles.Standard,
                    Urgent = false
                });
            }

            await _db.SaveChangesAsync();
        }

        private async Task<MedicalCertificateDetailDto?> LoadDetailAsync(int visitId)
        {
            var branchId = BranchContext.ResolveBranchId(Request, User);
            var row = await (
                from v in _db.Visit.AsNoTracking()
                join p in _db.Patient.AsNoTracking() on v.PatientId equals p.Id
                where v.Id == visitId && v.Purpose == VisitPurposes.MedicalCertificate
                select new { v, p }
            ).FirstOrDefaultAsync();

            if (row == null || !BranchContext.Matches(row.v.BranchId, branchId))
            {
                return null;
            }

            var cert = await _db.MedicalCertificate.AsNoTracking()
                .FirstOrDefaultAsync(c => c.VisitId == visitId);

            var lines = await (
                from vs in _db.VisitService.AsNoTracking()
                join s in _db.Service.AsNoTracking() on vs.ServiceId equals s.Id
                where vs.VisitId == visitId
                orderby s.Pathway, s.Name
                select new { vs, s }
            ).ToListAsync();

            var vsIds = lines.Select(l => l.vs.Id).ToList();
            var results = await _db.TestResult.AsNoTracking()
                .Where(r => vsIds.Contains(r.VisitServiceId) && r.IsCurrent)
                .GroupBy(r => r.VisitServiceId)
                .Select(g => g.OrderByDescending(x => x.Version).First())
                .ToListAsync();
            var resultMap = results.ToDictionary(r => r.VisitServiceId);

            var lineDtos = lines.Select(l =>
            {
                resultMap.TryGetValue(l.vs.Id, out var tr);
                var pathway = string.IsNullOrWhiteSpace(l.s.Pathway) ? ServicePathways.Laboratory : l.s.Pathway;
                var profile = string.IsNullOrWhiteSpace(l.s.ReportProfile) ? ReportProfiles.Standard : l.s.ReportProfile;
                var complete = IsComplete(l.vs.Status, pathway);
                var preview = tr?.ResultText;
                if (preview != null && preview.Length > 120)
                {
                    preview = preview[..117] + "...";
                }
                return new MedicalCertificateLineDto
                {
                    VisitServiceId = l.vs.Id,
                    ServiceId = l.s.Id,
                    ServiceName = l.s.Name,
                    Pathway = pathway,
                    ReportProfile = profile,
                    Status = l.vs.Status,
                    Complete = complete,
                    ResultPreview = preview
                };
            }).ToList();

            var physical = lineDtos.FirstOrDefault(l => l.Pathway == ServicePathways.Physical);
            string? physicalJson = null;
            if (physical != null && resultMap.TryGetValue(physical.VisitServiceId, out var physTr))
            {
                physicalJson = physTr.ResultText;
            }

            var ready = lineDtos.Count > 0 && lineDtos.All(l => l.Complete);

            return new MedicalCertificateDetailDto
            {
                VisitId = row.v.Id,
                VisitNumber = row.v.VisitNumber,
                PatientName = row.p.FullName,
                Phone = row.p.Phone,
                Age = row.p.Age,
                Gender = row.p.Gender,
                PassportNo = row.p.PassportNo,
                Cnic = row.p.Cnic,
                PhotoUrl = row.p.PhotoPath,
                Purpose = row.v.Purpose,
                Fitness = cert?.Fitness ?? CertificateFitness.Pending,
                DestinationCountry = cert?.DestinationCountry,
                Remarks = cert?.Remarks,
                IssuedByName = cert?.IssuedByName,
                IssuedAt = cert?.IssuedAt,
                ReadyToIssue = ready && (cert == null || cert.Fitness == CertificateFitness.Pending),
                PhysicalExamJson = physicalJson,
                PhysicalVisitServiceId = physical?.VisitServiceId,
                Lines = lineDtos
            };
        }

        private async Task<MedicalCertificate> EnsureCertificateRowAsync(int visitId)
        {
            var existing = await _db.MedicalCertificate.FirstOrDefaultAsync(c => c.VisitId == visitId);
            if (existing != null)
            {
                return existing;
            }

            var created = new MedicalCertificate
            {
                VisitId = visitId,
                Fitness = CertificateFitness.Pending
            };
            _db.MedicalCertificate.Add(created);
            await _db.SaveChangesAsync();
            return created;
        }

        private static bool IsComplete(string status, string? pathway)
        {
            if (CompleteStatuses.Contains(status))
            {
                return true;
            }
            // Radiology: Performed alone is not enough; Imaged/Finalized required
            if (pathway == ServicePathways.Radiology)
            {
                return status is VisitServiceStatuses.Imaged or VisitServiceStatuses.Finalized;
            }
            return false;
        }

        private static List<string> WafidPackageNames() =>
        [
            "Physical Examination",
            "Anti-HIV",
            "HBsAg",
            "Anti-HCV",
            "VDRL",
            "Chest X-ray",
            "Hepatitis B vaccine"
        ];
    }
}
