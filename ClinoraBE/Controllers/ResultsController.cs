using ClinoraBE.Infrastructure;

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
    [StationAuthorize("Lab")]
    public class ResultsController : ControllerBase
    {
        private readonly ClinoraContext _db;

        public ResultsController(ClinoraContext db)
        {
            _db = db;
        }

        [HttpGet("parameters/{serviceId:int}")]
        public async Task<ActionResult<List<ServiceParameterLiteDto>>> Parameters(int serviceId)
        {
            var list = await _db.ServiceParameter.AsNoTracking()
                .Where(p => p.ServiceId == serviceId && p.Status)
                .OrderBy(p => p.SortOrder)
                .Select(p => new ServiceParameterLiteDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Unit = p.Unit,
                    RefLow = p.RefLow,
                    RefHigh = p.RefHigh
                })
                .ToListAsync();
            return Ok(list);
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

            var visit = await _db.Visit.AsNoTracking().FirstAsync(v => v.Id == line.VisitId);
            if (visit.VisitStatus == VisitStatuses.Cancelled)
            {
                return BadRequest(new { message = "Visit is cancelled." });
            }

            var branchId = BranchContext.ResolveBranchId(Request, User);
            if (!BranchContext.Matches(visit.BranchId, branchId))
            {
                return NotFound(new { message = "Visit service not found for this branch." });
            }

            var amending = line.Status == VisitServiceStatuses.Finalized;
            if (amending && !request.Amend)
            {
                return BadRequest(new { message = "Set amend=true to change a finalized result." });
            }

            if (!amending && line.Status is not (VisitServiceStatuses.InLab or VisitServiceStatuses.ResultEntered))
            {
                return BadRequest(new { message = "This test is not ready for result entry." });
            }

            var parameterName = string.IsNullOrWhiteSpace(request.ParameterName)
                ? null
                : request.ParameterName.Trim();

            var currents = await _db.TestResult
                .Where(r => r.VisitServiceId == line.Id && r.IsCurrent)
                .OrderByDescending(r => r.Version)
                .ToListAsync();

            var current = FindCurrentForParameter(currents, parameterName);

            var units = !string.IsNullOrWhiteSpace(request.Units)
                ? request.Units.Trim()
                : current?.Units;
            var reference = !string.IsNullOrWhiteSpace(request.ReferenceRange)
                ? request.ReferenceRange.Trim()
                : current?.ReferenceRange;

            // When switching a lone unnamed blob onto a named parameter, keep editing that row.
            if (current != null && parameterName != null && string.IsNullOrWhiteSpace(current.ParameterName))
            {
                current.ParameterName = parameterName;
            }

            var (abnormal, critical) = EvaluateFlags(request.ResultText, request.RefLow, request.RefHigh, request.IsCritical);

            if (amending || current == null)
            {
                if (current != null)
                {
                    current.IsCurrent = false;
                }

                _db.TestResult.Add(new TestResult
                {
                    VisitServiceId = line.Id,
                    Source = amending ? "Amend" : (string.IsNullOrWhiteSpace(request.Source) ? "InHouse" : request.Source.Trim()),
                    ResultText = request.ResultText.Trim(),
                    Units = units,
                    ReferenceRange = reference,
                    EnteredByName = string.IsNullOrWhiteSpace(request.EnteredByName) ? null : request.EnteredByName.Trim(),
                    EnteredAt = DateTime.Now,
                    Version = current == null ? 1 : current.Version + 1,
                    IsCurrent = true,
                    IsAbnormal = abnormal,
                    IsCritical = critical,
                    ParameterName = parameterName
                });
            }
            else
            {
                current.ResultText = request.ResultText.Trim();
                current.Units = units;
                current.ReferenceRange = reference;
                current.EnteredByName = string.IsNullOrWhiteSpace(request.EnteredByName) ? current.EnteredByName : request.EnteredByName.Trim();
                current.EnteredAt = DateTime.Now;
                current.IsAbnormal = abnormal;
                current.IsCritical = critical;
                current.ParameterName = parameterName ?? current.ParameterName;
            }

            await _db.SaveChangesAsync();

            var allCurrent = await _db.TestResult
                .Where(r => r.VisitServiceId == line.Id && r.IsCurrent)
                .ToListAsync();
            line.IsAbnormal = allCurrent.Any(r => r.IsAbnormal);
            line.IsCritical = allCurrent.Any(r => r.IsCritical);
            line.Status = VisitServiceStatuses.ResultEntered;
            line.ResultEnteredAt = DateTime.Now;
            if (amending)
            {
                line.FinalizedAt = null;
                line.FinalizedByName = null;
            }

            await _db.SaveChangesAsync();
            return Ok(new
            {
                message = amending ? "Amendment saved. Finalize again." : "Result saved.",
                isAbnormal = abnormal,
                isCritical = critical
            });
        }

        private static TestResult? FindCurrentForParameter(List<TestResult> currents, string? parameterName)
        {
            if (parameterName != null)
            {
                return currents.FirstOrDefault(r =>
                           string.Equals(r.ParameterName, parameterName, StringComparison.OrdinalIgnoreCase))
                       ?? currents.FirstOrDefault(r => string.IsNullOrWhiteSpace(r.ParameterName) && currents.Count == 1);
            }

            return currents.FirstOrDefault(r => string.IsNullOrWhiteSpace(r.ParameterName))
                   ?? (currents.Count == 1 ? currents[0] : null);
        }

        private static (bool Abnormal, bool Critical) EvaluateFlags(string text, decimal? low, decimal? high, bool forceCritical)
        {
            if (forceCritical)
            {
                return (true, true);
            }

            if (low is null && high is null)
            {
                return (false, false);
            }

            if (!decimal.TryParse(text.Trim(), System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var value))
            {
                return (false, false);
            }

            var abnormal = (low is not null && value < low) || (high is not null && value > high);
            return (abnormal, false);
        }
    }

    public class ServiceParameterLiteDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Unit { get; set; }
        public decimal? RefLow { get; set; }
        public decimal? RefHigh { get; set; }
    }
}
