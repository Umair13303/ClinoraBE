using ClinoraBE.Model.DAL;
using ClinoraBE.Model.ViewModel;
using Microsoft.EntityFrameworkCore;

namespace ClinoraBE.Data
{
    public static class LabWorkflowSeeder
    {
        public static async Task EnsureSeedAsync(ClinoraContext db)
        {
            await db.Database.ExecuteSqlRawAsync("""
                IF OBJECT_ID('dbo.BottleColor','U') IS NULL
                CREATE TABLE dbo.BottleColor (
                  Id INT IDENTITY(1,1) PRIMARY KEY,
                  Code NVARCHAR(40) NOT NULL,
                  Name NVARCHAR(100) NOT NULL,
                  Status BIT NOT NULL DEFAULT 1
                );

                IF OBJECT_ID('dbo.SampleType','U') IS NULL
                CREATE TABLE dbo.SampleType (
                  Id INT IDENTITY(1,1) PRIMARY KEY,
                  Code NVARCHAR(40) NOT NULL,
                  Name NVARCHAR(100) NOT NULL,
                  Status BIT NOT NULL DEFAULT 1
                );

                IF OBJECT_ID('dbo.SampleGroup','U') IS NULL
                CREATE TABLE dbo.SampleGroup (
                  Id INT IDENTITY(1,1) PRIMARY KEY,
                  Code NVARCHAR(40) NOT NULL,
                  Name NVARCHAR(100) NOT NULL,
                  Status BIT NOT NULL DEFAULT 1
                );

                IF OBJECT_ID('dbo.Container','U') IS NULL
                CREATE TABLE dbo.Container (
                  Id INT IDENTITY(1,1) PRIMARY KEY,
                  Code NVARCHAR(40) NOT NULL,
                  Name NVARCHAR(100) NOT NULL,
                  DefaultBottleColorId INT NULL,
                  Status BIT NOT NULL DEFAULT 1
                );

                IF OBJECT_ID('dbo.OutsideLab','U') IS NULL
                CREATE TABLE dbo.OutsideLab (
                  Id INT IDENTITY(1,1) PRIMARY KEY,
                  Name NVARCHAR(200) NOT NULL,
                  Phone NVARCHAR(40) NULL,
                  Address NVARCHAR(300) NULL,
                  Status BIT NOT NULL DEFAULT 1
                );

                IF COL_LENGTH('dbo.Service', 'Pathway') IS NULL
                  ALTER TABLE dbo.Service ADD Pathway NVARCHAR(40) NOT NULL CONSTRAINT DF_Service_Pathway DEFAULT N'Laboratory';
                IF COL_LENGTH('dbo.Service', 'ReportProfile') IS NULL
                  ALTER TABLE dbo.Service ADD ReportProfile NVARCHAR(40) NOT NULL CONSTRAINT DF_Service_ReportProfile DEFAULT N'Standard';
                IF COL_LENGTH('dbo.Service', 'SampleTypeId') IS NULL
                  ALTER TABLE dbo.Service ADD SampleTypeId INT NULL;
                IF COL_LENGTH('dbo.Service', 'ContainerId') IS NULL
                  ALTER TABLE dbo.Service ADD ContainerId INT NULL;
                IF COL_LENGTH('dbo.Service', 'BottleColorId') IS NULL
                  ALTER TABLE dbo.Service ADD BottleColorId INT NULL;
                IF COL_LENGTH('dbo.Service', 'SampleGroupId') IS NULL
                  ALTER TABLE dbo.Service ADD SampleGroupId INT NULL;
                IF COL_LENGTH('dbo.Service', 'CanSendOut') IS NULL
                  ALTER TABLE dbo.Service ADD CanSendOut BIT NOT NULL CONSTRAINT DF_Service_CanSendOut DEFAULT 0;
                IF COL_LENGTH('dbo.Service', 'DefaultOutsideLabId') IS NULL
                  ALTER TABLE dbo.Service ADD DefaultOutsideLabId INT NULL;

                IF COL_LENGTH('dbo.VisitService', 'Status') IS NULL
                  ALTER TABLE dbo.VisitService ADD Status NVARCHAR(40) NOT NULL CONSTRAINT DF_VisitService_Status DEFAULT N'AwaitingCollection';
                IF COL_LENGTH('dbo.VisitService', 'SampleId') IS NULL
                  ALTER TABLE dbo.VisitService ADD SampleId INT NULL;
                IF COL_LENGTH('dbo.VisitService', 'IsSendOut') IS NULL
                  ALTER TABLE dbo.VisitService ADD IsSendOut BIT NOT NULL CONSTRAINT DF_VisitService_IsSendOut DEFAULT 0;
                IF COL_LENGTH('dbo.VisitService', 'OutsideLabId') IS NULL
                  ALTER TABLE dbo.VisitService ADD OutsideLabId INT NULL;
                IF COL_LENGTH('dbo.VisitService', 'ResultEnteredAt') IS NULL
                  ALTER TABLE dbo.VisitService ADD ResultEnteredAt DATETIME2 NULL;
                IF COL_LENGTH('dbo.VisitService', 'FinalizedAt') IS NULL
                  ALTER TABLE dbo.VisitService ADD FinalizedAt DATETIME2 NULL;
                IF COL_LENGTH('dbo.VisitService', 'FinalizedByName') IS NULL
                  ALTER TABLE dbo.VisitService ADD FinalizedByName NVARCHAR(200) NULL;

                IF OBJECT_ID('dbo.LabSample','U') IS NULL
                CREATE TABLE dbo.LabSample (
                  Id INT IDENTITY(1,1) PRIMARY KEY,
                  VisitId INT NOT NULL,
                  SampleGroupId INT NULL,
                  SampleTypeId INT NOT NULL,
                  ContainerId INT NOT NULL,
                  BottleColorId INT NOT NULL,
                  Barcode NVARCHAR(60) NOT NULL,
                  PhlebotomistName NVARCHAR(200) NULL,
                  CollectedAt DATETIME2 NOT NULL,
                  IsSendOut BIT NOT NULL DEFAULT 0,
                  OutsideLabId INT NULL
                );

                IF OBJECT_ID('dbo.SampleTest','U') IS NULL
                CREATE TABLE dbo.SampleTest (
                  Id INT IDENTITY(1,1) PRIMARY KEY,
                  SampleId INT NOT NULL,
                  VisitServiceId INT NOT NULL
                );

                IF OBJECT_ID('dbo.OutsideLabShipment','U') IS NULL
                CREATE TABLE dbo.OutsideLabShipment (
                  Id INT IDENTITY(1,1) PRIMARY KEY,
                  OutsideLabId INT NOT NULL,
                  Status NVARCHAR(40) NOT NULL,
                  Awb NVARCHAR(100) NULL,
                  Notes NVARCHAR(500) NULL,
                  SentAt DATETIME2 NOT NULL,
                  ReceivedAt DATETIME2 NULL,
                  SentBy INT NULL
                );

                IF OBJECT_ID('dbo.OutsideLabShipmentItem','U') IS NULL
                CREATE TABLE dbo.OutsideLabShipmentItem (
                  Id INT IDENTITY(1,1) PRIMARY KEY,
                  ShipmentId INT NOT NULL,
                  SampleId INT NOT NULL,
                  VisitServiceId INT NOT NULL
                );

                IF OBJECT_ID('dbo.TestResult','U') IS NULL
                CREATE TABLE dbo.TestResult (
                  Id INT IDENTITY(1,1) PRIMARY KEY,
                  VisitServiceId INT NOT NULL,
                  Source NVARCHAR(40) NOT NULL,
                  ResultText NVARCHAR(MAX) NOT NULL,
                  Units NVARCHAR(40) NULL,
                  ReferenceRange NVARCHAR(200) NULL,
                  EnteredByName NVARCHAR(200) NULL,
                  EnteredAt DATETIME2 NOT NULL
                );

                -- Radiology HTML reports need MAX; older installs used NVARCHAR(2000).
                IF COL_LENGTH('dbo.TestResult', 'ResultText') IS NOT NULL
                   AND EXISTS (
                     SELECT 1 FROM sys.columns c
                     JOIN sys.types t ON c.user_type_id = t.user_type_id
                     WHERE c.object_id = OBJECT_ID('dbo.TestResult')
                       AND c.name = 'ResultText'
                       AND t.name = N'nvarchar'
                       AND c.max_length > 0
                   )
                BEGIN
                  ALTER TABLE dbo.TestResult ALTER COLUMN ResultText NVARCHAR(MAX) NOT NULL;
                END

                IF OBJECT_ID('dbo.Report','U') IS NULL
                CREATE TABLE dbo.Report (
                  Id INT IDENTITY(1,1) PRIMARY KEY,
                  VisitId INT NOT NULL,
                  Pathway NVARCHAR(40) NOT NULL,
                  FinalizedByName NVARCHAR(200) NULL,
                  FinalizedAt DATETIME2 NOT NULL,
                  PrintCount INT NOT NULL DEFAULT 0
                );
                """);

            if (!await db.BottleColor.AnyAsync())
            {
                db.BottleColor.AddRange(
                    new BottleColor { Code = "PURPLE", Name = "Purple" },
                    new BottleColor { Code = "GOLD", Name = "Gold" },
                    new BottleColor { Code = "RED", Name = "Red" },
                    new BottleColor { Code = "BLUE", Name = "Blue" },
                    new BottleColor { Code = "CLEAR", Name = "Clear" }
                );
                await db.SaveChangesAsync();
            }

            if (!await db.SampleType.AnyAsync())
            {
                db.SampleType.AddRange(
                    new SampleType { Code = "BLOOD", Name = "Blood" },
                    new SampleType { Code = "URINE", Name = "Urine" },
                    new SampleType { Code = "STOOL", Name = "Stool" },
                    new SampleType { Code = "SWAB", Name = "Swab" },
                    new SampleType { Code = "OTHER", Name = "Other" },
                    new SampleType { Code = "SEMEN", Name = "Semen" },
                    new SampleType { Code = "TISSUE", Name = "Tissue" }
                );
                await db.SaveChangesAsync();
            }
            else
            {
                await EnsureSampleTypeAsync(db, "SEMEN", "Semen");
                await EnsureSampleTypeAsync(db, "TISSUE", "Tissue");
            }

            if (!await db.SampleGroup.AnyAsync())
            {
                db.SampleGroup.AddRange(
                    new SampleGroup { Code = "EDTA", Name = "EDTA blood" },
                    new SampleGroup { Code = "SERUM", Name = "Serum chemistry" },
                    new SampleGroup { Code = "URINE", Name = "Urine sample" },
                    new SampleGroup { Code = "GLUCOSE", Name = "Glucose tube" },
                    new SampleGroup { Code = "SEMEN", Name = "Semen sample" },
                    new SampleGroup { Code = "TISSUE", Name = "Tissue / histopathology" }
                );
                await db.SaveChangesAsync();
            }
            else
            {
                await EnsureSampleGroupAsync(db, "SEMEN", "Semen sample");
                await EnsureSampleGroupAsync(db, "TISSUE", "Tissue / histopathology");
            }

            var colors = await db.BottleColor.AsNoTracking().ToDictionaryAsync(c => c.Code, c => c.Id);
            if (!await db.Container.AnyAsync())
            {
                db.Container.AddRange(
                    new Container { Code = "EDTA", Name = "EDTA tube", DefaultBottleColorId = colors.GetValueOrDefault("PURPLE") },
                    new Container { Code = "SST", Name = "SST / gel tube", DefaultBottleColorId = colors.GetValueOrDefault("GOLD") },
                    new Container { Code = "PLAIN", Name = "Plain tube", DefaultBottleColorId = colors.GetValueOrDefault("RED") },
                    new Container { Code = "FLUORIDE", Name = "Fluoride tube", DefaultBottleColorId = colors.GetValueOrDefault("BLUE") },
                    new Container { Code = "URINEPOT", Name = "Urine pot", DefaultBottleColorId = colors.GetValueOrDefault("CLEAR") },
                    new Container { Code = "STERILEPOT", Name = "Sterile pot", DefaultBottleColorId = colors.GetValueOrDefault("CLEAR") },
                    new Container { Code = "FORMALIN", Name = "Formalin pot", DefaultBottleColorId = colors.GetValueOrDefault("CLEAR") }
                );
                await db.SaveChangesAsync();
            }
            else
            {
                await EnsureContainerAsync(db, "STERILEPOT", "Sterile pot", colors.GetValueOrDefault("CLEAR"));
                await EnsureContainerAsync(db, "FORMALIN", "Formalin pot", colors.GetValueOrDefault("CLEAR"));
            }

            if (!await db.OutsideLab.AnyAsync())
            {
                db.OutsideLab.AddRange(
                    new OutsideLab { Name = "City Reference Lab", Phone = "021-111000111", Address = "Karachi" },
                    new OutsideLab { Name = "Metro Diagnostics", Phone = "042-111222333", Address = "Lahore" }
                );
                await db.SaveChangesAsync();
            }

            var types = await db.SampleType.AsNoTracking().ToDictionaryAsync(t => t.Code, t => t.Id);
            var groups = await db.SampleGroup.AsNoTracking().ToDictionaryAsync(g => g.Code, g => g.Id);
            var containers = await db.Container.AsNoTracking().ToDictionaryAsync(c => c.Code, c => c.Id);
            var outside = await db.OutsideLab.AsNoTracking().OrderBy(o => o.Id).FirstOrDefaultAsync();

            foreach (var service in await db.Service.ToListAsync())
            {
                var pathway = service.Category switch
                {
                    "Consultation" => ServicePathways.Consultation,
                    "Radiology" => ServicePathways.Radiology,
                    "Cardiology" when service.Name.Contains("ECG", StringComparison.OrdinalIgnoreCase)
                        => ServicePathways.Radiology,
                    _ => ServicePathways.Laboratory
                };
                service.Pathway = pathway;
                if (pathway == ServicePathways.Radiology)
                {
                    service.ReportProfile = ReportProfiles.Radiology;
                }

                if (pathway != ServicePathways.Laboratory)
                {
                    service.SampleTypeId = null;
                    service.ContainerId = null;
                    service.BottleColorId = null;
                    service.SampleGroupId = null;
                    service.CanSendOut = false;
                    continue;
                }

                service.SampleTypeId = types.GetValueOrDefault("BLOOD");
                service.CanSendOut = service.Name is "LFT" or "RFT" or "Thyroid profile" or "Lipid profile";
                service.DefaultOutsideLabId = service.CanSendOut ? outside?.Id : null;

                switch (service.Name)
                {
                    case "CBC":
                        service.ContainerId = containers.GetValueOrDefault("EDTA");
                        service.BottleColorId = colors.GetValueOrDefault("PURPLE");
                        service.SampleGroupId = groups.GetValueOrDefault("EDTA");
                        break;
                    case "Blood glucose":
                        service.ContainerId = containers.GetValueOrDefault("FLUORIDE");
                        service.BottleColorId = colors.GetValueOrDefault("BLUE");
                        service.SampleGroupId = groups.GetValueOrDefault("GLUCOSE");
                        break;
                    case "Urine RE":
                        service.SampleTypeId = types.GetValueOrDefault("URINE");
                        service.ContainerId = containers.GetValueOrDefault("URINEPOT");
                        service.BottleColorId = colors.GetValueOrDefault("CLEAR");
                        service.SampleGroupId = groups.GetValueOrDefault("URINE");
                        service.CanSendOut = false;
                        service.DefaultOutsideLabId = null;
                        break;
                    default:
                        service.ContainerId = containers.GetValueOrDefault("SST");
                        service.BottleColorId = colors.GetValueOrDefault("GOLD");
                        service.SampleGroupId = groups.GetValueOrDefault("SERUM");
                        break;
                }
            }

            await db.SaveChangesAsync();
        }

        private static async Task EnsureSampleTypeAsync(ClinoraContext db, string code, string name)
        {
            if (await db.SampleType.AnyAsync(t => t.Code == code))
            {
                return;
            }

            db.SampleType.Add(new SampleType { Code = code, Name = name });
            await db.SaveChangesAsync();
        }

        private static async Task EnsureSampleGroupAsync(ClinoraContext db, string code, string name)
        {
            if (await db.SampleGroup.AnyAsync(g => g.Code == code))
            {
                return;
            }

            db.SampleGroup.Add(new SampleGroup { Code = code, Name = name });
            await db.SaveChangesAsync();
        }

        private static async Task EnsureContainerAsync(ClinoraContext db, string code, string name, int? colorId)
        {
            if (await db.Container.AnyAsync(c => c.Code == code))
            {
                return;
            }

            db.Container.Add(new Container { Code = code, Name = name, DefaultBottleColorId = colorId });
            await db.SaveChangesAsync();
        }
    }
}
