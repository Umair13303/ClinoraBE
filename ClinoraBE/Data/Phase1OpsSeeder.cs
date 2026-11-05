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

            await SeedTodayDemoReportsAsync(db);
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
                $"DEMO-ALL-{day}"
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

            if (cbc == null && semen == null && histo == null && xray == null)
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

            // One patient, one visit, all four tests → one combined report card.
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
