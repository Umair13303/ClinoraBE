namespace ClinoraBE.Model.Security
{

    public class AuthenticationPayLoad
    {
        public string Token { get; set; } = string.Empty;
        public int UserId { get; set; }
        public string? Email { get; set; }
        public string? Name { get; set; }
        public int? BranchId { get; set; }
        public int? CompanyId { get; set; }
        public int UserTypeId { get; set; }
        public int RoleId { get; set; }
    }
}
