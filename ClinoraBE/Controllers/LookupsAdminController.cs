using ClinoraBE.Infrastructure;
using ClinoraBE.Model.DAL;
using ClinoraBE.Model.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinoraBE.Controllers
{
    [ApiController]
    [Route("api/admin/lookups")]
    [Authorize]
    [StationAuthorize("Admin")]
    public class LookupsAdminController : ControllerBase
    {
        private readonly ClinoraContext _db;

        public LookupsAdminController(ClinoraContext db)
        {
            _db = db;
        }

        [HttpPost("sample-types")]
        public async Task<ActionResult<LookupItemDto>> UpsertSampleType([FromBody] UpsertLookupRequest request)
        {
            var name = (request.Name ?? string.Empty).Trim();
            var code = (request.Code ?? string.Empty).Trim().ToUpperInvariant();
            if (name.Length < 2 || code.Length < 1)
            {
                return BadRequest(new { message = "Code and name are required." });
            }

            SampleType entity;
            if (request.Id is int id)
            {
                entity = await _db.SampleType.FirstOrDefaultAsync(x => x.Id == id);
                if (entity == null)
                {
                    return NotFound(new { message = "Sample type not found." });
                }
                entity.Code = code;
                entity.Name = name;
                entity.Status = request.Status;
            }
            else
            {
                entity = new SampleType { Code = code, Name = name, Status = request.Status };
                _db.SampleType.Add(entity);
            }

            await _db.SaveChangesAsync();
            return Ok(new LookupItemDto { Id = entity.Id, Code = entity.Code, Name = entity.Name });
        }

        [HttpPost("containers")]
        public async Task<ActionResult<LookupItemDto>> UpsertContainer([FromBody] UpsertLookupRequest request)
        {
            var name = (request.Name ?? string.Empty).Trim();
            var code = (request.Code ?? string.Empty).Trim().ToUpperInvariant();
            if (name.Length < 2 || code.Length < 1)
            {
                return BadRequest(new { message = "Code and name are required." });
            }

            Container entity;
            if (request.Id is int id)
            {
                entity = await _db.Container.FirstOrDefaultAsync(x => x.Id == id);
                if (entity == null)
                {
                    return NotFound(new { message = "Container not found." });
                }
                entity.Code = code;
                entity.Name = name;
                entity.Status = request.Status;
            }
            else
            {
                entity = new Container { Code = code, Name = name, Status = request.Status };
                _db.Container.Add(entity);
            }

            await _db.SaveChangesAsync();
            return Ok(new LookupItemDto { Id = entity.Id, Code = entity.Code, Name = entity.Name });
        }

        [HttpPost("bottle-colors")]
        public async Task<ActionResult<LookupItemDto>> UpsertBottleColor([FromBody] UpsertLookupRequest request)
        {
            var name = (request.Name ?? string.Empty).Trim();
            var code = (request.Code ?? string.Empty).Trim().ToUpperInvariant();
            if (name.Length < 2 || code.Length < 1)
            {
                return BadRequest(new { message = "Code and name are required." });
            }

            BottleColor entity;
            if (request.Id is int id)
            {
                entity = await _db.BottleColor.FirstOrDefaultAsync(x => x.Id == id);
                if (entity == null)
                {
                    return NotFound(new { message = "Bottle color not found." });
                }
                entity.Code = code;
                entity.Name = name;
                entity.Status = request.Status;
            }
            else
            {
                entity = new BottleColor { Code = code, Name = name, Status = request.Status };
                _db.BottleColor.Add(entity);
            }

            await _db.SaveChangesAsync();
            return Ok(new LookupItemDto { Id = entity.Id, Code = entity.Code, Name = entity.Name });
        }

        [HttpPost("sample-groups")]
        public async Task<ActionResult<LookupItemDto>> UpsertSampleGroup([FromBody] UpsertLookupRequest request)
        {
            var name = (request.Name ?? string.Empty).Trim();
            var code = (request.Code ?? string.Empty).Trim().ToUpperInvariant();
            if (name.Length < 2 || code.Length < 1)
            {
                return BadRequest(new { message = "Code and name are required." });
            }

            SampleGroup entity;
            if (request.Id is int id)
            {
                entity = await _db.SampleGroup.FirstOrDefaultAsync(x => x.Id == id);
                if (entity == null)
                {
                    return NotFound(new { message = "Sample group not found." });
                }
                entity.Code = code;
                entity.Name = name;
                entity.Status = request.Status;
            }
            else
            {
                entity = new SampleGroup { Code = code, Name = name, Status = request.Status };
                _db.SampleGroup.Add(entity);
            }

            await _db.SaveChangesAsync();
            return Ok(new LookupItemDto { Id = entity.Id, Code = entity.Code, Name = entity.Name });
        }

        [HttpPost("antibodies")]
        public async Task<ActionResult<LookupItemDto>> UpsertAntibody([FromBody] UpsertLookupRequest request)
        {
            var name = (request.Name ?? string.Empty).Trim();
            var code = (request.Code ?? string.Empty).Trim().ToUpperInvariant();
            if (name.Length < 2 || code.Length < 1)
            {
                return BadRequest(new { message = "Code and name are required." });
            }

            Antibody entity;
            if (request.Id is int id)
            {
                entity = await _db.Antibody.FirstOrDefaultAsync(x => x.Id == id);
                if (entity == null)
                {
                    return NotFound(new { message = "Antibody not found." });
                }
                entity.Code = code;
                entity.Name = name;
                entity.Status = request.Status;
            }
            else
            {
                entity = new Antibody { Code = code, Name = name, Status = request.Status };
                _db.Antibody.Add(entity);
            }

            await _db.SaveChangesAsync();
            return Ok(new LookupItemDto { Id = entity.Id, Code = entity.Code, Name = entity.Name });
        }

        [HttpPost("outside-labs")]
        public async Task<ActionResult<OutsideLabDto>> UpsertOutsideLab([FromBody] UpsertOutsideLabRequest request)
        {
            var name = (request.Name ?? string.Empty).Trim();
            if (name.Length < 2)
            {
                return BadRequest(new { message = "Outside lab name is required." });
            }

            OutsideLab entity;
            if (request.Id is int id)
            {
                entity = await _db.OutsideLab.FirstOrDefaultAsync(x => x.Id == id);
                if (entity == null)
                {
                    return NotFound(new { message = "Outside lab not found." });
                }
                entity.Name = name;
                entity.Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim();
                entity.Address = string.IsNullOrWhiteSpace(request.Address) ? null : request.Address.Trim();
                entity.Status = request.Status;
            }
            else
            {
                entity = new OutsideLab
                {
                    Name = name,
                    Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim(),
                    Address = string.IsNullOrWhiteSpace(request.Address) ? null : request.Address.Trim(),
                    Status = request.Status
                };
                _db.OutsideLab.Add(entity);
            }

            await _db.SaveChangesAsync();
            return Ok(new OutsideLabDto { Id = entity.Id, Name = entity.Name, Phone = entity.Phone });
        }
    }

    public class UpsertLookupRequest
    {
        public int? Id { get; set; }
        public string? Code { get; set; }
        public string? Name { get; set; }
        public bool Status { get; set; } = true;
    }

    public class UpsertOutsideLabRequest
    {
        public int? Id { get; set; }
        public string? Name { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public bool Status { get; set; } = true;
    }
}
