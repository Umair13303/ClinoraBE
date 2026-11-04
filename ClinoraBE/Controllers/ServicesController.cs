using ClinoraBE.Model.DAL;
using ClinoraBE.Model.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinoraBE.Controllers
{
    [ApiController]
    [Route("api/services")]
    [Authorize]
    public class ServicesController : ControllerBase
    {
        private readonly ClinoraContext _db;

        public ServicesController(ClinoraContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<ActionResult<List<ServiceDto>>> Get([FromQuery] string? query)
        {
            var q = (query ?? string.Empty).Trim().ToLowerInvariant();
            var services = await (
                from s in _db.Service.AsNoTracking()
                join g in _db.SampleGroup.AsNoTracking() on s.SampleGroupId equals g.Id into gj
                from g in gj.DefaultIfEmpty()
                where s.Status
                where q == string.Empty
                    || s.Name.ToLower().Contains(q)
                    || s.Category.ToLower().Contains(q)
                orderby s.Name
                select new ServiceDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    Category = s.Category,
                    Fee = s.Fee,
                    NormalMinutes = s.NormalMinutes,
                    UrgentMinutes = s.UrgentMinutes,
                    Pathway = s.Pathway,
                    ReportProfile = string.IsNullOrWhiteSpace(s.ReportProfile) ? "Standard" : s.ReportProfile,
                    SampleTypeId = s.SampleTypeId,
                    ContainerId = s.ContainerId,
                    BottleColorId = s.BottleColorId,
                    SampleGroupId = s.SampleGroupId,
                    SampleGroupCode = g != null ? g.Code : null,
                    CanSendOut = s.CanSendOut,
                    DefaultOutsideLabId = s.DefaultOutsideLabId
                }
            ).Take(q == string.Empty ? 50 : 12).ToListAsync();

            return Ok(services);
        }
    }
}
