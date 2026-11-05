using ClinoraBE.Infrastructure;
using ClinoraBE.Model.DAL;
using ClinoraBE.Model.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinoraBE.Controllers
{
    [ApiController]
    [Route("api/patients")]
    [Authorize]
    [StationAuthorize("FrontDesk", "Lab", "Doctor", "Radiology", "Phlebotomy", "Vaccination")]
    public class PatientsController : ControllerBase
    {
        private readonly ClinoraContext _db;

        public PatientsController(ClinoraContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<ActionResult<List<PatientDto>>> Get([FromQuery] string? query)
        {
            var q = (query ?? string.Empty).Trim().ToLowerInvariant();
            if (q.Length < 2)
            {
                return Ok(new List<PatientDto>());
            }

            var patients = await _db.Patient
                .AsNoTracking()
                .Where(p => p.Status)
                .Where(p =>
                    p.FullName.ToLower().Contains(q)
                    || p.Phone.Contains(q)
                    || (p.Cnic != null && p.Cnic.ToLower().Contains(q))
                    || (p.PassportNo != null && p.PassportNo.ToLower().Contains(q))
                    || _db.Visit.Any(v =>
                        v.PatientId == p.Id
                        && (v.VisitNumber.ToLower().Contains(q)
                            || v.VisitNumber.ToLower().Replace("v-", "").Contains(q))))
                .OrderBy(p => p.FullName)
                .Take(20)
                .Select(p => new PatientDto
                {
                    Id = p.Id,
                    FullName = p.FullName,
                    Phone = p.Phone,
                    Age = p.Age,
                    Gender = p.Gender,
                    Cnic = p.Cnic,
                    PassportNo = p.PassportNo,
                    PhotoUrl = p.PhotoPath,
                    IsPanel = p.IsPanel,
                    PanelName = p.PanelName,
                    VisitCount = _db.Visit.Count(v => v.PatientId == p.Id)
                })
                .ToListAsync();

            return Ok(patients);
        }
    }
}
