using ClinoraBE.Infrastructure;
using ClinoraBE.Model.DAL;
using ClinoraBE.Model.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinoraBE.Controllers
{
    [ApiController]
    [Route("api/radiology")]
    [Authorize]
    public class RadiologyController : ControllerBase
    {
        private readonly ClinoraContext _db;

        public RadiologyController(ClinoraContext db)
        {
            _db = db;
        }

        [HttpGet("templates")]
        public async Task<ActionResult<List<RadiologyTemplateDto>>> Templates(
            [FromQuery] int? serviceId = null,
            [FromQuery] string? profile = null)
        {
            var profileFilter = string.IsNullOrWhiteSpace(profile) ? ReportProfiles.Radiology : profile.Trim();
            var templates = await _db.RadiologyTemplate.AsNoTracking()
                .Where(t => t.Status)
                .Where(t => (string.IsNullOrWhiteSpace(t.Profile) ? ReportProfiles.Radiology : t.Profile) == profileFilter)
                .OrderBy(t => t.Modality)
                .ThenBy(t => t.Name)
                .ToListAsync();

            var dtos = await MapTemplatesAsync(templates);
            if (serviceId is int sid)
            {
                dtos = dtos
                    .Where(t => t.ServiceIds.Count == 0 || t.ServiceIds.Contains(sid))
                    .ToList();
            }

            return Ok(dtos);
        }

        [HttpGet("templates/all")]
        public async Task<ActionResult<List<RadiologyTemplateDto>>> TemplatesAll([FromQuery] string? profile = null)
        {
            var query = _db.RadiologyTemplate.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(profile))
            {
                var profileFilter = profile.Trim();
                query = query.Where(t => (string.IsNullOrWhiteSpace(t.Profile) ? ReportProfiles.Radiology : t.Profile) == profileFilter);
            }

            var templates = await query
                .OrderByDescending(t => t.Status)
                .ThenBy(t => t.Modality)
                .ThenBy(t => t.Name)
                .ToListAsync();
            return Ok(await MapTemplatesAsync(templates));
        }

        [HttpPost("templates")]
        public async Task<ActionResult<RadiologyTemplateDto>> CreateTemplate([FromBody] UpsertRadiologyTemplateRequest request)
        {
            var name = (request.Name ?? string.Empty).Trim();
            var body = (request.Body ?? string.Empty).Trim();
            if (name.Length < 2 || body.Length < 2)
            {
                return BadRequest(new { message = "Template name and body are required." });
            }

            var serviceIds = NormalizeServiceIds(request.ServiceIds);
            var profile = string.IsNullOrWhiteSpace(request.Profile) ? ReportProfiles.Radiology : request.Profile.Trim();
            var entity = new RadiologyTemplate
            {
                Name = name,
                Modality = string.IsNullOrWhiteSpace(request.Modality)
                    ? (profile == ReportProfiles.Narrative ? "Histo" : "XRay")
                    : request.Modality.Trim(),
                Profile = profile is ReportProfiles.Narrative ? ReportProfiles.Narrative : ReportProfiles.Radiology,
                ServiceId = serviceIds.FirstOrDefault() is int first && first > 0 ? first : null,
                Body = body,
                Status = request.Status,
                CreatedOn = DateTime.UtcNow
            };
            _db.RadiologyTemplate.Add(entity);
            await _db.SaveChangesAsync();
            await ReplaceTemplateServicesAsync(entity.Id, serviceIds);
            return Ok(await GetTemplateDto(entity.Id));
        }

        [HttpPut("templates/{id:int}")]
        public async Task<ActionResult<RadiologyTemplateDto>> UpdateTemplate(int id, [FromBody] UpsertRadiologyTemplateRequest request)
        {
            var entity = await _db.RadiologyTemplate.FirstOrDefaultAsync(t => t.Id == id);
            if (entity == null)
            {
                return NotFound(new { message = "Template not found." });
            }

            var name = (request.Name ?? string.Empty).Trim();
            var body = (request.Body ?? string.Empty).Trim();
            if (name.Length < 2 || body.Length < 2)
            {
                return BadRequest(new { message = "Template name and body are required." });
            }

            var serviceIds = NormalizeServiceIds(request.ServiceIds);
            entity.Name = name;
            entity.Modality = string.IsNullOrWhiteSpace(request.Modality) ? entity.Modality : request.Modality.Trim();
            if (!string.IsNullOrWhiteSpace(request.Profile))
            {
                entity.Profile = request.Profile.Trim() == ReportProfiles.Narrative
                    ? ReportProfiles.Narrative
                    : ReportProfiles.Radiology;
            }
            entity.ServiceId = serviceIds.FirstOrDefault() is int first && first > 0 ? first : null;
            entity.Body = body;
            entity.Status = request.Status;
            await _db.SaveChangesAsync();
            await ReplaceTemplateServicesAsync(entity.Id, serviceIds);
            return Ok(await GetTemplateDto(entity.Id));
        }

        [HttpPost("complete")]
        public async Task<ActionResult> Complete([FromBody] RadiologyCompleteRequest request)
        {
            var result = await SaveDraftAsync(request);
            if (result.Error != null)
            {
                return result.Error;
            }

            return Ok(new { message = "Report drafted. Radiologist can sign when ready.", templateName = result.TemplateName });
        }

        [HttpPost("sign")]
        public async Task<ActionResult<ReportDto>> Sign([FromBody] RadiologyCompleteRequest request)
        {
            var draft = await SaveDraftAsync(request);
            if (draft.Error != null)
            {
                return draft.Error;
            }

            var line = draft.Line!;
            if (line.Status != VisitServiceStatuses.Imaged)
            {
                return BadRequest(new { message = "Save the drafted report before signing." });
            }

            var byName = string.IsNullOrWhiteSpace(request.PerformedByName) ? "Radiologist" : request.PerformedByName.Trim();
            var now = DateTime.Now;
            line.Status = VisitServiceStatuses.Finalized;
            line.FinalizedAt = now;
            line.FinalizedByName = byName;

            var service = await _db.Service.AsNoTracking().FirstAsync(s => s.Id == line.ServiceId);
            var report = new Report
            {
                VisitId = line.VisitId,
                Pathway = ServicePathways.Radiology,
                FinalizedByName = byName,
                FinalizedAt = now,
                PrintCount = 0
            };
            _db.Report.Add(report);
            await _db.SaveChangesAsync();

            var tr = await _db.TestResult.AsNoTracking()
                .Where(r => r.VisitServiceId == line.Id && r.IsCurrent)
                .OrderByDescending(r => r.Version)
                .FirstOrDefaultAsync();

            _db.ReportLine.Add(new ReportLine
            {
                ReportId = report.Id,
                VisitServiceId = line.Id,
                ServiceName = service.Name,
                ResultText = tr?.ResultText,
                Units = tr?.Units,
                ReferenceRange = tr?.ReferenceRange,
                ReportProfile = ReportProfiles.Radiology,
                Urgent = line.Urgent,
                IsAbnormal = line.IsAbnormal,
                IsCritical = line.IsCritical
            });
            await _db.SaveChangesAsync();

            var visit = await _db.Visit.AsNoTracking().FirstAsync(v => v.Id == line.VisitId);
            var patient = await _db.Patient.AsNoTracking().FirstAsync(p => p.Id == visit.PatientId);

            return Ok(new ReportDto
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
                PrintCount = 0,
                Lines =
                [
                    new ReportLineDto
                    {
                        ServiceName = service.Name,
                        ResultText = tr?.ResultText,
                        Units = tr?.Units,
                        ReferenceRange = tr?.ReferenceRange,
                        ReportProfile = ReportProfiles.Radiology,
                        Urgent = line.Urgent,
                        IsAbnormal = line.IsAbnormal,
                        IsCritical = line.IsCritical
                    }
                ]
            });
        }

        private async Task<(VisitService? Line, string? TemplateName, ActionResult? Error)> SaveDraftAsync(RadiologyCompleteRequest request)
        {
            var line = await _db.VisitService.FirstOrDefaultAsync(vs => vs.Id == request.VisitServiceId);
            if (line == null)
            {
                return (null, null, NotFound(new { message = "Visit service not found." }));
            }

            var branchId = BranchContext.ResolveBranchId(Request, User);
            if (!await BranchContext.VisitAllowedAsync(_db, line.VisitId, branchId))
            {
                return (null, null, NotFound(new { message = "Visit service not found for this branch." }));
            }

            if (line.Status is not (VisitServiceStatuses.AwaitingImaging or VisitServiceStatuses.Imaged))
            {
                return (null, null, BadRequest(new { message = "Only awaiting / drafted radiology studies can be saved here." }));
            }

            var findings = (request.Findings ?? string.Empty).Trim();
            var plainFindings = System.Text.RegularExpressions.Regex
                .Replace(findings, "<[^>]+>", " ")
                .Replace("&nbsp;", " ", StringComparison.OrdinalIgnoreCase)
                .Trim();
            if (plainFindings.Length < 3)
            {
                return (null, null, BadRequest(new { message = "Select a template and enter / edit the report text." }));
            }

            string? templateName = string.IsNullOrWhiteSpace(request.TemplateName) ? null : request.TemplateName.Trim();
            if (request.TemplateId is int tid)
            {
                var template = await _db.RadiologyTemplate.AsNoTracking().FirstOrDefaultAsync(t => t.Id == tid);
                if (template != null)
                {
                    templateName = template.Name;
                }
            }

            var existing = await _db.TestResult
                .Where(r => r.VisitServiceId == line.Id && r.IsCurrent)
                .OrderByDescending(r => r.Version)
                .FirstOrDefaultAsync();

            if (existing == null)
            {
                _db.TestResult.Add(new TestResult
                {
                    VisitServiceId = line.Id,
                    Source = "Radiology",
                    ResultText = findings,
                    EnteredByName = string.IsNullOrWhiteSpace(request.PerformedByName) ? null : request.PerformedByName.Trim(),
                    EnteredAt = DateTime.Now,
                    Version = 1,
                    IsCurrent = true,
                    ParameterName = templateName
                });
            }
            else
            {
                existing.ResultText = findings;
                existing.EnteredByName = string.IsNullOrWhiteSpace(request.PerformedByName)
                    ? existing.EnteredByName
                    : request.PerformedByName.Trim();
                existing.EnteredAt = DateTime.Now;
                existing.Source = "Radiology";
                existing.ParameterName = templateName ?? existing.ParameterName;
            }

            line.Status = VisitServiceStatuses.Imaged;
            line.ResultEnteredAt = DateTime.Now;
            await _db.SaveChangesAsync();
            return (line, templateName, null);
        }

        private static List<int> NormalizeServiceIds(List<int>? ids) =>
            (ids ?? []).Where(id => id > 0).Distinct().OrderBy(id => id).ToList();

        private async Task ReplaceTemplateServicesAsync(int templateId, List<int> serviceIds)
        {
            var existing = await _db.RadiologyTemplateService.Where(x => x.TemplateId == templateId).ToListAsync();
            _db.RadiologyTemplateService.RemoveRange(existing);
            foreach (var serviceId in serviceIds)
            {
                _db.RadiologyTemplateService.Add(new RadiologyTemplateService
                {
                    TemplateId = templateId,
                    ServiceId = serviceId
                });
            }
            await _db.SaveChangesAsync();
        }

        private async Task<RadiologyTemplateDto> GetTemplateDto(int id)
        {
            var entity = await _db.RadiologyTemplate.AsNoTracking().FirstAsync(t => t.Id == id);
            return (await MapTemplatesAsync([entity])).First();
        }

        private async Task<List<RadiologyTemplateDto>> MapTemplatesAsync(List<RadiologyTemplate> templates)
        {
            if (templates.Count == 0)
            {
                return [];
            }

            var templateIds = templates.Select(t => t.Id).ToList();
            var links = await _db.RadiologyTemplateService.AsNoTracking()
                .Where(x => templateIds.Contains(x.TemplateId))
                .ToListAsync();
            var serviceIds = links.Select(x => x.ServiceId).Distinct().ToList();
            var services = serviceIds.Count == 0
                ? new Dictionary<int, string>()
                : await _db.Service.AsNoTracking()
                    .Where(s => serviceIds.Contains(s.Id))
                    .ToDictionaryAsync(s => s.Id, s => s.Name);

            return templates.Select(t =>
            {
                var ids = links.Where(x => x.TemplateId == t.Id).Select(x => x.ServiceId).Distinct().OrderBy(x => x).ToList();
                // Fallback for rows not yet migrated into join table.
                if (ids.Count == 0 && t.ServiceId is int legacyId)
                {
                    ids = [legacyId];
                }

                var names = ids
                    .Where(id => services.ContainsKey(id))
                    .Select(id => services[id])
                    .ToList();

                return new RadiologyTemplateDto
                {
                    Id = t.Id,
                    Name = t.Name,
                    Modality = t.Modality,
                    Profile = string.IsNullOrWhiteSpace(t.Profile) ? ReportProfiles.Radiology : t.Profile,
                    ServiceIds = ids,
                    ServiceNames = names,
                    ServiceName = names.Count == 0 ? null : string.Join(", ", names),
                    Body = t.Body,
                    Status = t.Status
                };
            }).ToList();
        }
    }
}
