using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ClinoraBE.Infrastructure
{
    /// <summary>
    /// Requires the caller's role to include at least one of the listed station keys
    /// from Clinic:RoleStations (Admin always passes).
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class StationAuthorizeAttribute : Attribute, IAuthorizationFilter
    {
        private readonly string[] _stations;

        public StationAuthorizeAttribute(params string[] stations)
        {
            _stations = stations ?? [];
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            if (_stations.Length == 0)
            {
                return;
            }

            var user = context.HttpContext.User;
            if (user?.Identity?.IsAuthenticated != true)
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            var roleId = user.FindFirstValue("RoleId")
                         ?? user.FindFirstValue(ClaimTypes.Role)
                         ?? user.FindFirstValue("UserTypeId");

            var config = context.HttpContext.RequestServices.GetService(typeof(IConfiguration)) as IConfiguration;
            var map = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            if (config != null)
            {
                foreach (var child in config.GetSection("Clinic:RoleStations").GetChildren())
                {
                    map[child.Key] = child.Get<List<string>>() ?? [];
                }
            }

            var allowed = ResolveStations(map, roleId);
            if (allowed.Any(s => s.Equals("Admin", StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            if (_stations.Any(required => allowed.Any(a => a.Equals(required, StringComparison.OrdinalIgnoreCase))))
            {
                return;
            }

            context.Result = new ObjectResult(new { message = "This station is not allowed for your role." })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }

        private static List<string> ResolveStations(Dictionary<string, List<string>> map, string? roleId)
        {
            if (!string.IsNullOrWhiteSpace(roleId) && map.TryGetValue(roleId.Trim(), out var exact))
            {
                return exact;
            }

            return map.TryGetValue("default", out var fallback) ? fallback : [];
        }
    }
}
