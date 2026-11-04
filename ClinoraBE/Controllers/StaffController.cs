using ClinoraBE.Model.DAL;
using ClinoraBE.Model.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinoraBE.Controllers
{
    [ApiController]
    [Route("api/staff")]
    [Authorize]
    public class StaffController : ControllerBase
    {
        private readonly ClinoraContext _db;

        public StaffController(ClinoraContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<ActionResult<List<StaffDto>>> Get([FromQuery] string? query)
        {
            var q = (query ?? string.Empty).Trim().ToLowerInvariant();
            var staff = await _db.Staff
                .AsNoTracking()
                .Where(s => s.Status)
                .Where(s => q == string.Empty
                    || s.Name.ToLower().Contains(q)
                    || (s.Department != null && s.Department.ToLower().Contains(q)))
                .OrderBy(s => s.Name)
                .Take(50)
                .Select(s => new StaffDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    Department = s.Department
                })
                .ToListAsync();

            return Ok(staff);
        }
    }
}
