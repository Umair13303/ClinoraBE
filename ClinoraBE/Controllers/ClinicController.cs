using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinoraBE.Controllers
{
    [ApiController]
    [Route("api/clinic")]
    [Authorize]
    public class ClinicController : ControllerBase
    {
        private readonly IConfiguration _config;

        public ClinicController(IConfiguration config)
        {
            _config = config;
        }

        [HttpGet("settings")]
        public ActionResult<ClinicSettingsDto> Settings()
        {
            var roleStations = _config.GetSection("Clinic:RoleStations")
                .GetChildren()
                .ToDictionary(
                    c => c.Key,
                    c => c.GetChildren().Select(x => x.Value ?? string.Empty).Where(x => x.Length > 0).ToList());

            return Ok(new ClinicSettingsDto
            {
                Name = _config["Clinic:Name"] ?? "Clinora Diagnostics",
                Address = _config["Clinic:Address"],
                Phone = _config["Clinic:Phone"],
                RoleStations = roleStations
            });
        }
    }

    public class ClinicSettingsDto
    {
        public string Name { get; set; } = "Clinora Diagnostics";
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public Dictionary<string, List<string>> RoleStations { get; set; } = new();
    }
}
