using ClinoraBE.Model.DAL;
using ClinoraBE.Model.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinoraBE.Controllers
{
    [ApiController]
    [Route("api/results")]
    [Authorize]
    public class ResultsController : ControllerBase
    {
        private readonly ClinoraContext _db;

        public ResultsController(ClinoraContext db)
        {
            _db = db;
        }

        [HttpPost]
        public async Task<ActionResult> Save([FromBody] SaveResultRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.ResultText))
            {
                return BadRequest(new { message = "Enter the result." });
            }

            var line = await _db.VisitService.FirstOrDefaultAsync(vs => vs.Id == request.VisitServiceId);
            if (line == null)
            {
                return NotFound(new { message = "Visit service not found." });
            }

            if (line.Status is not (VisitServiceStatuses.InLab or VisitServiceStatuses.ResultEntered))
            {
                return BadRequest(new { message = "This test is not ready for result entry." });
            }

            var existing = await _db.TestResult.FirstOrDefaultAsync(r => r.VisitServiceId == line.Id);
            if (existing == null)
            {
                _db.TestResult.Add(new TestResult
                {
                    VisitServiceId = line.Id,
                    Source = string.IsNullOrWhiteSpace(request.Source) ? "InHouse" : request.Source.Trim(),
                    ResultText = request.ResultText.Trim(),
                    Units = string.IsNullOrWhiteSpace(request.Units) ? null : request.Units.Trim(),
                    ReferenceRange = string.IsNullOrWhiteSpace(request.ReferenceRange) ? null : request.ReferenceRange.Trim(),
                    EnteredByName = string.IsNullOrWhiteSpace(request.EnteredByName) ? null : request.EnteredByName.Trim(),
                    EnteredAt = DateTime.Now
                });
            }
            else
            {
                existing.ResultText = request.ResultText.Trim();
                existing.Units = string.IsNullOrWhiteSpace(request.Units) ? null : request.Units.Trim();
                existing.ReferenceRange = string.IsNullOrWhiteSpace(request.ReferenceRange) ? null : request.ReferenceRange.Trim();
                existing.EnteredByName = string.IsNullOrWhiteSpace(request.EnteredByName) ? null : request.EnteredByName.Trim();
                existing.EnteredAt = DateTime.Now;
                existing.Source = string.IsNullOrWhiteSpace(request.Source) ? existing.Source : request.Source.Trim();
            }

            line.Status = VisitServiceStatuses.ResultEntered;
            line.ResultEnteredAt = DateTime.Now;
            await _db.SaveChangesAsync();
            return Ok(new { message = "Result saved." });
        }
    }
}
