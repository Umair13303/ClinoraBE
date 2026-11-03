using ClinoraBE.Model.ViewModel;
using Microsoft.EntityFrameworkCore;
using ClinoraBE.Model.DAL;
using System.Security.Claims;

namespace ClinoraBE.Areas.ApplicationConfiguration.Service
{
    public interface ICOMAuthentication
    {
        Task<List<VMMenu>> getVRight_By_RoleId();
    }

    public class ICOMAuthenticationService : ICOMAuthentication
    {
        private readonly ClinoraContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ICOMAuthenticationService(ClinoraContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<List<VMMenu>> getVRight_By_RoleId()
        {
            var user = _httpContextAccessor.HttpContext?.User;

            if (user == null || !user.Identity.IsAuthenticated)
            {
                return new List<VMMenu>();
            }
            var roleIdClaim = user.FindFirst("RoleId") ?? user.FindFirst(ClaimTypes.Role) ?? user.FindFirst("UserTypeId");

            if (roleIdClaim == null || string.IsNullOrEmpty(roleIdClaim.Value))
            {
                return new List<VMMenu>();
            }

            string currentUserRoleId = roleIdClaim.Value;

            var rightList = await _context.VRight.AsNoTracking().ToListAsync();
            return rightList
                .Where(r => !string.IsNullOrEmpty(r.RoleIds) &&
                            r.RoleIds.Split(',').Select(s => s.Trim()).Contains(currentUserRoleId) &&
                            r.Status == true &&
                            r.IsDisplayAllowed == true)
                .GroupBy(r => r.Menu)
                .Select(menuGroup => new VMMenu
                {
                    Menu = menuGroup.Key,
                    SubMenu = menuGroup.GroupBy(r => r.SubMenu).Select(subMenuGroup => new VMSubMenu
                    {
                        SubMenu = subMenuGroup.Key,
                        Rights = subMenuGroup.Select(r => new VMRight
                        {
                            DisplayName = r.DisplayName,
                            Url = r.FormName,
                            OperationType = r.Purpose,
                            Priority = r.Priority
                        }).OrderBy(x => x.Priority).ToList()
                    }).OrderBy(sm => sm.SubMenu).ToList()
                }).ToList();
        }
    }
}
