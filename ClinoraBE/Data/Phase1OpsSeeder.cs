using ClinoraBE.Model.DAL;
using ClinoraBE.Model.ViewModel;
using Microsoft.EntityFrameworkCore;

namespace ClinoraBE.Data
{
    public static class Phase1OpsSeeder
    {
        public static async Task EnsureSeedAsync(ClinoraContext db)
        {
            await db.Database.ExecuteSqlRawAsync("""
                IF COL_LENGTH('dbo.Visit', 'TokenNo') IS NULL
                  ALTER TABLE dbo.Visit ADD TokenNo INT NULL;
                IF COL_LENGTH('dbo.Visit', 'VisitStatus') IS NULL
                  ALTER TABLE dbo.Visit ADD VisitStatus NVARCHAR(40) NOT NULL CONSTRAINT DF_Visit_VisitStatus DEFAULT N'Active';

                IF COL_LENGTH('dbo.VisitService', 'RejectReason') IS NULL
                  ALTER TABLE dbo.VisitService ADD RejectReason NVARCHAR(500) NULL;
                IF COL_LENGTH('dbo.VisitService', 'RejectedAt') IS NULL
                  ALTER TABLE dbo.VisitService ADD RejectedAt DATETIME2 NULL;
                IF COL_LENGTH('dbo.VisitService', 'RejectedByName') IS NULL
                  ALTER TABLE dbo.VisitService ADD RejectedByName NVARCHAR(200) NULL;
                IF COL_LENGTH('dbo.VisitService', 'IsAbnormal') IS NULL
                  ALTER TABLE dbo.VisitService ADD IsAbnormal BIT NOT NULL CONSTRAINT DF_VisitService_IsAbnormal DEFAULT 0;
                IF COL_LENGTH('dbo.VisitService', 'IsCritical') IS NULL
                  ALTER TABLE dbo.VisitService ADD IsCritical BIT NOT NULL CONSTRAINT DF_VisitService_IsCritical DEFAULT 0;

                IF COL_LENGTH('dbo.LabSample', 'SampleStatus') IS NULL
                  ALTER TABLE dbo.LabSample ADD SampleStatus NVARCHAR(40) NOT NULL CONSTRAINT DF_LabSample_SampleStatus DEFAULT N'Collected';
                IF COL_LENGTH('dbo.LabSample', 'RejectReason') IS NULL
                  ALTER TABLE dbo.LabSample ADD RejectReason NVARCHAR(500) NULL;
                IF COL_LENGTH('dbo.LabSample', 'RejectedAt') IS NULL
                  ALTER TABLE dbo.LabSample ADD RejectedAt DATETIME2 NULL;
                IF COL_LENGTH('dbo.LabSample', 'RejectedByName') IS NULL
                  ALTER TABLE dbo.LabSample ADD RejectedByName NVARCHAR(200) NULL;
                IF COL_LENGTH('dbo.LabSample', 'ReceivedAt') IS NULL
                  ALTER TABLE dbo.LabSample ADD ReceivedAt DATETIME2 NULL;
                IF COL_LENGTH('dbo.LabSample', 'ReceivedBy') IS NULL
                  ALTER TABLE dbo.LabSample ADD ReceivedBy NVARCHAR(200) NULL;

                IF COL_LENGTH('dbo.OutsideLabShipment', 'ExpectedAt') IS NULL
                  ALTER TABLE dbo.OutsideLabShipment ADD ExpectedAt DATETIME2 NULL;

                IF COL_LENGTH('dbo.TestResult', 'Version') IS NULL
                  ALTER TABLE dbo.TestResult ADD Version INT NOT NULL CONSTRAINT DF_TestResult_Version DEFAULT 1;
                IF COL_LENGTH('dbo.TestResult', 'IsCurrent') IS NULL
                  ALTER TABLE dbo.TestResult ADD IsCurrent BIT NOT NULL CONSTRAINT DF_TestResult_IsCurrent DEFAULT 1;
                IF COL_LENGTH('dbo.TestResult', 'IsAbnormal') IS NULL
                  ALTER TABLE dbo.TestResult ADD IsAbnormal BIT NOT NULL CONSTRAINT DF_TestResult_IsAbnormal DEFAULT 0;
                IF COL_LENGTH('dbo.TestResult', 'IsCritical') IS NULL
                  ALTER TABLE dbo.TestResult ADD IsCritical BIT NOT NULL CONSTRAINT DF_TestResult_IsCritical DEFAULT 0;
                IF COL_LENGTH('dbo.TestResult', 'ParameterName') IS NULL
                  ALTER TABLE dbo.TestResult ADD ParameterName NVARCHAR(100) NULL;

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

                UPDATE dbo.Service
                SET ReportProfile = N'Radiology'
                WHERE Pathway = N'Radiology'
                  AND (ReportProfile IS NULL OR ReportProfile = N'' OR ReportProfile = N'Standard');

                IF OBJECT_ID('dbo.ServiceParameter','U') IS NULL
                CREATE TABLE dbo.ServiceParameter (
                  Id INT IDENTITY(1,1) PRIMARY KEY,
                  ServiceId INT NOT NULL,
                  Name NVARCHAR(100) NOT NULL,
                  Unit NVARCHAR(40) NULL,
                  RefLow DECIMAL(18,4) NULL,
                  RefHigh DECIMAL(18,4) NULL,
                  SortOrder INT NOT NULL DEFAULT 0,
                  Status BIT NOT NULL DEFAULT 1
                );

                IF COL_LENGTH('dbo.Branch', 'Code') IS NULL
                  ALTER TABLE dbo.Branch ADD Code NVARCHAR(40) NULL;

                IF COL_LENGTH('dbo.Service', 'ReportProfile') IS NULL
                  ALTER TABLE dbo.Service ADD ReportProfile NVARCHAR(40) NOT NULL CONSTRAINT DF_Service_ReportProfile DEFAULT N'Standard';

                IF COL_LENGTH('dbo.RadiologyTemplate', 'Profile') IS NULL
                  ALTER TABLE dbo.RadiologyTemplate ADD Profile NVARCHAR(40) NOT NULL CONSTRAINT DF_RadiologyTemplate_Profile DEFAULT N'Radiology';

                IF COL_LENGTH('dbo.ReportLine', 'ReportProfile') IS NULL
                  ALTER TABLE dbo.ReportLine ADD ReportProfile NVARCHAR(40) NOT NULL CONSTRAINT DF_ReportLine_ReportProfile DEFAULT N'Standard';
                IF COL_LENGTH('dbo.ReportLine', 'ParameterName') IS NULL
                  ALTER TABLE dbo.ReportLine ADD ParameterName NVARCHAR(100) NULL;

                IF OBJECT_ID('dbo.ReportLine','U') IS NULL
                CREATE TABLE dbo.ReportLine (
                  Id INT IDENTITY(1,1) PRIMARY KEY,
                  ReportId INT NOT NULL,
                  VisitServiceId INT NOT NULL,
                  ServiceName NVARCHAR(200) NOT NULL,
                  ParameterName NVARCHAR(100) NULL,
                  ResultText NVARCHAR(MAX) NULL,
                  Units NVARCHAR(40) NULL,
                  ReferenceRange NVARCHAR(100) NULL,
                  Urgent BIT NOT NULL DEFAULT 0,
                  IsAbnormal BIT NOT NULL DEFAULT 0,
                  IsCritical BIT NOT NULL DEFAULT 0
                );

                IF OBJECT_ID('dbo.RadiologyTemplate','U') IS NULL
                CREATE TABLE dbo.RadiologyTemplate (
                  Id INT IDENTITY(1,1) PRIMARY KEY,
                  Name NVARCHAR(200) NOT NULL,
                  Modality NVARCHAR(40) NOT NULL DEFAULT N'XRay',
                  ServiceId INT NULL,
                  Body NVARCHAR(MAX) NOT NULL,
                  Status BIT NOT NULL DEFAULT 1,
                  CreatedOn DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
                );

                IF OBJECT_ID('dbo.RadiologyTemplateService','U') IS NULL
                CREATE TABLE dbo.RadiologyTemplateService (
                  Id INT IDENTITY(1,1) PRIMARY KEY,
                  TemplateId INT NOT NULL,
                  ServiceId INT NOT NULL,
                  CONSTRAINT UQ_RadiologyTemplateService UNIQUE (TemplateId, ServiceId)
                );

                -- Migrate legacy single ServiceId into join rows once.
                INSERT INTO dbo.RadiologyTemplateService (TemplateId, ServiceId)
                SELECT t.Id, t.ServiceId
                FROM dbo.RadiologyTemplate t
                WHERE t.ServiceId IS NOT NULL
                  AND NOT EXISTS (
                    SELECT 1 FROM dbo.RadiologyTemplateService x
                    WHERE x.TemplateId = t.Id AND x.ServiceId = t.ServiceId
                  );
                """);

            if (!await db.Branch.AnyAsync())
            {
                db.Branch.AddRange(
                    new Branch
                    {
                        Description = "Main branch",
                        Code = "MAIN",
                        Status = true,
                        Contact = "021-0000000",
                        Address = "Karachi",
                        CreatedOn = DateTime.UtcNow
                    },
                    new Branch
                    {
                        Description = "City branch",
                        Code = "CITY",
                        Status = true,
                        Contact = "021-1111111",
                        Address = "Karachi",
                        CreatedOn = DateTime.UtcNow
                    }
                );
                await db.SaveChangesAsync();
            }
            else
            {
                await db.Database.ExecuteSqlRawAsync("""
                    UPDATE dbo.Branch SET Code = N'MAIN' WHERE Code IS NULL AND Id = (SELECT MIN(Id) FROM dbo.Branch);
                    UPDATE dbo.Branch SET Code = CONCAT(N'BR', Id) WHERE Code IS NULL;
                    """);
            }

            if (!await db.ServiceParameter.AnyAsync())
            {
                var lft = await db.Service.FirstOrDefaultAsync(s => s.Name == "LFT");
                var cbc = await db.Service.FirstOrDefaultAsync(s => s.Name == "CBC");
                var rft = await db.Service.FirstOrDefaultAsync(s => s.Name == "RFT");
                if (lft != null)
                {
                    db.ServiceParameter.AddRange(
                        new ServiceParameter { ServiceId = lft.Id, Name = "ALT", Unit = "U/L", RefLow = 7, RefHigh = 56, SortOrder = 1 },
                        new ServiceParameter { ServiceId = lft.Id, Name = "AST", Unit = "U/L", RefLow = 10, RefHigh = 40, SortOrder = 2 },
                        new ServiceParameter { ServiceId = lft.Id, Name = "Bilirubin", Unit = "mg/dL", RefLow = 0.1m, RefHigh = 1.2m, SortOrder = 3 }
                    );
                }
                if (rft != null)
                {
                    db.ServiceParameter.AddRange(
                        new ServiceParameter { ServiceId = rft.Id, Name = "Urea", Unit = "mg/dL", RefLow = 15, RefHigh = 40, SortOrder = 1 },
                        new ServiceParameter { ServiceId = rft.Id, Name = "Creatinine", Unit = "mg/dL", RefLow = 0.6m, RefHigh = 1.3m, SortOrder = 2 }
                    );
                }
                if (cbc != null)
                {
                    db.ServiceParameter.AddRange(
                        new ServiceParameter { ServiceId = cbc.Id, Name = "Hemoglobin", Unit = "g/dL", RefLow = 12, RefHigh = 16, SortOrder = 1 },
                        new ServiceParameter { ServiceId = cbc.Id, Name = "WBC", Unit = "10^3/uL", RefLow = 4, RefHigh = 11, SortOrder = 2 },
                        new ServiceParameter { ServiceId = cbc.Id, Name = "Platelets", Unit = "10^3/uL", RefLow = 150, RefHigh = 450, SortOrder = 3 }
                    );
                }
                await db.SaveChangesAsync();
            }

            if (!await db.RadiologyTemplate.AnyAsync())
            {
                var xray = await db.Service.FirstOrDefaultAsync(s => s.Name.Contains("X-ray") || s.Name.Contains("Xray"));
                var us = await db.Service.FirstOrDefaultAsync(s => s.Name.Contains("Ultrasound"));
                var ct = await db.Service.FirstOrDefaultAsync(s => s.Name.Contains("CT"));

                var chest = new RadiologyTemplate
                {
                    Name = "Chest X-ray — PA",
                    Modality = "XRay",
                    Profile = ReportProfiles.Radiology,
                    ServiceId = xray?.Id,
                    Body = """
                        <p><strong>CLINICAL HISTORY:</strong></p><p>—</p>
                        <p><strong>TECHNIQUE:</strong></p><p>Frontal chest radiograph (PA).</p>
                        <p><strong>FINDINGS:</strong></p><p>The lungs are clear. No focal consolidation, pleural effusion, or pneumothorax. The cardiomediastinal silhouette is within normal limits. The visualized bony structures are intact.</p>
                        <p><strong>IMPRESSION:</strong></p><p>No acute cardiopulmonary process.</p>
                        """,
                    Status = true,
                    CreatedOn = DateTime.UtcNow
                };
                var abdomenUs = new RadiologyTemplate
                {
                    Name = "Ultrasound abdomen — routine",
                    Modality = "Ultrasound",
                    Profile = ReportProfiles.Radiology,
                    ServiceId = us?.Id,
                    Body = """
                        <p><strong>CLINICAL HISTORY:</strong></p><p>—</p>
                        <p><strong>TECHNIQUE:</strong></p><p>Transabdominal ultrasound of the abdomen.</p>
                        <p><strong>FINDINGS:</strong></p><p>Liver: Normal size and echotexture. No focal lesion.<br/>Gallbladder: No stones. Wall not thickened.<br/>CBD: Not dilated.<br/>Pancreas: Visualized portions unremarkable.<br/>Spleen: Normal.<br/>Kidneys: Normal size and echogenicity bilaterally. No hydronephrosis.<br/>Ascites: None.</p>
                        <p><strong>IMPRESSION:</strong></p><p>Normal abdominal ultrasound.</p>
                        """,
                    Status = true,
                    CreatedOn = DateTime.UtcNow
                };
                var ctHead = new RadiologyTemplate
                {
                    Name = "CT head — non-contrast",
                    Modality = "CT",
                    Profile = ReportProfiles.Radiology,
                    ServiceId = ct?.Id,
                    Body = """
                        <p><strong>CLINICAL HISTORY:</strong></p><p>—</p>
                        <p><strong>TECHNIQUE:</strong></p><p>Non-contrast CT of the head.</p>
                        <p><strong>FINDINGS:</strong></p><p>No acute intracranial hemorrhage. No large territorial infarct. Ventricles and basal cisterns are patent. No significant mass effect or midline shift. Calvarium intact.</p>
                        <p><strong>IMPRESSION:</strong></p><p>No acute intracranial abnormality.</p>
                        """,
                    Status = true,
                    CreatedOn = DateTime.UtcNow
                };

                db.RadiologyTemplate.AddRange(chest, abdomenUs, ctHead);
                await db.SaveChangesAsync();

                if (xray != null)
                {
                    db.RadiologyTemplateService.Add(new RadiologyTemplateService { TemplateId = chest.Id, ServiceId = xray.Id });
                }
                if (us != null)
                {
                    db.RadiologyTemplateService.Add(new RadiologyTemplateService { TemplateId = abdomenUs.Id, ServiceId = us.Id });
                }
                if (ct != null)
                {
                    db.RadiologyTemplateService.Add(new RadiologyTemplateService { TemplateId = ctHead.Id, ServiceId = ct.Id });
                }
                await db.SaveChangesAsync();
            }

            // Specialty lab services + narrative histo template.
            var semenType = await db.SampleType.AsNoTracking().FirstOrDefaultAsync(t => t.Code == "SEMEN");
            var tissueType = await db.SampleType.AsNoTracking().FirstOrDefaultAsync(t => t.Code == "TISSUE");
            var sterilePot = await db.Container.AsNoTracking().FirstOrDefaultAsync(c => c.Code == "STERILEPOT" || c.Code == "URINEPOT");
            var formalinPot = await db.Container.AsNoTracking().FirstOrDefaultAsync(c => c.Code == "FORMALIN" || c.Code == "URINEPOT");
            var clearColor = await db.BottleColor.AsNoTracking().FirstOrDefaultAsync(c => c.Code == "CLEAR");
            var semenGroup = await db.SampleGroup.AsNoTracking().FirstOrDefaultAsync(g => g.Code == "SEMEN");
            var tissueGroup = await db.SampleGroup.AsNoTracking().FirstOrDefaultAsync(g => g.Code == "TISSUE");

            if (!await db.Service.AnyAsync(s => s.Name == "Semen Analysis"))
            {
                db.Service.Add(new Service
                {
                    Name = "Semen Analysis",
                    Category = "Laboratory",
                    Fee = 2500,
                    NormalMinutes = 120,
                    UrgentMinutes = 60,
                    Pathway = ServicePathways.Laboratory,
                    ReportProfile = ReportProfiles.Semen,
                    SampleTypeId = semenType?.Id,
                    ContainerId = sterilePot?.Id,
                    BottleColorId = clearColor?.Id,
                    SampleGroupId = semenGroup?.Id,
                    CanSendOut = false,
                    Status = true,
                    CreatedOn = DateTime.UtcNow
                });
                await db.SaveChangesAsync();
            }
            else
            {
                var semen = await db.Service.FirstAsync(s => s.Name == "Semen Analysis");
                var changed = false;
                if (string.IsNullOrWhiteSpace(semen.ReportProfile) || semen.ReportProfile == ReportProfiles.Standard)
                {
                    semen.ReportProfile = ReportProfiles.Semen;
                    changed = true;
                }
                if (semenType != null && semen.SampleTypeId != semenType.Id)
                {
                    semen.SampleTypeId = semenType.Id;
                    changed = true;
                }
                if (sterilePot != null && (semen.ContainerId == null || semen.ContainerId != sterilePot.Id))
                {
                    semen.ContainerId = sterilePot.Id;
                    changed = true;
                }
                if (clearColor != null && semen.BottleColorId == null)
                {
                    semen.BottleColorId = clearColor.Id;
                    changed = true;
                }
                if (semenGroup != null && semen.SampleGroupId != semenGroup.Id)
                {
                    semen.SampleGroupId = semenGroup.Id;
                    changed = true;
                }
                if (changed)
                {
                    await db.SaveChangesAsync();
                }
            }

            if (!await db.Service.AnyAsync(s => s.Name == "Histopathology"))
            {
                db.Service.Add(new Service
                {
                    Name = "Histopathology",
                    Category = "Laboratory",
                    Fee = 4500,
                    NormalMinutes = 2880,
                    UrgentMinutes = 1440,
                    Pathway = ServicePathways.Laboratory,
                    ReportProfile = ReportProfiles.Narrative,
                    SampleTypeId = tissueType?.Id,
                    ContainerId = formalinPot?.Id,
                    BottleColorId = clearColor?.Id,
                    SampleGroupId = tissueGroup?.Id,
                    CanSendOut = false,
                    Status = true,
                    CreatedOn = DateTime.UtcNow
                });
                await db.SaveChangesAsync();
            }
            else
            {
                var histo = await db.Service.FirstAsync(s => s.Name == "Histopathology");
                var changed = false;
                if (string.IsNullOrWhiteSpace(histo.ReportProfile) || histo.ReportProfile == ReportProfiles.Standard)
                {
                    histo.ReportProfile = ReportProfiles.Narrative;
                    changed = true;
                }
                if (tissueType != null && histo.SampleTypeId != tissueType.Id)
                {
                    histo.SampleTypeId = tissueType.Id;
                    changed = true;
                }
                if (formalinPot != null && (histo.ContainerId == null || histo.ContainerId != formalinPot.Id))
                {
                    histo.ContainerId = formalinPot.Id;
                    changed = true;
                }
                if (clearColor != null && histo.BottleColorId == null)
                {
                    histo.BottleColorId = clearColor.Id;
                    changed = true;
                }
                if (tissueGroup != null && histo.SampleGroupId != tissueGroup.Id)
                {
                    histo.SampleGroupId = tissueGroup.Id;
                    changed = true;
                }
                if (changed)
                {
                    await db.SaveChangesAsync();
                }
            }

            if (!await db.RadiologyTemplate.AnyAsync(t => t.Profile == ReportProfiles.Narrative && t.Name == "Histopathology — routine"))
            {
                var histo = await db.Service.AsNoTracking().FirstOrDefaultAsync(s => s.Name == "Histopathology");
                var tmpl = new RadiologyTemplate
                {
                    Name = "Histopathology — routine",
                    Modality = "Histo",
                    Profile = ReportProfiles.Narrative,
                    ServiceId = histo?.Id,
                    Body = """
                        <p><strong>CLINICAL HISTORY:</strong></p><p>—</p>
                        <p><strong>SPECIMEN:</strong></p><p>—</p>
                        <p><strong>GROSS:</strong></p><p>—</p>
                        <p><strong>MICROSCOPIC:</strong></p><p>—</p>
                        <p><strong>DIAGNOSIS:</strong></p><p>—</p>
                        """,
                    Status = true,
                    CreatedOn = DateTime.UtcNow
                };
                db.RadiologyTemplate.Add(tmpl);
                await db.SaveChangesAsync();
                if (histo != null)
                {
                    db.RadiologyTemplateService.Add(new RadiologyTemplateService { TemplateId = tmpl.Id, ServiceId = histo.Id });
                    await db.SaveChangesAsync();
                }
            }

            await db.Database.ExecuteSqlRawAsync("""
                IF OBJECT_ID('dbo.Antibody','U') IS NULL
                CREATE TABLE dbo.Antibody (
                  Id INT IDENTITY(1,1) PRIMARY KEY,
                  Code NVARCHAR(40) NOT NULL,
                  Name NVARCHAR(200) NOT NULL,
                  Status BIT NOT NULL DEFAULT 1
                );
                """);

            if (!await db.Antibody.AnyAsync())
            {
                db.Antibody.AddRange(
                    new Antibody { Code = "ER", Name = "Estrogen receptor" },
                    new Antibody { Code = "PR", Name = "Progesterone receptor" },
                    new Antibody { Code = "HER2", Name = "HER2 / neu" },
                    new Antibody { Code = "KI67", Name = "Ki-67" },
                    new Antibody { Code = "CK7", Name = "Cytokeratin 7" },
                    new Antibody { Code = "CK20", Name = "Cytokeratin 20" },
                    new Antibody { Code = "TTF1", Name = "TTF-1" },
                    new Antibody { Code = "CD20", Name = "CD20" },
                    new Antibody { Code = "CD3", Name = "CD3" },
                    new Antibody { Code = "P53", Name = "p53" }
                );
                await db.SaveChangesAsync();
            }

            if (!await db.Service.AnyAsync(s => s.Name == "IHC Breast Panel"))
            {
                db.Service.Add(new Service
                {
                    Name = "IHC Breast Panel",
                    Category = "Laboratory",
                    Fee = 8500,
                    NormalMinutes = 2880,
                    UrgentMinutes = 1440,
                    Pathway = ServicePathways.Laboratory,
                    ReportProfile = ReportProfiles.Ihc,
                    SampleTypeId = tissueType?.Id,
                    ContainerId = formalinPot?.Id,
                    BottleColorId = clearColor?.Id,
                    SampleGroupId = tissueGroup?.Id,
                    CanSendOut = false,
                    Status = true,
                    CreatedOn = DateTime.UtcNow
                });
                await db.SaveChangesAsync();
            }
            else
            {
                var ihc = await db.Service.FirstAsync(s => s.Name == "IHC Breast Panel");
                var changed = false;
                if (string.IsNullOrWhiteSpace(ihc.ReportProfile) || ihc.ReportProfile == ReportProfiles.Standard)
                {
                    ihc.ReportProfile = ReportProfiles.Ihc;
                    changed = true;
                }
                if (tissueType != null && ihc.SampleTypeId != tissueType.Id)
                {
                    ihc.SampleTypeId = tissueType.Id;
                    changed = true;
                }
                if (formalinPot != null && (ihc.ContainerId == null || ihc.ContainerId != formalinPot.Id))
                {
                    ihc.ContainerId = formalinPot.Id;
                    changed = true;
                }
                if (clearColor != null && ihc.BottleColorId == null)
                {
                    ihc.BottleColorId = clearColor.Id;
                    changed = true;
                }
                if (tissueGroup != null && ihc.SampleGroupId != tissueGroup.Id)
                {
                    ihc.SampleGroupId = tissueGroup.Id;
                    changed = true;
                }
                if (changed)
                {
                    await db.SaveChangesAsync();
                }
            }

            await EnsureVaccinationServicesAsync(db);
            await EnsureMedicalCertificateServicesAsync(db);
            await SeedTodayDemoReportsAsync(db);
            await SeedVaccinationDemoAsync(db);
            await SeedMedicalCertificateDemoAsync(db);
        }

        private static async Task EnsureMedicalCertificateServicesAsync(ClinoraContext db)
        {
            await db.Database.ExecuteSqlRawAsync("""
                IF COL_LENGTH('dbo.Visit', 'Purpose') IS NULL
                  ALTER TABLE dbo.Visit ADD Purpose NVARCHAR(40) NOT NULL CONSTRAINT DF_Visit_Purpose DEFAULT N'Standard';

                IF OBJECT_ID('dbo.MedicalCertificate','U') IS NULL
                CREATE TABLE dbo.MedicalCertificate (
                  Id INT IDENTITY(1,1) PRIMARY KEY,
                  VisitId INT NOT NULL,
                  Fitness NVARCHAR(40) NOT NULL DEFAULT N'Pending',
                  DestinationCountry NVARCHAR(100) NULL,
                  Remarks NVARCHAR(1000) NULL,
                  IssuedByName NVARCHAR(200) NULL,
                  IssuedAt DATETIME2 NULL
                );
                """);

            async Task EnsureServiceAsync(string name, string category, string pathway, string profile, decimal fee, int normalMin)
            {
                if (!await db.Service.AnyAsync(s => s.Name == name))
                {
                    db.Service.Add(new Service
                    {
                        Name = name,
                        Category = category,
                        Fee = fee,
                        NormalMinutes = normalMin,
                        UrgentMinutes = Math.Max(30, normalMin / 2),
                        Pathway = pathway,
                        ReportProfile = profile,
                        CanSendOut = false,
                        Status = true,
                        CreatedOn = DateTime.UtcNow
                    });
                    await db.SaveChangesAsync();
                    return;
                }

                var svc = await db.Service.FirstAsync(s => s.Name == name);
                var changed = false;
                if (svc.Pathway != pathway)
                {
                    svc.Pathway = pathway;
                    svc.Category = category;
                    changed = true;
                }
                if (string.IsNullOrWhiteSpace(svc.ReportProfile) ||
                    (svc.ReportProfile == ReportProfiles.Standard && profile != ReportProfiles.Standard) ||
                    (profile == ReportProfiles.PhysicalExam && svc.ReportProfile != ReportProfiles.PhysicalExam) ||
                    (profile == ReportProfiles.Radiology && svc.ReportProfile != ReportProfiles.Radiology))
                {
                    svc.ReportProfile = profile;
                    changed = true;
                }
                if (changed)
                {
                    await db.SaveChangesAsync();
                }
            }

            await EnsureServiceAsync("Physical Examination", "Physical", ServicePathways.Physical, ReportProfiles.PhysicalExam, 2500, 30);
            await EnsureServiceAsync("Anti-HIV", "Laboratory", ServicePathways.Laboratory, ReportProfiles.Standard, 1800, 240);
            await EnsureServiceAsync("HBsAg", "Laboratory", ServicePathways.Laboratory, ReportProfiles.Standard, 1500, 240);
            await EnsureServiceAsync("Anti-HCV", "Laboratory", ServicePathways.Laboratory, ReportProfiles.Standard, 1800, 240);
            await EnsureServiceAsync("VDRL", "Laboratory", ServicePathways.Laboratory, ReportProfiles.Standard, 1200, 240);

            // Prefer existing chest x-ray naming
            if (!await db.Service.AnyAsync(s => s.Name == "Chest X-ray" || s.Name.Contains("Chest X-ray") || s.Name.Contains("X-ray")))
            {
                await EnsureServiceAsync("Chest X-ray", "Radiology", ServicePathways.Radiology, ReportProfiles.Radiology, 2500, 60);
            }
            else
            {
                var xray = await db.Service.FirstAsync(s => s.Name == "Chest X-ray" || s.Name.Contains("Chest X-ray") || s.Name.Contains("X-ray"));
                if (xray.Name != "Chest X-ray" && !await db.Service.AnyAsync(s => s.Name == "Chest X-ray"))
                {
                    // Keep existing; package lookup also matches Contains in controller? Package uses exact names.
                    // Add alias service only if no exact "Chest X-ray"
                    await EnsureServiceAsync("Chest X-ray", "Radiology", ServicePathways.Radiology, ReportProfiles.Radiology, xray.Fee, 60);
                }
                else if (xray.Pathway != ServicePathways.Radiology)
                {
                    xray.Pathway = ServicePathways.Radiology;
                    xray.ReportProfile = ReportProfiles.Radiology;
                    await db.SaveChangesAsync();
                }
            }
        }

        private static async Task SeedMedicalCertificateDemoAsync(ClinoraContext db)
        {
            var day = DateTime.Today.ToString("yyyyMMdd");
            var names = new[]
            {
                "Physical Examination", "Anti-HIV", "HBsAg", "Anti-HCV", "VDRL", "Chest X-ray", "Hepatitis B vaccine"
            };
            var services = await db.Service.AsNoTracking()
                .Where(s => names.Contains(s.Name) && s.Status)
                .ToListAsync();
            if (services.Count == 0)
            {
                return;
            }

            var branchId = await db.Branch.AsNoTracking().OrderBy(b => b.Id).Select(b => (int?)b.Id).FirstOrDefaultAsync();
            var now = DateTime.Now;

            // Pending package (work-in-progress queue)
            var pendingNo = $"DEMO-WAF-{day}";
            if (!await db.Visit.AsNoTracking().AnyAsync(v => v.VisitNumber == pendingNo))
            {
                var patient = new Patient
                {
                    FullName = "Omar Siddiqui (Demo Wafid)",
                    Phone = "03001234007",
                    Age = 29,
                    Gender = "Male",
                    PassportNo = "AB1234567",
                    Status = true,
                    BranchId = branchId,
                    CreatedOn = now
                };
                db.Patient.Add(patient);
                await db.SaveChangesAsync();

                var fee = services.Sum(s => s.Fee);
                var visit = new Visit
                {
                    VisitNumber = pendingNo,
                    PatientId = patient.Id,
                    TokenNo = Random.Shared.Next(10, 99),
                    VisitStatus = VisitStatuses.Active,
                    Purpose = VisitPurposes.MedicalCertificate,
                    Fees = fee,
                    Net = fee,
                    AmountReceived = fee,
                    PaymentReceived = true,
                    BranchId = branchId,
                    Notes = "Demo Wafid / Gulf medical package",
                    CreatedAt = now
                };
                db.Visit.Add(visit);
                await db.SaveChangesAsync();

                foreach (var svc in services)
                {
                    var status = svc.Pathway switch
                    {
                        ServicePathways.Physical => VisitServiceStatuses.AwaitingExamination,
                        ServicePathways.Radiology => VisitServiceStatuses.AwaitingImaging,
                        ServicePathways.Vaccination => VisitServiceStatuses.AwaitingAdministration,
                        ServicePathways.Consultation => VisitServiceStatuses.Completed,
                        _ => VisitServiceStatuses.AwaitingCollection
                    };
                    db.VisitService.Add(new VisitService
                    {
                        VisitId = visit.Id,
                        ServiceId = svc.Id,
                        Fee = svc.Fee,
                        Status = status
                    });
                }

                db.MedicalCertificate.Add(new MedicalCertificate
                {
                    VisitId = visit.Id,
                    Fitness = CertificateFitness.Pending,
                    DestinationCountry = "Saudi Arabia"
                });
                await db.SaveChangesAsync();
            }

            // Fully completed + Fit — ready for final Wafid-style print (also on report collection)
            var printNo = $"DEMO-WAF-FIT-{day}";
            if (await db.Visit.AsNoTracking().AnyAsync(v => v.VisitNumber == printNo))
            {
                await EnsureCertificateReportsForIssuedAsync(db);
                return;
            }

            var fitPatient = new Patient
            {
                FullName = "Mohsin Sharif (Demo Fit)",
                Phone = "03001234008",
                Age = 23,
                Gender = "Male",
                PassportNo = "PK9988776",
                Status = true,
                BranchId = branchId,
                CreatedOn = now.AddHours(-5)
            };
            db.Patient.Add(fitPatient);
            await db.SaveChangesAsync();

            var fitFee = services.Sum(s => s.Fee);
            var fitVisit = new Visit
            {
                VisitNumber = printNo,
                PatientId = fitPatient.Id,
                TokenNo = Random.Shared.Next(10, 99),
                VisitStatus = VisitStatuses.Active,
                Purpose = VisitPurposes.MedicalCertificate,
                Fees = fitFee,
                Net = fitFee,
                AmountReceived = fitFee,
                PaymentReceived = true,
                BranchId = branchId,
                Notes = "Demo completed Wafid report for final print",
                CreatedAt = now.AddHours(-5)
            };
            db.Visit.Add(fitVisit);
            await db.SaveChangesAsync();

            var physicalJson = """
                {"profile":"PhysicalExam","nationality":"Pakistani","maritalStatus":"Single","profession":"Labour","passportExpiry":"15/08/2030","heightCm":"165","weightKg":"60","bmi":"22.04","bp":"110/70","pulse":"80","rr":"18","visionRight":"6/6","visionLeft":"6/6","visionNearRight":"N5","visionNearLeft":"N5","colorVision":"Normal","hearingRight":"Normal","hearingLeft":"Normal","respiratory":"NAD","cardiovascular":"NAD","gastro":"NAD","nervous":"NAD","genitourinary":"NAD","musculoskeletal":"NAD","others":"NAD","appearance":"NAD","speech":"NAD","cognition":"NAD","orientation":"NAD","generalAppearance":"Well oriented","systemicNotes":"","fitnessComment":"Fit for employment as per GCC criteria."}
                """;
            var labResults = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Anti-HIV"] = "Negative",
                ["HBsAg"] = "Negative",
                ["Anti-HCV"] = "Negative",
                ["VDRL"] = "Negative"
            };
            var xrayHtml = "<p><strong>FINDINGS:</strong></p><p>No active lung lesion. Cardiomediastinal silhouette within normal limits.</p><p><strong>IMPRESSION:</strong></p><p>NAD</p>";

            foreach (var svc in services)
            {
                string status;
                string? resultText = null;
                string? paramName = null;
                string source = "Demo";

                if (svc.Pathway == ServicePathways.Physical)
                {
                    status = VisitServiceStatuses.Examined;
                    resultText = physicalJson;
                    paramName = "Physical examination";
                    source = "Physical";
                }
                else if (svc.Pathway == ServicePathways.Radiology)
                {
                    status = VisitServiceStatuses.Finalized;
                    resultText = xrayHtml;
                    paramName = "Chest X-ray — PA";
                    source = "Radiology";
                }
                else if (svc.Pathway == ServicePathways.Vaccination)
                {
                    status = VisitServiceStatuses.Administered;
                }
                else
                {
                    status = VisitServiceStatuses.Finalized;
                    resultText = labResults.TryGetValue(svc.Name, out var lab) ? lab : "Normal";
                    paramName = svc.Name;
                }

                var line = new VisitService
                {
                    VisitId = fitVisit.Id,
                    ServiceId = svc.Id,
                    Fee = svc.Fee,
                    Status = status,
                    ResultEnteredAt = now.AddHours(-3),
                    FinalizedAt = now.AddHours(-2),
                    FinalizedByName = "Dr. Ayisha Demo"
                };
                db.VisitService.Add(line);
                await db.SaveChangesAsync();

                if (resultText != null)
                {
                    db.TestResult.Add(new TestResult
                    {
                        VisitServiceId = line.Id,
                        Source = source,
                        ResultText = resultText,
                        ParameterName = paramName,
                        EnteredByName = "Dr. Ayisha Demo",
                        EnteredAt = now.AddHours(-3),
                        Version = 1,
                        IsCurrent = true
                    });
                }

                if (svc.Pathway == ServicePathways.Vaccination)
                {
                    db.VaccinationRecord.Add(new VaccinationRecord
                    {
                        VisitServiceId = line.Id,
                        BatchNo = "HB-2401",
                        LotNo = "L7788",
                        ExpiryDate = now.AddYears(1).Date,
                        DoseNumber = "1st",
                        Site = "Left deltoid",
                        Route = "IM",
                        AdministeredAt = now.AddHours(-2),
                        AdministeredByName = "Nurse Demo",
                        Notes = "Demo Hep B"
                    });
                }
            }

            db.MedicalCertificate.Add(new MedicalCertificate
            {
                VisitId = fitVisit.Id,
                Fitness = CertificateFitness.Fit,
                DestinationCountry = "Saudi Arabia",
                Remarks = "Fit according to GCC / Wafid medical criteria (demo).",
                IssuedByName = "Dr. Ayisha Demo",
                // Keep on local "today" so Report collection Today list includes it after midnight seed.
                IssuedAt = now
            });
            await db.SaveChangesAsync();

            await EnsureCertificateReportsForIssuedAsync(db);
        }

        /// <summary>
        /// Issued medical certificates appear on Report collection via Report rows.
        /// </summary>
        private static async Task EnsureCertificateReportsForIssuedAsync(ClinoraContext db)
        {
            var issued = await db.MedicalCertificate.AsNoTracking()
                .Where(c => c.IssuedAt != null && c.Fitness != CertificateFitness.Pending)
                .ToListAsync();

            var todayStart = DateTime.Today;

            foreach (var cert in issued)
            {
                var existingReport = await db.Report.FirstOrDefaultAsync(r =>
                    r.VisitId == cert.VisitId && r.Pathway == VisitPurposes.MedicalCertificate);

                // Demo / overnight: if cert is for today's visit number but FinalizedAt slipped to yesterday, bump it.
                if (existingReport != null)
                {
                    var visitNo = await db.Visit.AsNoTracking()
                        .Where(v => v.Id == cert.VisitId)
                        .Select(v => v.VisitNumber)
                        .FirstOrDefaultAsync();
                    if (visitNo != null
                        && visitNo.StartsWith("DEMO-WAF-FIT-", StringComparison.OrdinalIgnoreCase)
                        && existingReport.FinalizedAt < todayStart)
                    {
                        existingReport.FinalizedAt = DateTime.Now;
                        var trackedCert = await db.MedicalCertificate.FirstAsync(c => c.Id == cert.Id);
                        trackedCert.IssuedAt = existingReport.FinalizedAt;
                        await db.SaveChangesAsync();
                    }
                    continue;
                }

                var lines = await (
                    from vs in db.VisitService.AsNoTracking()
                    join s in db.Service.AsNoTracking() on vs.ServiceId equals s.Id
                    where vs.VisitId == cert.VisitId
                    orderby s.Pathway, s.Name
                    select new { vs, s }
                ).ToListAsync();

                if (lines.Count == 0)
                {
                    continue;
                }

                var report = new Report
                {
                    VisitId = cert.VisitId,
                    Pathway = VisitPurposes.MedicalCertificate,
                    FinalizedByName = cert.IssuedByName ?? "Doctor",
                    FinalizedAt = cert.IssuedAt ?? DateTime.Now,
                    PrintCount = 0
                };
                db.Report.Add(report);
                await db.SaveChangesAsync();

                var vsIds = lines.Select(l => l.vs.Id).ToList();
                var results = await db.TestResult.AsNoTracking()
                    .Where(r => vsIds.Contains(r.VisitServiceId) && r.IsCurrent)
                    .OrderByDescending(r => r.Version)
                    .ToListAsync();
                var resultMap = results
                    .GroupBy(r => r.VisitServiceId)
                    .ToDictionary(g => g.Key, g => g.First());

                foreach (var row in lines)
                {
                    resultMap.TryGetValue(row.vs.Id, out var tr);
                    var text = tr?.ResultText;
                    if (row.s.Pathway == ServicePathways.Vaccination && string.IsNullOrWhiteSpace(text))
                    {
                        text = "Administered";
                    }

                    db.ReportLine.Add(new ReportLine
                    {
                        ReportId = report.Id,
                        VisitServiceId = row.vs.Id,
                        ServiceName = row.s.Name,
                        ParameterName = tr?.ParameterName
                            ?? (row.s.Pathway == ServicePathways.Physical ? "Physical examination" : null),
                        ResultText = text,
                        Units = tr?.Units,
                        ReferenceRange = tr?.ReferenceRange,
                        ReportProfile = string.IsNullOrWhiteSpace(row.s.ReportProfile)
                            ? ReportProfiles.Standard
                            : row.s.ReportProfile,
                        Urgent = row.vs.Urgent,
                        IsAbnormal = row.vs.IsAbnormal,
                        IsCritical = row.vs.IsCritical
                    });
                }

                db.ReportLine.Add(new ReportLine
                {
                    ReportId = report.Id,
                    VisitServiceId = lines[0].vs.Id,
                    ServiceName = $"Fitness: {cert.Fitness}",
                    ParameterName = "Certificate",
                    ResultText = cert.Remarks,
                    ReportProfile = ReportProfiles.Standard,
                    Urgent = false
                });
                await db.SaveChangesAsync();
            }
        }

        private static async Task EnsureVaccinationServicesAsync(ClinoraContext db)
        {
            await db.Database.ExecuteSqlRawAsync("""
                IF OBJECT_ID('dbo.VaccinationRecord','U') IS NULL
                CREATE TABLE dbo.VaccinationRecord (
                  Id INT IDENTITY(1,1) PRIMARY KEY,
                  VisitServiceId INT NOT NULL,
                  BatchNo NVARCHAR(80) NULL,
                  LotNo NVARCHAR(80) NULL,
                  ExpiryDate DATETIME2 NULL,
                  DoseNumber NVARCHAR(40) NULL,
                  Site NVARCHAR(80) NULL,
                  Route NVARCHAR(80) NULL,
                  AdministeredAt DATETIME2 NOT NULL,
                  NextDueAt DATETIME2 NULL,
                  AdministeredByName NVARCHAR(200) NULL,
                  Notes NVARCHAR(500) NULL
                );
                """);

            var vaccines = new (string Name, decimal Fee)[]
            {
                ("Hepatitis B vaccine", 1500),
                ("Tetanus toxoid (TT)", 800),
                ("Influenza vaccine", 2200),
                ("COVID-19 booster", 0),
                ("IPV (polio injectable)", 1200)
            };

            foreach (var (name, fee) in vaccines)
            {
                if (!await db.Service.AnyAsync(s => s.Name == name))
                {
                    db.Service.Add(new Service
                    {
                        Name = name,
                        Category = "Vaccination",
                        Fee = fee,
                        NormalMinutes = 15,
                        UrgentMinutes = 10,
                        Pathway = ServicePathways.Vaccination,
                        ReportProfile = ReportProfiles.Standard,
                        CanSendOut = false,
                        Status = true,
                        CreatedOn = DateTime.UtcNow
                    });
                }
                else
                {
                    var svc = await db.Service.FirstAsync(s => s.Name == name);
                    var changed = false;
                    if (svc.Pathway != ServicePathways.Vaccination)
                    {
                        svc.Pathway = ServicePathways.Vaccination;
                        svc.Category = "Vaccination";
                        changed = true;
                    }
                    if (string.IsNullOrWhiteSpace(svc.ReportProfile))
                    {
                        svc.ReportProfile = ReportProfiles.Standard;
                        changed = true;
                    }
                    if (changed)
                    {
                        await db.SaveChangesAsync();
                    }
                }
            }

            await db.SaveChangesAsync();
        }

        private static async Task SeedVaccinationDemoAsync(ClinoraContext db)
        {
            var day = DateTime.Today.ToString("yyyyMMdd");
            var visitNumber = $"DEMO-VAC-{day}";
            if (await db.Visit.AsNoTracking().AnyAsync(v => v.VisitNumber == visitNumber))
            {
                return;
            }

            var vaccine = await db.Service.AsNoTracking()
                .FirstOrDefaultAsync(s => s.Pathway == ServicePathways.Vaccination && s.Name == "Hepatitis B vaccine");
            if (vaccine == null)
            {
                vaccine = await db.Service.AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Pathway == ServicePathways.Vaccination);
            }
            if (vaccine == null)
            {
                return;
            }

            var branchId = await db.Branch.AsNoTracking().OrderBy(b => b.Id).Select(b => (int?)b.Id).FirstOrDefaultAsync();
            var now = DateTime.Now;
            var patient = new Patient
            {
                FullName = "Ayesha Khan (Demo Vaccine)",
                Phone = "03001234006",
                Age = 28,
                Gender = "Female",
                Status = true,
                BranchId = branchId,
                CreatedOn = now
            };
            db.Patient.Add(patient);
            await db.SaveChangesAsync();

            var visit = new Visit
            {
                VisitNumber = visitNumber,
                PatientId = patient.Id,
                TokenNo = Random.Shared.Next(10, 99),
                VisitStatus = VisitStatuses.Active,
                Fees = vaccine.Fee,
                Net = vaccine.Fee,
                AmountReceived = vaccine.Fee,
                PaymentReceived = true,
                BranchId = branchId,
                Notes = "Demo vaccination awaiting administration",
                CreatedAt = now
            };
            db.Visit.Add(visit);
            await db.SaveChangesAsync();

            db.VisitService.Add(new VisitService
            {
                VisitId = visit.Id,
                ServiceId = vaccine.Id,
                Fee = vaccine.Fee,
                Status = VisitServiceStatuses.AwaitingAdministration
            });
            await db.SaveChangesAsync();
        }

        /// <summary>
        /// Creates today's sample finalized reports (CBC, Semen, Histo, Chest X-ray) for report-collection desk.
        /// </summary>
        private static async Task SeedTodayDemoReportsAsync(ClinoraContext db)
        {
            var day = DateTime.Today.ToString("yyyyMMdd");
            var demos = new[]
            {
                $"DEMO-CBC-{day}",
                $"DEMO-SEM-{day}",
                $"DEMO-HIS-{day}",
                $"DEMO-XRY-{day}",
                $"DEMO-ALL-{day}",
                $"DEMO-IHC-{day}"
            };
            var existing = await db.Visit.AsNoTracking()
                .Where(v => demos.Contains(v.VisitNumber))
                .Select(v => v.VisitNumber)
                .ToListAsync();
            var need = demos.Where(d => !existing.Contains(d)).ToHashSet();
            if (need.Count == 0)
            {
                return;
            }

            var branchId = await db.Branch.AsNoTracking().OrderBy(b => b.Id).Select(b => (int?)b.Id).FirstOrDefaultAsync();
            var cbc = await db.Service.AsNoTracking().FirstOrDefaultAsync(s => s.Name == "CBC");
            var semen = await db.Service.AsNoTracking().FirstOrDefaultAsync(s => s.Name == "Semen Analysis");
            var histo = await db.Service.AsNoTracking().FirstOrDefaultAsync(s => s.Name == "Histopathology");
            var xray = await db.Service.AsNoTracking().FirstOrDefaultAsync(s =>
                s.Name.Contains("X-ray") || s.Name.Contains("Xray"));
            var ihc = await db.Service.AsNoTracking().FirstOrDefaultAsync(s => s.Name == "IHC Breast Panel");

            if (cbc == null && semen == null && histo == null && xray == null && ihc == null)
            {
                return;
            }

            var now = DateTime.Now;
            var cbcParams = new List<(string? Param, string Result, string? Units, string? Ref)>
            {
                ("Hemoglobin", "14.2", "g/dL", "12–16"),
                ("WBC", "7.5", "10^3/uL", "4–11"),
                ("Platelets", "250", "10^3/uL", "150–450")
            };

            async Task AddDemoAsync(
                string visitNumber,
                string patientName,
                string phone,
                int age,
                string gender,
                Service service,
                string pathway,
                string reportProfile,
                List<(string? Param, string Result, string? Units, string? Ref)> results,
                string doctor)
            {
                var patient = new Patient
                {
                    FullName = patientName,
                    Phone = phone,
                    Age = age,
                    Gender = gender,
                    Status = true,
                    BranchId = branchId,
                    CreatedOn = now
                };
                db.Patient.Add(patient);
                await db.SaveChangesAsync();

                var visit = new Visit
                {
                    VisitNumber = visitNumber,
                    PatientId = patient.Id,
                    TokenNo = Random.Shared.Next(10, 99),
                    VisitStatus = VisitStatuses.Active,
                    Fees = service.Fee,
                    Net = service.Fee,
                    AmountReceived = service.Fee,
                    PaymentReceived = true,
                    BranchId = branchId,
                    Notes = "Demo seed for report collection",
                    CreatedAt = now
                };
                db.Visit.Add(visit);
                await db.SaveChangesAsync();

                var line = new VisitService
                {
                    VisitId = visit.Id,
                    ServiceId = service.Id,
                    Fee = service.Fee,
                    Status = VisitServiceStatuses.Finalized,
                    ResultEnteredAt = now.AddMinutes(-30),
                    FinalizedAt = now.AddMinutes(-5),
                    FinalizedByName = doctor
                };
                db.VisitService.Add(line);
                await db.SaveChangesAsync();

                var report = new Report
                {
                    VisitId = visit.Id,
                    Pathway = pathway,
                    FinalizedByName = doctor,
                    FinalizedAt = now.AddMinutes(-5),
                    PrintCount = 0
                };
                db.Report.Add(report);
                await db.SaveChangesAsync();

                foreach (var row in results)
                {
                    db.TestResult.Add(new TestResult
                    {
                        VisitServiceId = line.Id,
                        Source = "Demo",
                        ResultText = row.Result,
                        Units = row.Units,
                        ReferenceRange = row.Ref,
                        ParameterName = row.Param,
                        EnteredByName = doctor,
                        EnteredAt = now.AddMinutes(-30),
                        Version = 1,
                        IsCurrent = true
                    });
                    db.ReportLine.Add(new ReportLine
                    {
                        ReportId = report.Id,
                        VisitServiceId = line.Id,
                        ServiceName = service.Name,
                        ParameterName = row.Param,
                        ResultText = row.Result,
                        Units = row.Units,
                        ReferenceRange = row.Ref,
                        ReportProfile = reportProfile,
                        Urgent = false
                    });
                }

                await db.SaveChangesAsync();
            }

            var semenJson = """
                {"profile":"Semen","abstinenceDays":"3","volumeMl":"2.5","color":"Grey-white","viscosity":"Normal","liquefactionMin":"25","ph":"7.4","concentration":"45","totalCount":"112","progressiveMotility":"42","nonProgressiveMotility":"18","immotile":"40","normalMorphology":"8","wbc":"Occasional","impression":"Normozoospermia — within reference ranges."}
                """;
            var histoHtml = """
                <p><strong>CLINICAL HISTORY:</strong></p><p>Right breast lump for 2 months.</p>
                <p><strong>SPECIMEN:</strong></p><p>Excision biopsy — right breast lump.</p>
                <p><strong>GROSS:</strong></p><p>Grey-white tissue 1.8 × 1.2 × 0.8 cm.</p>
                <p><strong>MICROSCOPIC:</strong></p><p>Sections show fibroadenoma with stromal and epithelial components. No malignancy seen.</p>
                <p><strong>DIAGNOSIS:</strong></p><p>Fibroadenoma, right breast.</p>
                """;
            var xrayHtml = """
                <p><strong>CLINICAL HISTORY:</strong></p><p>Cough · fever.</p>
                <p><strong>TECHNIQUE:</strong></p><p>Frontal chest radiograph (PA).</p>
                <p><strong>FINDINGS:</strong></p><p>The lungs are clear. No focal consolidation, pleural effusion, or pneumothorax. Cardiomediastinal silhouette within normal limits.</p>
                <p><strong>IMPRESSION:</strong></p><p>No acute cardiopulmonary process.</p>
                """;

            if (need.Contains(demos[0]) && cbc != null)
            {
                await AddDemoAsync(
                    demos[0],
                    "Ahmed Khan (Demo CBC)",
                    "03001234001",
                    32,
                    "Male",
                    cbc,
                    ServicePathways.Laboratory,
                    ReportProfiles.Standard,
                    cbcParams,
                    "Dr. Demo Pathologist");
            }

            if (need.Contains(demos[1]) && semen != null)
            {
                await AddDemoAsync(
                    demos[1],
                    "Bilal Hussain (Demo Semen)",
                    "03001234002",
                    28,
                    "Male",
                    semen,
                    ServicePathways.Laboratory,
                    ReportProfiles.Semen,
                    [("Semen worksheet", semenJson, null, null)],
                    "Dr. Demo Pathologist");
            }

            if (need.Contains(demos[2]) && histo != null)
            {
                await AddDemoAsync(
                    demos[2],
                    "Sara Ali (Demo Histo)",
                    "03001234003",
                    26,
                    "Female",
                    histo,
                    ServicePathways.Laboratory,
                    ReportProfiles.Narrative,
                    [("Histopathology — routine", histoHtml, null, null)],
                    "Dr. Demo Pathologist");
            }

            if (need.Contains(demos[3]) && xray != null)
            {
                await AddDemoAsync(
                    demos[3],
                    "Usman Raza (Demo X-ray)",
                    "03001234004",
                    40,
                    "Male",
                    xray,
                    ServicePathways.Radiology,
                    ReportProfiles.Radiology,
                    [("Chest X-ray — PA", xrayHtml, null, null)],
                    "Dr. Demo Radiologist");
            }

            var ihcJson = """
                {"profile":"Ihc","panelName":"Breast panel","markers":[{"code":"ER","name":"Estrogen receptor","result":"Positive","percent":"90","intensity":"Strong","score":"8/8","notes":""},{"code":"PR","name":"Progesterone receptor","result":"Positive","percent":"70","intensity":"Moderate","score":"6/8","notes":""},{"code":"HER2","name":"HER2 / neu","result":"Negative","percent":"","intensity":"0","score":"0","notes":""},{"code":"KI67","name":"Ki-67","result":"Positive","percent":"15","intensity":"","score":"","notes":"Low proliferative index"}],"interpretation":"ER+/PR+/HER2− · Luminal A–like phenotype."}
                """;

            if (need.Contains(demos[5]) && ihc != null)
            {
                await AddDemoAsync(
                    demos[5],
                    "Nadia Fatima (Demo IHC)",
                    "03001234005",
                    48,
                    "Female",
                    ihc,
                    ServicePathways.Laboratory,
                    ReportProfiles.Ihc,
                    [("IHC worksheet", ihcJson, null, null)],
                    "Dr. Demo Pathologist");
            }

            // Demo-only combined card (CBC+Semen+Histo+X-ray). Production flow usually splits
            // radiology (sign) from lab (doctor finalize) into separate reports.
            if (need.Contains(demos[4]))
            {
                var services = new List<(Service Service, string Profile, List<(string? Param, string Result, string? Units, string? Ref)> Rows)>();
                if (cbc != null)
                {
                    services.Add((cbc, ReportProfiles.Standard, cbcParams));
                }
                if (semen != null)
                {
                    services.Add((semen, ReportProfiles.Semen, [("Semen worksheet", semenJson, null, null)]));
                }
                if (histo != null)
                {
                    services.Add((histo, ReportProfiles.Narrative, [("Histopathology — routine", histoHtml, null, null)]));
                }
                if (xray != null)
                {
                    services.Add((xray, ReportProfiles.Radiology, [("Chest X-ray — PA", xrayHtml, null, null)]));
                }

                if (services.Count > 0)
                {
                    var patient = new Patient
                    {
                        FullName = "Hassan Malik (Demo All-in-one)",
                        Phone = "03001234999",
                        Age = 35,
                        Gender = "Male",
                        Status = true,
                        BranchId = branchId,
                        CreatedOn = now
                    };
                    db.Patient.Add(patient);
                    await db.SaveChangesAsync();

                    var fee = services.Sum(s => s.Service.Fee);
                    var visit = new Visit
                    {
                        VisitNumber = demos[4],
                        PatientId = patient.Id,
                        TokenNo = Random.Shared.Next(10, 99),
                        VisitStatus = VisitStatuses.Active,
                        Fees = fee,
                        Net = fee,
                        AmountReceived = fee,
                        PaymentReceived = true,
                        BranchId = branchId,
                        Notes = "Demo: one visit with CBC + Semen + Histo + X-ray",
                        CreatedAt = now
                    };
                    db.Visit.Add(visit);
                    await db.SaveChangesAsync();

                    var pathways = services.Select(s => s.Service.Pathway).Distinct().ToList();
                    var pathway = pathways.Count == 1
                        ? pathways[0]
                        : pathways.Contains(ServicePathways.Radiology) && pathways.Contains(ServicePathways.Laboratory)
                            ? "Mixed"
                            : ServicePathways.Laboratory;

                    var report = new Report
                    {
                        VisitId = visit.Id,
                        Pathway = pathway,
                        FinalizedByName = "Dr. Demo",
                        FinalizedAt = now.AddMinutes(-2),
                        PrintCount = 0
                    };
                    db.Report.Add(report);
                    await db.SaveChangesAsync();

                    foreach (var item in services)
                    {
                        var line = new VisitService
                        {
                            VisitId = visit.Id,
                            ServiceId = item.Service.Id,
                            Fee = item.Service.Fee,
                            Status = VisitServiceStatuses.Finalized,
                            ResultEnteredAt = now.AddMinutes(-40),
                            FinalizedAt = now.AddMinutes(-2),
                            FinalizedByName = "Dr. Demo"
                        };
                        db.VisitService.Add(line);
                        await db.SaveChangesAsync();

                        foreach (var row in item.Rows)
                        {
                            db.TestResult.Add(new TestResult
                            {
                                VisitServiceId = line.Id,
                                Source = "Demo",
                                ResultText = row.Result,
                                Units = row.Units,
                                ReferenceRange = row.Ref,
                                ParameterName = row.Param,
                                EnteredByName = "Dr. Demo",
                                EnteredAt = now.AddMinutes(-40),
                                Version = 1,
                                IsCurrent = true
                            });
                            db.ReportLine.Add(new ReportLine
                            {
                                ReportId = report.Id,
                                VisitServiceId = line.Id,
                                ServiceName = item.Service.Name,
                                ParameterName = row.Param,
                                ResultText = row.Result,
                                Units = row.Units,
                                ReferenceRange = row.Ref,
                                ReportProfile = item.Profile,
                                Urgent = false
                            });
                        }

                        await db.SaveChangesAsync();
                    }
                }
            }
        }
    }
}
