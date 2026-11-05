using ClinoraBE.Infrastructure;
using ClinoraBE.Model.DAL;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinoraBE.Controllers
{
    [ApiController]
    [Route("api/branches")]
    [Authorize]
    [StationAuthorize("Admin")]
    public class BranchesController : ControllerBase
    {
        private readonly ClinoraContext _db;

        public BranchesController(ClinoraContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<ActionResult<List<BranchDto>>> Get()
        {
            var branches = await _db.Branch.AsNoTracking()
                .Where(b => b.Status == true)
                .OrderBy(b => b.Description)
                .Select(b => new BranchDto
                {
                    Id = b.Id,
                    Code = b.Code ?? ("BR" + b.Id),
                    Name = b.Description ?? ("Branch " + b.Id),
                    Contact = b.Contact,
                    Address = b.Address
                })
                .ToListAsync();

            return Ok(branches);
        }
    }

    public class BranchDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Contact { get; set; }
        public string? Address { get; set; }
    }
}
