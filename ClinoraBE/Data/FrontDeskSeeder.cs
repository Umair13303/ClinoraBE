using ClinoraBE.Model.DAL;
using Microsoft.EntityFrameworkCore;

namespace ClinoraBE.Data
{
    public static class FrontDeskSeeder
    {
        public static async Task EnsureSeedAsync(ClinoraContext db)
        {
            await db.Database.ExecuteSqlRawAsync("""
                IF OBJECT_ID('dbo.Service','U') IS NULL
                CREATE TABLE dbo.Service (
                  Id INT IDENTITY(1,1) PRIMARY KEY,
                  Name NVARCHAR(200) NOT NULL,
                  Category NVARCHAR(100) NOT NULL,
                  Fee DECIMAL(18,2) NOT NULL,
                  NormalMinutes INT NOT NULL DEFAULT 60,
                  UrgentMinutes INT NOT NULL DEFAULT 30,
                  Status BIT NOT NULL DEFAULT 1,
                  CreatedOn DATETIME2 NULL
                );

                IF COL_LENGTH('dbo.Service', 'NormalMinutes') IS NULL
                  ALTER TABLE dbo.Service ADD NormalMinutes INT NOT NULL DEFAULT 60;
                IF COL_LENGTH('dbo.Service', 'UrgentMinutes') IS NULL
                  ALTER TABLE dbo.Service ADD UrgentMinutes INT NOT NULL DEFAULT 30;

                IF OBJECT_ID('dbo.Staff','U') IS NULL
                CREATE TABLE dbo.Staff (
                  Id INT IDENTITY(1,1) PRIMARY KEY,
                  Name NVARCHAR(200) NOT NULL,
                  Department NVARCHAR(100) NULL,
                  Status BIT NOT NULL DEFAULT 1,
                  BranchId INT NULL,
                  CompanyId INT NULL,
                  CreatedOn DATETIME2 NULL
                );

                IF OBJECT_ID('dbo.Patient','U') IS NULL
                CREATE TABLE dbo.Patient (
                  Id INT IDENTITY(1,1) PRIMARY KEY,
                  FullName NVARCHAR(200) NOT NULL,
                  Phone NVARCHAR(40) NOT NULL,
                  Age INT NOT NULL,
                  Gender NVARCHAR(20) NOT NULL,
                  Cnic NVARCHAR(30) NULL,
                  PassportNo NVARCHAR(40) NULL,
                  PhotoPath NVARCHAR(500) NULL,
                  IsPanel BIT NOT NULL DEFAULT 0,
                  PanelName NVARCHAR(200) NULL,
                  Status BIT NOT NULL DEFAULT 1,
                  BranchId INT NULL,
                  CompanyId INT NULL,
                  CreatedOn DATETIME2 NULL,
                  CreatedBy INT NULL
                );

                IF COL_LENGTH('dbo.Patient', 'Cnic') IS NULL
                  ALTER TABLE dbo.Patient ADD Cnic NVARCHAR(30) NULL;
                IF COL_LENGTH('dbo.Patient', 'PassportNo') IS NULL
                  ALTER TABLE dbo.Patient ADD PassportNo NVARCHAR(40) NULL;
                IF COL_LENGTH('dbo.Patient', 'PhotoPath') IS NULL
                  ALTER TABLE dbo.Patient ADD PhotoPath NVARCHAR(500) NULL;

                IF OBJECT_ID('dbo.Visit','U') IS NULL
                CREATE TABLE dbo.Visit (
                  Id INT IDENTITY(1,1) PRIMARY KEY,
                  VisitNumber NVARCHAR(40) NOT NULL,
                  PatientId INT NOT NULL,
                  PercentOff DECIMAL(18,2) NOT NULL DEFAULT 0,
                  VisitOff DECIMAL(18,2) NOT NULL DEFAULT 0,
                  Fees DECIMAL(18,2) NOT NULL DEFAULT 0,
                  Discounts DECIMAL(18,2) NOT NULL DEFAULT 0,
                  Net DECIMAL(18,2) NOT NULL DEFAULT 0,
                  AmountReceived DECIMAL(18,2) NOT NULL DEFAULT 0,
                  PaymentReceived BIT NOT NULL DEFAULT 0,
                  IsReferred BIT NOT NULL DEFAULT 0,
                  ReferredBy NVARCHAR(200) NULL,
                  SlipNo NVARCHAR(100) NULL,
                  Notes NVARCHAR(500) NULL,
                  BranchId INT NULL,
                  CompanyId INT NULL,
                  CreatedBy INT NULL,
                  CreatedAt DATETIME2 NOT NULL
                );

                IF OBJECT_ID('dbo.VisitService','U') IS NULL
                CREATE TABLE dbo.VisitService (
                  Id INT IDENTITY(1,1) PRIMARY KEY,
                  VisitId INT NOT NULL,
                  ServiceId INT NOT NULL,
                  Fee DECIMAL(18,2) NOT NULL,
                  Discount DECIMAL(18,2) NOT NULL DEFAULT 0,
                  Urgent BIT NOT NULL DEFAULT 0,
                  StaffId INT NULL,
                  StaffName NVARCHAR(200) NULL,
                  ReadyBy NVARCHAR(10) NULL
                );
                """);

            if (!await db.Service.AnyAsync())
            {
                var now = DateTime.UtcNow;
                db.Service.AddRange(
                    // Phase-1: consultation pathway has no station — keep catalog rows inactive.
                    new Service { Name = "General consultation", Category = "Consultation", Fee = 1500, NormalMinutes = 30, UrgentMinutes = 15, Status = false, CreatedOn = now },
                    new Service { Name = "Follow-up consultation", Category = "Consultation", Fee = 800, NormalMinutes = 20, UrgentMinutes = 10, Status = false, CreatedOn = now },
                    new Service { Name = "LFT", Category = "Laboratory", Fee = 2200, NormalMinutes = 240, UrgentMinutes = 120, Status = true, CreatedOn = now },
                    new Service { Name = "RFT", Category = "Laboratory", Fee = 1800, NormalMinutes = 240, UrgentMinutes = 120, Status = true, CreatedOn = now },
                    new Service { Name = "CBC", Category = "Laboratory", Fee = 800, NormalMinutes = 60, UrgentMinutes = 30, Status = true, CreatedOn = now },
                    new Service { Name = "Blood glucose", Category = "Laboratory", Fee = 400, NormalMinutes = 30, UrgentMinutes = 15, Status = true, CreatedOn = now },
                    new Service { Name = "Lipid profile", Category = "Laboratory", Fee = 1600, NormalMinutes = 180, UrgentMinutes = 90, Status = true, CreatedOn = now },
                    new Service { Name = "Urine RE", Category = "Laboratory", Fee = 500, NormalMinutes = 45, UrgentMinutes = 20, Status = true, CreatedOn = now },
                    new Service { Name = "ECG", Category = "Cardiology", Fee = 1000, NormalMinutes = 30, UrgentMinutes = 15, Status = true, CreatedOn = now },
                    new Service { Name = "X-ray chest", Category = "Radiology", Fee = 1800, NormalMinutes = 60, UrgentMinutes = 30, Status = true, CreatedOn = now },
                    new Service { Name = "Ultrasound abdomen", Category = "Radiology", Fee = 2500, NormalMinutes = 90, UrgentMinutes = 45, Status = true, CreatedOn = now },
                    new Service { Name = "Thyroid profile", Category = "Laboratory", Fee = 1900, NormalMinutes = 240, UrgentMinutes = 120, Status = true, CreatedOn = now },
                    new Service { Name = "CT scan head", Category = "Radiology", Fee = 6500, NormalMinutes = 180, UrgentMinutes = 90, Status = true, CreatedOn = now }
                );
            }
            else if (!await db.Service.AnyAsync(s => s.Name == "RFT"))
            {
                db.Service.Add(new Service
                {
                    Name = "RFT",
                    Category = "Laboratory",
                    Fee = 1800,
                    NormalMinutes = 240,
                    UrgentMinutes = 120,
                    Status = true,
                    CreatedOn = DateTime.UtcNow
                });
            }

            await db.SaveChangesAsync();

            await db.Database.ExecuteSqlRawAsync("""
                UPDATE dbo.Service SET Status = 0 WHERE Category = N'Consultation' AND Status = 1;
                UPDATE dbo.Service SET NormalMinutes = 30, UrgentMinutes = 15 WHERE Name = N'General consultation' AND NormalMinutes = 60 AND UrgentMinutes = 30;
                UPDATE dbo.Service SET NormalMinutes = 20, UrgentMinutes = 10 WHERE Name = N'Follow-up consultation' AND NormalMinutes = 60 AND UrgentMinutes = 30;
                UPDATE dbo.Service SET NormalMinutes = 240, UrgentMinutes = 120 WHERE Name IN (N'LFT', N'RFT', N'Thyroid profile') AND NormalMinutes = 60 AND UrgentMinutes = 30;
                UPDATE dbo.Service SET NormalMinutes = 60, UrgentMinutes = 30 WHERE Name = N'CBC' AND NormalMinutes = 60 AND UrgentMinutes = 30 AND Fee = 800;
                UPDATE dbo.Service SET NormalMinutes = 30, UrgentMinutes = 15 WHERE Name = N'Blood glucose' AND NormalMinutes = 60 AND UrgentMinutes = 30;
                UPDATE dbo.Service SET NormalMinutes = 180, UrgentMinutes = 90 WHERE Name = N'Lipid profile' AND NormalMinutes = 60 AND UrgentMinutes = 30;
                UPDATE dbo.Service SET NormalMinutes = 45, UrgentMinutes = 20 WHERE Name = N'Urine RE' AND NormalMinutes = 60 AND UrgentMinutes = 30;
                UPDATE dbo.Service SET NormalMinutes = 30, UrgentMinutes = 15 WHERE Name = N'ECG' AND NormalMinutes = 60 AND UrgentMinutes = 30;
                UPDATE dbo.Service SET NormalMinutes = 60, UrgentMinutes = 30 WHERE Name = N'X-ray chest' AND NormalMinutes = 60 AND UrgentMinutes = 30 AND Fee = 1800;
                UPDATE dbo.Service SET NormalMinutes = 90, UrgentMinutes = 45 WHERE Name = N'Ultrasound abdomen' AND NormalMinutes = 60 AND UrgentMinutes = 30;
                UPDATE dbo.Service SET NormalMinutes = 180, UrgentMinutes = 90 WHERE Name = N'CT scan head' AND NormalMinutes = 60 AND UrgentMinutes = 30;
                """);

            if (!await db.Staff.AnyAsync())
            {
                var now = DateTime.UtcNow;
                db.Staff.AddRange(
                    new Staff { Name = "Ayesha Khan", Department = "Laboratory", Status = true, CreatedOn = now },
                    new Staff { Name = "Sana Ali", Department = "Laboratory", Status = true, CreatedOn = now },
                    new Staff { Name = "Dr. Imran Shah", Department = "Consultation", Status = true, CreatedOn = now },
                    new Staff { Name = "Bilal Hussain", Department = "Radiology", Status = true, CreatedOn = now }
                );
            }

            await db.SaveChangesAsync();
            await LabWorkflowSeeder.EnsureSeedAsync(db);
            await Phase1OpsSeeder.EnsureSeedAsync(db);
        }
    }
}
