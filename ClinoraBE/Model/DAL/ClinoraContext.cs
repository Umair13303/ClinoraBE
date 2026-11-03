using ClinoraBE.Model.Enum;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;

namespace ClinoraBE.Model.DAL
{
    public partial class ClinoraContext : DbContext
    {
        public ClinoraContext(DbContextOptions<ClinoraContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Company> Company { get; set; }
        public virtual DbSet<Branch> Branch { get; set; }
        public virtual DbSet<User> User { get; set; }
        public virtual DbSet<VRight> VRight { get; set; }
        public virtual DbSet<VRole> VRole { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<VRight>(entity =>
            {
                entity.ToTable("vRight");
            });

            modelBuilder.Entity<VRole>(entity =>
            {
                entity.ToTable("vRole");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }

    public partial class Company
    {
        public int Id { get; set; }
        public DateTime? CreatedOn { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public int? UpdatedBy { get; set; }
        public bool? Status { get; set; }
        public string? Description { get; set; }
        public string? Contact { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? Logo { get; set; }
        public string? Website { get; set; }
        public string? PatientPortal { get; set; }
        public string? PanelPortal { get; set; }
    }

    public partial class Branch
    {
        public int Id { get; set; }
        public DateTime? CreatedOn { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public int? UpdatedBy { get; set; }
        public bool? Status { get; set; }
        public int? CompanyId { get; set; }
        public string? Description { get; set; }
        public string? Contact { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? NTN { get; set; }
    }

    public partial class User
    {
        public int Id { get; set; }
        public DateTime? CreatedOn { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public int? UpdatedBy { get; set; }
        public bool? Status { get; set; }
        public int? BranchId { get; set; }
        public int? CompanyId { get; set; }
        public int UserTypeId { get; set; } = (int)UserType.WebUser; 
        public string? Description { get; set; }
        public string? Password { get; set; }
        public string? Contact { get; set; }
        public string? Email { get; set; }
        public int? EmployeeId { get; set; }
    }

    public partial class VRight
    {
        public int Id { get; set; }
        public string? Menu { get; set; }
        public string? SubMenu { get; set; }
        public string? FormName { get; set; }
        public string? DisplayName { get; set; }
        public string? Purpose { get; set; }
        public string? RoleIds { get; set; }
        public int Priority { get; set; } = 0;
        public bool IsDisplayAllowed { get; set; } = true;
        public bool Status { get; set; } = true;
    }

    public partial class VRole
    {
        public int Id { get; set; }
        public string? Description { get; set; }
        public bool Status { get; set; } = true;
    }
}