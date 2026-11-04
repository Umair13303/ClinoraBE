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

        [HttpPost("complete")]
        public async Task<ActionResult> Complete([FromBody] RadiologyCompleteRequest request)
        {
            var line = await _db.VisitService.FirstOrDefaultAsync(vs => vs.Id == request.VisitServiceId);
            if (line == null)
            {
                return NotFound(new { message = "Visit service not found." });
            }

            if (line.Status != VisitServiceStatuses.AwaitingImaging)
            {
                return BadRequest(new { message = "Only awaiting-imaging studies can be marked imaged." });
            }

            var findings = string.IsNullOrWhiteSpace(request.Findings) ? "Imaging completed." : request.Findings.Trim();
            var existing = await _db.TestResult.FirstOrDefaultAsync(r => r.VisitServiceId == line.Id);
            if (existing == null)
            {
                _db.TestResult.Add(new TestResult
                {
                    VisitServiceId = line.Id,
                    Source = "Radiology",
                    ResultText = findings,
                    EnteredByName = string.IsNullOrWhiteSpace(request.PerformedByName) ? null : request.PerformedByName.Trim(),
                    EnteredAt = DateTime.Now
                });
            }
            else
            {
                existing.ResultText = findings;
                existing.EnteredByName = string.IsNullOrWhiteSpace(request.PerformedByName) ? existing.EnteredByName : request.PerformedByName.Trim();
                existing.EnteredAt = DateTime.Now;
                existing.Source = "Radiology";
            }

            line.Status = VisitServiceStatuses.Imaged;
            line.ResultEnteredAt = DateTime.Now;
            await _db.SaveChangesAsync();
            return Ok(new { message = "Imaging marked complete. Ready for radiologist finalize." });
        }
    }
}
