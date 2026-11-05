using ClinoraBE.Infrastructure;
using ClinoraBE.Model.DAL;
using ClinoraBE.Model.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinoraBE.Controllers
{
    [ApiController]
    [Route("api/lookups")]
    [Authorize]
    [StationAuthorize("FrontDesk", "Lab", "Doctor", "Radiology", "Phlebotomy", "Vaccination")]
    public class LookupsController : ControllerBase
    {
        private readonly ClinoraContext _db;

        public LookupsController(ClinoraContext db)
        {
            _db = db;
        }

        [HttpGet("sample-setup")]
        public async Task<ActionResult<SampleSetupDto>> SampleSetup()
        {
            var dto = new SampleSetupDto
            {
                SampleTypes = await _db.SampleType.AsNoTracking()
                    .Where(x => x.Status)
                    .OrderBy(x => x.Name)
                    .Select(x => new LookupItemDto { Id = x.Id, Code = x.Code, Name = x.Name })
                    .ToListAsync(),
                Containers = await _db.Container.AsNoTracking()
                    .Where(x => x.Status)
                    .OrderBy(x => x.Name)
                    .Select(x => new LookupItemDto { Id = x.Id, Code = x.Code, Name = x.Name })
                    .ToListAsync(),
                BottleColors = await _db.BottleColor.AsNoTracking()
                    .Where(x => x.Status)
                    .OrderBy(x => x.Name)
                    .Select(x => new LookupItemDto { Id = x.Id, Code = x.Code, Name = x.Name })
                    .ToListAsync(),
                SampleGroups = await _db.SampleGroup.AsNoTracking()
                    .Where(x => x.Status)
                    .OrderBy(x => x.Name)
                    .Select(x => new LookupItemDto { Id = x.Id, Code = x.Code, Name = x.Name })
                    .ToListAsync(),
                OutsideLabs = await _db.OutsideLab.AsNoTracking()
                    .Where(x => x.Status)
                    .OrderBy(x => x.Name)
                    .Select(x => new OutsideLabDto { Id = x.Id, Name = x.Name, Phone = x.Phone })
                    .ToListAsync(),
                Antibodies = await _db.Antibody.AsNoTracking()
                    .Where(x => x.Status)
                    .OrderBy(x => x.Code)
                    .Select(x => new LookupItemDto { Id = x.Id, Code = x.Code, Name = x.Name })
                    .ToListAsync()
            };

            return Ok(dto);
        }
    }
}
