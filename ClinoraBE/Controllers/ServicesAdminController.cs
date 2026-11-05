using ClinoraBE.Infrastructure;
using ClinoraBE.Model.DAL;
using ClinoraBE.Model.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinoraBE.Controllers
{
    [ApiController]
    [Route("api/admin/services")]
    [Authorize]
    [StationAuthorize("Admin")]
    public class ServicesAdminController : ControllerBase
    {
        private readonly ClinoraContext _db;

        public ServicesAdminController(ClinoraContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<ActionResult<List<ServiceAdminDto>>> List()
        {
            var list = await (
                from s in _db.Service.AsNoTracking()
                orderby s.Name
                select new ServiceAdminDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    Category = s.Category,
                    Fee = s.Fee,
                    NormalMinutes = s.NormalMinutes,
                    UrgentMinutes = s.UrgentMinutes,
                    Pathway = s.Pathway,
                    ReportProfile = string.IsNullOrWhiteSpace(s.ReportProfile) ? ReportProfiles.Standard : s.ReportProfile,
                    SampleTypeId = s.SampleTypeId,
                    ContainerId = s.ContainerId,
                    BottleColorId = s.BottleColorId,
                    SampleGroupId = s.SampleGroupId,
                    CanSendOut = s.CanSendOut,
                    DefaultOutsideLabId = s.DefaultOutsideLabId,
                    Status = s.Status,
                    Parameters = _db.ServiceParameter.AsNoTracking()
                        .Where(p => p.ServiceId == s.Id && p.Status)
                        .OrderBy(p => p.SortOrder)
                        .Select(p => new ServiceParameterDto
                        {
                            Id = p.Id,
                            Name = p.Name,
                            Unit = p.Unit,
                            RefLow = p.RefLow,
                            RefHigh = p.RefHigh,
                            SortOrder = p.SortOrder
                        })
                        .ToList()
                }
            ).ToListAsync();

            return Ok(list);
        }

        [HttpPost]
        public async Task<ActionResult<ServiceAdminDto>> Create([FromBody] UpsertServiceRequest request)
        {
            var name = (request.Name ?? string.Empty).Trim();
            if (name.Length < 2)
            {
                return BadRequest(new { message = "Service name is required." });
            }

            var entity = MapNew(request, name);
            _db.Service.Add(entity);
            await _db.SaveChangesAsync();
            await ReplaceParameters(entity.Id, request.Parameters);
            return Ok(await GetDto(entity.Id));
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<ServiceAdminDto>> Update(int id, [FromBody] UpsertServiceRequest request)
        {
            var entity = await _db.Service.FirstOrDefaultAsync(s => s.Id == id);
            if (entity == null)
            {
                return NotFound(new { message = "Service not found." });
            }

            var name = (request.Name ?? string.Empty).Trim();
            if (name.Length < 2)
            {
                return BadRequest(new { message = "Service name is required." });
            }

            entity.Name = name;
            entity.Category = string.IsNullOrWhiteSpace(request.Category) ? entity.Category : request.Category.Trim();
            entity.Fee = Math.Max(request.Fee, 0);
            entity.NormalMinutes = Math.Max(request.NormalMinutes, 0);
            entity.UrgentMinutes = Math.Max(request.UrgentMinutes, 0);
            entity.Pathway = string.IsNullOrWhiteSpace(request.Pathway) ? entity.Pathway : request.Pathway.Trim();
            entity.ReportProfile = NormalizeProfile(request.ReportProfile, entity.Pathway);
            entity.SampleTypeId = request.SampleTypeId;
            entity.ContainerId = request.ContainerId;
            entity.BottleColorId = request.BottleColorId;
            entity.SampleGroupId = request.SampleGroupId;
            entity.CanSendOut = request.CanSendOut;
            entity.DefaultOutsideLabId = request.DefaultOutsideLabId;
            entity.Status = request.Status;
            await _db.SaveChangesAsync();
            await ReplaceParameters(entity.Id, request.Parameters);
            return Ok(await GetDto(id));
        }

        private async Task<ServiceAdminDto> GetDto(int id)
        {
            var s = await _db.Service.AsNoTracking().FirstAsync(x => x.Id == id);
            var parameters = await _db.ServiceParameter.AsNoTracking()
                .Where(p => p.ServiceId == id && p.Status)
                .OrderBy(p => p.SortOrder)
                .Select(p => new ServiceParameterDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Unit = p.Unit,
                    RefLow = p.RefLow,
                    RefHigh = p.RefHigh,
                    SortOrder = p.SortOrder
                })
                .ToListAsync();
            return new ServiceAdminDto
            {
                Id = s.Id,
                Name = s.Name,
                Category = s.Category,
                Fee = s.Fee,
                NormalMinutes = s.NormalMinutes,
                UrgentMinutes = s.UrgentMinutes,
                Pathway = s.Pathway,
                ReportProfile = string.IsNullOrWhiteSpace(s.ReportProfile) ? ReportProfiles.Standard : s.ReportProfile,
                SampleTypeId = s.SampleTypeId,
                ContainerId = s.ContainerId,
                BottleColorId = s.BottleColorId,
                SampleGroupId = s.SampleGroupId,
                CanSendOut = s.CanSendOut,
                DefaultOutsideLabId = s.DefaultOutsideLabId,
                Status = s.Status,
                Parameters = parameters
            };
        }

        private static Service MapNew(UpsertServiceRequest request, string name)
        {
            var pathway = string.IsNullOrWhiteSpace(request.Pathway) ? ServicePathways.Laboratory : request.Pathway.Trim();
            return new Service
            {
                Name = name,
                Category = string.IsNullOrWhiteSpace(request.Category) ? "Laboratory" : request.Category.Trim(),
                Fee = Math.Max(request.Fee, 0),
                NormalMinutes = Math.Max(request.NormalMinutes, 0),
                UrgentMinutes = Math.Max(request.UrgentMinutes, 0),
                Pathway = pathway,
                ReportProfile = NormalizeProfile(request.ReportProfile, pathway),
                SampleTypeId = request.SampleTypeId,
                ContainerId = request.ContainerId,
                BottleColorId = request.BottleColorId,
                SampleGroupId = request.SampleGroupId,
                CanSendOut = request.CanSendOut,
                DefaultOutsideLabId = request.DefaultOutsideLabId,
                Status = request.Status,
                CreatedOn = DateTime.UtcNow
            };
        }

        private static string NormalizeProfile(string? profile, string pathway)
        {
            if (pathway == ServicePathways.Radiology)
            {
                return ReportProfiles.Radiology;
            }

            var value = ReportProfiles.Normalize(profile);
            return value is ReportProfiles.Narrative or ReportProfiles.Semen or ReportProfiles.Standard
                ? value
                : ReportProfiles.Standard;
        }

        private async Task ReplaceParameters(int serviceId, List<ServiceParameterDto>? parameters)
        {
            var existing = await _db.ServiceParameter.Where(p => p.ServiceId == serviceId).ToListAsync();
            _db.ServiceParameter.RemoveRange(existing);
            if (parameters == null)
            {
                await _db.SaveChangesAsync();
                return;
            }

            var order = 1;
            foreach (var p in parameters.Where(x => !string.IsNullOrWhiteSpace(x.Name)))
            {
                _db.ServiceParameter.Add(new ServiceParameter
                {
                    ServiceId = serviceId,
                    Name = p.Name.Trim(),
                    Unit = string.IsNullOrWhiteSpace(p.Unit) ? null : p.Unit.Trim(),
                    RefLow = p.RefLow,
                    RefHigh = p.RefHigh,
                    SortOrder = p.SortOrder > 0 ? p.SortOrder : order,
                    Status = true
                });
                order++;
            }

            await _db.SaveChangesAsync();
        }

    }

    public class ServiceParameterDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Unit { get; set; }
        public decimal? RefLow { get; set; }
        public decimal? RefHigh { get; set; }
        public int SortOrder { get; set; }
    }

    public class ServiceAdminDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public decimal Fee { get; set; }
        public int NormalMinutes { get; set; }
        public int UrgentMinutes { get; set; }
        public string Pathway { get; set; } = string.Empty;
        public string ReportProfile { get; set; } = ReportProfiles.Standard;
        public int? SampleTypeId { get; set; }
        public int? ContainerId { get; set; }
        public int? BottleColorId { get; set; }
        public int? SampleGroupId { get; set; }
        public bool CanSendOut { get; set; }
        public int? DefaultOutsideLabId { get; set; }
        public bool Status { get; set; }
        public List<ServiceParameterDto> Parameters { get; set; } = [];
    }

    public class UpsertServiceRequest
    {
        public string? Name { get; set; }
        public string? Category { get; set; }
        public decimal Fee { get; set; }
        public int NormalMinutes { get; set; } = 60;
        public int UrgentMinutes { get; set; } = 30;
        public string? Pathway { get; set; }
        public string? ReportProfile { get; set; }
        public int? SampleTypeId { get; set; }
        public int? ContainerId { get; set; }
        public int? BottleColorId { get; set; }
        public int? SampleGroupId { get; set; }
        public bool CanSendOut { get; set; }
        public int? DefaultOutsideLabId { get; set; }
        public bool Status { get; set; } = true;
        public List<ServiceParameterDto>? Parameters { get; set; }
    }
}
