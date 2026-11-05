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

    /// <summary>IHC antibody catalog (ER, PR, HER2, …) for worksheet pick-lists.</summary>
    public partial class Antibody
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
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
        public DateTime? ReceivedAt { get; set; }
        public string? ReceivedBy { get; set; }
        public bool IsSendOut { get; set; }
        public int? OutsideLabId { get; set; }
        public string SampleStatus { get; set; } = "Collected";
        public string? RejectReason { get; set; }
        public DateTime? RejectedAt { get; set; }
        public string? RejectedByName { get; set; }
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
        public DateTime? ExpectedAt { get; set; }
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
        public int Version { get; set; } = 1;
        public bool IsCurrent { get; set; } = true;
        public bool IsAbnormal { get; set; }
        public bool IsCritical { get; set; }
        public string? ParameterName { get; set; }
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

    public partial class ReportLine
    {
        public int Id { get; set; }
        public int ReportId { get; set; }
        public int VisitServiceId { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public string? ParameterName { get; set; }
        public string? ResultText { get; set; }
        public string? Units { get; set; }
        public string? ReferenceRange { get; set; }
        public string ReportProfile { get; set; } = "Standard";
        public bool Urgent { get; set; }
        public bool IsAbnormal { get; set; }
        public bool IsCritical { get; set; }
    }

    public partial class RadiologyTemplate
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Modality { get; set; } = "XRay";
        /// <summary>Radiology | Narrative (lab histo/cyto templates).</summary>
        public string Profile { get; set; } = "Radiology";
        /// <summary>Legacy single-service link; prefer RadiologyTemplateService rows.</summary>
        public int? ServiceId { get; set; }
        public string Body { get; set; } = string.Empty;
        public bool Status { get; set; } = true;
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    }

    public partial class RadiologyTemplateService
    {
        public int Id { get; set; }
        public int TemplateId { get; set; }
        public int ServiceId { get; set; }
    }

    public partial class ServiceParameter
    {
        public int Id { get; set; }
        public int ServiceId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Unit { get; set; }
        public decimal? RefLow { get; set; }
        public decimal? RefHigh { get; set; }
        public int SortOrder { get; set; }
        public bool Status { get; set; } = true;
    }

    public partial class VaccinationRecord
    {
        public int Id { get; set; }
        public int VisitServiceId { get; set; }
        public string? BatchNo { get; set; }
        public string? LotNo { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string? DoseNumber { get; set; }
        public string? Site { get; set; }
        public string? Route { get; set; }
        public DateTime AdministeredAt { get; set; }
        public string? AdministeredByName { get; set; }
        public DateTime? NextDueAt { get; set; }
        public string? Notes { get; set; }
    }

    public partial class MedicalCertificate
    {
        public int Id { get; set; }
        public int VisitId { get; set; }
        public string Fitness { get; set; } = "Pending";
        public string? DestinationCountry { get; set; }
        public string? Remarks { get; set; }
        public string? IssuedByName { get; set; }
        public DateTime? IssuedAt { get; set; }
    }
}
