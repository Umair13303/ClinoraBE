namespace ClinoraBE.Model.DAL
{
    public partial class BottleColor
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool Status { get; set; } = true;
    }

    public partial class SampleType
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool Status { get; set; } = true;
    }

    public partial class SampleGroup
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool Status { get; set; } = true;
    }

    public partial class Container
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int? DefaultBottleColorId { get; set; }
        public bool Status { get; set; } = true;
    }

    public partial class OutsideLab
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public bool Status { get; set; } = true;
    }

    public partial class LabSample
    {
        public int Id { get; set; }
        public int VisitId { get; set; }
        public int? SampleGroupId { get; set; }
        public int SampleTypeId { get; set; }
        public int ContainerId { get; set; }
        public int BottleColorId { get; set; }
        public string Barcode { get; set; } = string.Empty;
        public string? PhlebotomistName { get; set; }
        public DateTime CollectedAt { get; set; }
        public bool IsSendOut { get; set; }
        public int? OutsideLabId { get; set; }
    }

    public partial class SampleTest
    {
        public int Id { get; set; }
        public int SampleId { get; set; }
        public int VisitServiceId { get; set; }
    }

    public partial class OutsideLabShipment
    {
        public int Id { get; set; }
        public int OutsideLabId { get; set; }
        public string Status { get; set; } = "Sent";
        public string? Awb { get; set; }
        public string? Notes { get; set; }
        public DateTime SentAt { get; set; }
        public DateTime? ReceivedAt { get; set; }
        public int? SentBy { get; set; }
    }

    public partial class OutsideLabShipmentItem
    {
        public int Id { get; set; }
        public int ShipmentId { get; set; }
        public int SampleId { get; set; }
        public int VisitServiceId { get; set; }
    }

    public partial class TestResult
    {
        public int Id { get; set; }
        public int VisitServiceId { get; set; }
        public string Source { get; set; } = "InHouse";
        public string ResultText { get; set; } = string.Empty;
        public string? Units { get; set; }
        public string? ReferenceRange { get; set; }
        public string? EnteredByName { get; set; }
        public DateTime EnteredAt { get; set; }
    }

    public partial class Report
    {
        public int Id { get; set; }
        public int VisitId { get; set; }
        public string Pathway { get; set; } = string.Empty;
        public string? FinalizedByName { get; set; }
        public DateTime FinalizedAt { get; set; }
        public int PrintCount { get; set; }
    }
}
