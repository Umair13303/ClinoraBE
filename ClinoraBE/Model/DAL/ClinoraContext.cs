using ClinoraBE.Model.Enum;
using Microsoft.EntityFrameworkCore;

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
        public virtual DbSet<Service> Service { get; set; }
        public virtual DbSet<Staff> Staff { get; set; }
        public virtual DbSet<Patient> Patient { get; set; }
        public virtual DbSet<Visit> Visit { get; set; }
        public virtual DbSet<VisitService> VisitService { get; set; }
        public virtual DbSet<BottleColor> BottleColor { get; set; }
        public virtual DbSet<SampleType> SampleType { get; set; }
        public virtual DbSet<SampleGroup> SampleGroup { get; set; }
        public virtual DbSet<Container> Container { get; set; }
        public virtual DbSet<OutsideLab> OutsideLab { get; set; }
        public virtual DbSet<Antibody> Antibody { get; set; }
        public virtual DbSet<LabSample> LabSample { get; set; }
        public virtual DbSet<SampleTest> SampleTest { get; set; }
        public virtual DbSet<OutsideLabShipment> OutsideLabShipment { get; set; }
        public virtual DbSet<OutsideLabShipmentItem> OutsideLabShipmentItem { get; set; }
        public virtual DbSet<TestResult> TestResult { get; set; }
        public virtual DbSet<Report> Report { get; set; }
        public virtual DbSet<ReportLine> ReportLine { get; set; }
        public virtual DbSet<RadiologyTemplate> RadiologyTemplate { get; set; }
        public virtual DbSet<RadiologyTemplateService> RadiologyTemplateService { get; set; }
        public virtual DbSet<ServiceParameter> ServiceParameter { get; set; }

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

            modelBuilder.Entity<Service>(entity =>
            {
                entity.ToTable("Service");
                entity.Property(e => e.Fee).HasColumnType("decimal(18,2)");
            });

            modelBuilder.Entity<Staff>(entity =>
            {
                entity.ToTable("Staff");
            });

            modelBuilder.Entity<Patient>(entity =>
            {
                entity.ToTable("Patient");
            });

            modelBuilder.Entity<Visit>(entity =>
            {
                entity.ToTable("Visit");
                entity.Property(e => e.PercentOff).HasColumnType("decimal(18,2)");
                entity.Property(e => e.VisitOff).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Fees).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Discounts).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Net).HasColumnType("decimal(18,2)");
                entity.Property(e => e.AmountReceived).HasColumnType("decimal(18,2)");
                entity.HasOne<Patient>()
                    .WithMany()
                    .HasForeignKey(e => e.PatientId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<VisitService>(entity =>
            {
                entity.ToTable("VisitService");
                entity.Property(e => e.Fee).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Discount).HasColumnType("decimal(18,2)");
                entity.HasOne<Visit>()
                    .WithMany()
                    .HasForeignKey(e => e.VisitId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne<Service>()
                    .WithMany()
                    .HasForeignKey(e => e.ServiceId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<BottleColor>().ToTable("BottleColor");
            modelBuilder.Entity<SampleType>().ToTable("SampleType");
            modelBuilder.Entity<SampleGroup>().ToTable("SampleGroup");
            modelBuilder.Entity<Container>().ToTable("Container");
            modelBuilder.Entity<OutsideLab>().ToTable("OutsideLab");
            modelBuilder.Entity<Antibody>().ToTable("Antibody");
            modelBuilder.Entity<LabSample>().ToTable("LabSample");
            modelBuilder.Entity<SampleTest>().ToTable("SampleTest");
            modelBuilder.Entity<OutsideLabShipment>().ToTable("OutsideLabShipment");
            modelBuilder.Entity<OutsideLabShipmentItem>().ToTable("OutsideLabShipmentItem");
            modelBuilder.Entity<TestResult>().ToTable("TestResult");
            modelBuilder.Entity<Report>().ToTable("Report");
            modelBuilder.Entity<ReportLine>().ToTable("ReportLine");
            modelBuilder.Entity<RadiologyTemplate>().ToTable("RadiologyTemplate");
            modelBuilder.Entity<RadiologyTemplateService>(entity =>
            {
                entity.ToTable("RadiologyTemplateService");
                entity.HasIndex(e => new { e.TemplateId, e.ServiceId }).IsUnique();
            });
            modelBuilder.Entity<ServiceParameter>(entity =>
            {
                entity.ToTable("ServiceParameter");
                entity.Property(e => e.RefLow).HasColumnType("decimal(18,4)");
                entity.Property(e => e.RefHigh).HasColumnType("decimal(18,4)");
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
        public string? Code { get; set; }
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
        public int RoleId { get; set; }
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

    public partial class Service
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public decimal Fee { get; set; }
        public int NormalMinutes { get; set; }
        public int UrgentMinutes { get; set; }
        public string Pathway { get; set; } = "Laboratory";
        public string ReportProfile { get; set; } = "Standard";
        public int? SampleTypeId { get; set; }
        public int? ContainerId { get; set; }
        public int? BottleColorId { get; set; }
        public int? SampleGroupId { get; set; }
        public bool CanSendOut { get; set; }
        public int? DefaultOutsideLabId { get; set; }
        public bool Status { get; set; } = true;
        public DateTime? CreatedOn { get; set; }
    }

    public partial class Staff
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Department { get; set; }
        public bool Status { get; set; } = true;
        public int? BranchId { get; set; }
        public int? CompanyId { get; set; }
        public DateTime? CreatedOn { get; set; }
    }

    public partial class Patient
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public int Age { get; set; }
        public string Gender { get; set; } = string.Empty;
        public string? Cnic { get; set; }
        public string? PassportNo { get; set; }
        public string? PhotoPath { get; set; }
        public bool IsPanel { get; set; }
        public string? PanelName { get; set; }
        public bool Status { get; set; } = true;
        public int? BranchId { get; set; }
        public int? CompanyId { get; set; }
        public DateTime? CreatedOn { get; set; }
        public int? CreatedBy { get; set; }
    }

    public partial class Visit
    {
        public int Id { get; set; }
        public string VisitNumber { get; set; } = string.Empty;
        public int PatientId { get; set; }
        public int? TokenNo { get; set; }
        public string VisitStatus { get; set; } = "Active";
        public decimal PercentOff { get; set; }
        public decimal VisitOff { get; set; }
        public decimal Fees { get; set; }
        public decimal Discounts { get; set; }
        public decimal Net { get; set; }
        public decimal AmountReceived { get; set; }
        public bool PaymentReceived { get; set; }
        public bool IsReferred { get; set; }
        public string? ReferredBy { get; set; }
        public string? SlipNo { get; set; }
        public string? Notes { get; set; }
        public int? BranchId { get; set; }
        public int? CompanyId { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public partial class VisitService
    {
        public int Id { get; set; }
        public int VisitId { get; set; }
        public int ServiceId { get; set; }
        public decimal Fee { get; set; }
        public decimal Discount { get; set; }
        public bool Urgent { get; set; }
        public int? StaffId { get; set; }
        public string? StaffName { get; set; }
        public string? ReadyBy { get; set; }
        public string Status { get; set; } = "AwaitingCollection";
        public int? SampleId { get; set; }
        public bool IsSendOut { get; set; }
        public int? OutsideLabId { get; set; }
        public DateTime? ResultEnteredAt { get; set; }
        public DateTime? FinalizedAt { get; set; }
        public string? FinalizedByName { get; set; }
        public string? RejectReason { get; set; }
        public DateTime? RejectedAt { get; set; }
        public string? RejectedByName { get; set; }
        public bool IsAbnormal { get; set; }
        public bool IsCritical { get; set; }
    }
}
