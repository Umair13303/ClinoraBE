using ClinoraBE.Model.DAL;
using ClinoraBE.Model.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ClinoraBE.Controllers
{
    [ApiController]
    [Route("api/outside-lab")]
    [Authorize]
    public class OutsideLabController : ControllerBase
    {
        private readonly ClinoraContext _db;

        public OutsideLabController(ClinoraContext db)
        {
            _db = db;
        }

        [HttpGet("shipments")]
        public async Task<ActionResult<List<ShipmentDto>>> List()
        {
            var shipments = await _db.OutsideLabShipment.AsNoTracking()
                .OrderByDescending(s => s.SentAt)
                .Take(40)
                .ToListAsync();

            var result = new List<ShipmentDto>();
            foreach (var shipment in shipments)
            {
                result.Add(await BuildShipmentDto(shipment.Id) ?? new ShipmentDto { Id = shipment.Id });
            }

            return Ok(result);
        }

        [HttpPost("shipments")]
        public async Task<ActionResult<ShipmentDto>> Create([FromBody] CreateShipmentRequest request)
        {
            if (request.SampleIds == null || request.SampleIds.Count == 0)
            {
                return BadRequest(new { message = "Select at least one sample to ship." });
            }

            var lab = await _db.OutsideLab.FirstOrDefaultAsync(l => l.Id == request.OutsideLabId && l.Status);
            if (lab == null)
            {
                return BadRequest(new { message = "Outside lab not found." });
            }

            var samples = await _db.LabSample
                .Where(s => request.SampleIds.Contains(s.Id) && s.IsSendOut)
                .ToListAsync();

            if (samples.Count != request.SampleIds.Count)
            {
                return BadRequest(new { message = "One or more send-out samples were not found." });
            }

            var alreadyShipped = await _db.OutsideLabShipmentItem
                .AsNoTracking()
                .Where(i => request.SampleIds.Contains(i.SampleId))
                .Select(i => i.SampleId)
                .Distinct()
                .ToListAsync();
            if (alreadyShipped.Count > 0)
            {
                return BadRequest(new { message = "One or more samples are already on a shipment." });
            }

            var sentBy = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var uid) ? uid : (int?)null;
            var shipment = new OutsideLabShipment
            {
                OutsideLabId = request.OutsideLabId,
                Status = "Sent",
                Awb = string.IsNullOrWhiteSpace(request.Awb) ? null : request.Awb.Trim(),
                Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
                SentAt = DateTime.Now,
                SentBy = sentBy
            };
            _db.OutsideLabShipment.Add(shipment);
            await _db.SaveChangesAsync();

            foreach (var sample in samples)
            {
                sample.OutsideLabId = request.OutsideLabId;
                var visitServices = await _db.VisitService.Where(vs => vs.SampleId == sample.Id).ToListAsync();
                foreach (var vs in visitServices)
                {
                    vs.Status = VisitServiceStatuses.SentOut;
                    vs.IsSendOut = true;
                    vs.OutsideLabId = request.OutsideLabId;
                    _db.OutsideLabShipmentItem.Add(new OutsideLabShipmentItem
                    {
                        ShipmentId = shipment.Id,
                        SampleId = sample.Id,
                        VisitServiceId = vs.Id
                    });
                }
            }

            await _db.SaveChangesAsync();
            return Ok(await BuildShipmentDto(shipment.Id));
        }

        [HttpPost("shipments/{id:int}/receive")]
        public async Task<ActionResult<ShipmentDto>> Receive(int id, [FromBody] ReceiveShipmentRequest request)
        {
            var shipment = await _db.OutsideLabShipment.FirstOrDefaultAsync(s => s.Id == id);
            if (shipment == null)
            {
                return NotFound(new { message = "Shipment not found." });
            }

            if (shipment.Status == "Received")
            {
                return BadRequest(new { message = "Shipment already received." });
            }

            var items = await _db.OutsideLabShipmentItem.Where(i => i.ShipmentId == id).ToListAsync();
            var resultsByVs = (request.Items ?? []).ToDictionary(i => i.VisitServiceId);

            foreach (var item in items)
            {
                var line = await _db.VisitService.FirstAsync(vs => vs.Id == item.VisitServiceId);
                if (resultsByVs.TryGetValue(item.VisitServiceId, out var incoming) && !string.IsNullOrWhiteSpace(incoming.ResultText))
                {
                    var existing = await _db.TestResult.FirstOrDefaultAsync(r => r.VisitServiceId == line.Id);
                    if (existing == null)
                    {
                        _db.TestResult.Add(new TestResult
                        {
                            VisitServiceId = line.Id,
                            Source = "Outside",
                            ResultText = incoming.ResultText.Trim(),
                            Units = string.IsNullOrWhiteSpace(incoming.Units) ? null : incoming.Units.Trim(),
                            ReferenceRange = string.IsNullOrWhiteSpace(incoming.ReferenceRange) ? null : incoming.ReferenceRange.Trim(),
                            EnteredByName = "Outside lab receive",
                            EnteredAt = DateTime.Now
                        });
                    }
                    else
                    {
                        existing.ResultText = incoming.ResultText.Trim();
                        existing.Units = string.IsNullOrWhiteSpace(incoming.Units) ? null : incoming.Units.Trim();
                        existing.ReferenceRange = string.IsNullOrWhiteSpace(incoming.ReferenceRange) ? null : incoming.ReferenceRange.Trim();
                        existing.Source = "Outside";
                        existing.EnteredAt = DateTime.Now;
                    }

                    line.Status = VisitServiceStatuses.ResultEntered;
                    line.ResultEnteredAt = DateTime.Now;
                }
                else
                {
                    // Received without values → ready for local result entry
                    line.Status = VisitServiceStatuses.InLab;
                }
            }

            shipment.Status = "Received";
            shipment.ReceivedAt = DateTime.Now;
            if (!string.IsNullOrWhiteSpace(request.Notes))
            {
                shipment.Notes = string.IsNullOrWhiteSpace(shipment.Notes)
                    ? request.Notes.Trim()
                    : $"{shipment.Notes} | {request.Notes.Trim()}";
            }

            await _db.SaveChangesAsync();
            return Ok(await BuildShipmentDto(id));
        }

        private async Task<ShipmentDto?> BuildShipmentDto(int id)
        {
            var shipment = await _db.OutsideLabShipment.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);
            if (shipment == null)
            {
                return null;
            }

            var lab = await _db.OutsideLab.AsNoTracking().FirstAsync(l => l.Id == shipment.OutsideLabId);
            var items = await (
                from i in _db.OutsideLabShipmentItem.AsNoTracking()
                join s in _db.LabSample.AsNoTracking() on i.SampleId equals s.Id
                join vs in _db.VisitService.AsNoTracking() on i.VisitServiceId equals vs.Id
                join v in _db.Visit.AsNoTracking() on vs.VisitId equals v.Id
                join p in _db.Patient.AsNoTracking() on v.PatientId equals p.Id
                join svc in _db.Service.AsNoTracking() on vs.ServiceId equals svc.Id
                where i.ShipmentId == id
                select new ShipmentItemDto
                {
                    SampleId = s.Id,
                    Barcode = s.Barcode,
                    VisitServiceId = vs.Id,
                    VisitNumber = v.VisitNumber,
                    PatientName = p.FullName,
                    ServiceName = svc.Name
                }
            ).ToListAsync();

            return new ShipmentDto
            {
                Id = shipment.Id,
                OutsideLabId = shipment.OutsideLabId,
                OutsideLabName = lab.Name,
                Status = shipment.Status,
                Awb = shipment.Awb,
                Notes = shipment.Notes,
                SentAt = shipment.SentAt,
                ReceivedAt = shipment.ReceivedAt,
                Items = items
            };
        }
    }
}
