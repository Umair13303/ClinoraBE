namespace ClinoraBE.Model.ViewModel
{
    public static class VisitServiceStatuses
    {
        public const string Completed = "Completed";
        public const string AwaitingCollection = "AwaitingCollection";
        public const string Collected = "Collected";
        public const string InLab = "InLab";
        public const string SentOut = "SentOut";
        public const string ResultEntered = "ResultEntered";
        public const string AwaitingImaging = "AwaitingImaging";
        public const string Imaged = "Imaged";
        public const string Finalized = "Finalized";
    }

    public static class ServicePathways
    {
        public const string Consultation = "Consultation";
        public const string Laboratory = "Laboratory";
        public const string Radiology = "Radiology";
    }

    public class LookupItemDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    public class OutsideLabDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Phone { get; set; }
    }

    public class SampleSetupDto
    {
        public List<LookupItemDto> SampleTypes { get; set; } = [];
        public List<LookupItemDto> Containers { get; set; } = [];
        public List<LookupItemDto> BottleColors { get; set; } = [];
        public List<LookupItemDto> SampleGroups { get; set; } = [];
        public List<OutsideLabDto> OutsideLabs { get; set; } = [];
    }

    public class CollectionLineDto
    {
        public int VisitServiceId { get; set; }
        public int VisitId { get; set; }
        public string VisitNumber { get; set; } = string.Empty;
        public string PatientName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public int ServiceId { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public bool Urgent { get; set; }
        public string Status { get; set; } = string.Empty;
        public int? SampleGroupId { get; set; }
        public string? SampleGroupCode { get; set; }
        public string? SampleGroupName { get; set; }
        public int? SampleTypeId { get; set; }
        public string? SampleTypeName { get; set; }
        public int? ContainerId { get; set; }
        public string? ContainerName { get; set; }
        public int? BottleColorId { get; set; }
        public string? BottleColorName { get; set; }
        public bool CanSendOut { get; set; }
        public int? DefaultOutsideLabId { get; set; }
    }

    public class CollectionGroupDto
    {
        public int VisitId { get; set; }
        public string VisitNumber { get; set; } = string.Empty;
        public string PatientName { get; set; } = string.Empty;
        public int? SampleGroupId { get; set; }
        public string GroupKey { get; set; } = string.Empty;
        public string GroupLabel { get; set; } = string.Empty;
        public string? SampleTypeName { get; set; }
        public string? ContainerName { get; set; }
        public string? BottleColorName { get; set; }
        public bool CanSendOut { get; set; }
        public List<CollectionLineDto> Lines { get; set; } = [];
    }

    public class CollectSampleRequest
    {
        public int VisitId { get; set; }
        public List<int> VisitServiceIds { get; set; } = [];
        public int SampleTypeId { get; set; }
        public int ContainerId { get; set; }
        public int BottleColorId { get; set; }
        public int? SampleGroupId { get; set; }
        public string? PhlebotomistName { get; set; }
        public bool IsSendOut { get; set; }
        public int? OutsideLabId { get; set; }
    }

    public class SampleBarcodeDto
    {
        public int SampleId { get; set; }
        public string Barcode { get; set; } = string.Empty;
        public string VisitNumber { get; set; } = string.Empty;
        public string PatientName { get; set; } = string.Empty;
        public string SampleTypeName { get; set; } = string.Empty;
        public string ContainerName { get; set; } = string.Empty;
        public string BottleColorName { get; set; } = string.Empty;
        public string? SampleGroupName { get; set; }
        public bool IsSendOut { get; set; }
        public string? OutsideLabName { get; set; }
        public DateTime CollectedAt { get; set; }
        public List<string> Tests { get; set; } = [];
    }

    public class ResultWorkItemDto
    {
        public int VisitServiceId { get; set; }
        public int VisitId { get; set; }
        public string VisitNumber { get; set; } = string.Empty;
        public string PatientName { get; set; } = string.Empty;
        public string ServiceName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public bool Urgent { get; set; }
        public string? Barcode { get; set; }
        public bool IsSendOut { get; set; }
        public string? ExistingResultText { get; set; }
    }

    public class SaveResultRequest
    {
        public int VisitServiceId { get; set; }
        public string ResultText { get; set; } = string.Empty;
        public string? Units { get; set; }
        public string? ReferenceRange { get; set; }
        public string? EnteredByName { get; set; }
        public string Source { get; set; } = "InHouse";
    }

    public class FinalizeRequest
    {
        public int VisitId { get; set; }
        public List<int> VisitServiceIds { get; set; } = [];
        public string? FinalizedByName { get; set; }
        public string Pathway { get; set; } = "Laboratory";
    }

    public class ReportDto
    {
        public int Id { get; set; }
        public int VisitId { get; set; }
        public string VisitNumber { get; set; } = string.Empty;
        public string PatientName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public int Age { get; set; }
        public string Gender { get; set; } = string.Empty;
        public string Pathway { get; set; } = string.Empty;
        public string? FinalizedByName { get; set; }
        public DateTime FinalizedAt { get; set; }
        public int PrintCount { get; set; }
        public List<ReportLineDto> Lines { get; set; } = [];
    }

    public class ReportLineDto
    {
        public string ServiceName { get; set; } = string.Empty;
        public string? ResultText { get; set; }
        public string? Units { get; set; }
        public string? ReferenceRange { get; set; }
        public bool Urgent { get; set; }
    }

    public class RadiologyWorkItemDto
    {
        public int VisitServiceId { get; set; }
        public int VisitId { get; set; }
        public string VisitNumber { get; set; } = string.Empty;
        public string PatientName { get; set; } = string.Empty;
        public string ServiceName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public bool Urgent { get; set; }
        public string? ReadyBy { get; set; }
    }

    public class RadiologyCompleteRequest
    {
        public int VisitServiceId { get; set; }
        public string? Findings { get; set; }
        public string? PerformedByName { get; set; }
    }

    public class CreateShipmentRequest
    {
        public int OutsideLabId { get; set; }
        public string? Awb { get; set; }
        public string? Notes { get; set; }
        public List<int> SampleIds { get; set; } = [];
    }

    public class ShipmentDto
    {
        public int Id { get; set; }
        public int OutsideLabId { get; set; }
        public string OutsideLabName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? Awb { get; set; }
        public string? Notes { get; set; }
        public DateTime SentAt { get; set; }
        public DateTime? ReceivedAt { get; set; }
        public List<ShipmentItemDto> Items { get; set; } = [];
    }

    public class ShipmentItemDto
    {
        public int SampleId { get; set; }
        public string Barcode { get; set; } = string.Empty;
        public int VisitServiceId { get; set; }
        public string VisitNumber { get; set; } = string.Empty;
        public string PatientName { get; set; } = string.Empty;
        public string ServiceName { get; set; } = string.Empty;
    }

    public class ReceiveShipmentRequest
    {
        public string? Notes { get; set; }
        public List<ReceiveShipmentItemRequest> Items { get; set; } = [];
    }

    public class ReceiveShipmentItemRequest
    {
        public int VisitServiceId { get; set; }
        public string ResultText { get; set; } = string.Empty;
        public string? Units { get; set; }
        public string? ReferenceRange { get; set; }
    }

    public class VisitSlipsDto
    {
        public int VisitId { get; set; }
        public string VisitNumber { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public int Age { get; set; }
        public string Gender { get; set; } = string.Empty;
        public string? ReferredBy { get; set; }
        public decimal Net { get; set; }
        public decimal AmountReceived { get; set; }
        public List<VisitSlipLineDto> Lines { get; set; } = [];
    }

    public class VisitSlipLineDto
    {
        public string ServiceName { get; set; } = string.Empty;
        public string Pathway { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public decimal Fee { get; set; }
        public bool Urgent { get; set; }
        public string? ReadyBy { get; set; }
        public string? SampleTypeName { get; set; }
        public string? ContainerName { get; set; }
        public string? BottleColorName { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class SendOutSampleDto
    {
        public int SampleId { get; set; }
        public string Barcode { get; set; } = string.Empty;
        public string VisitNumber { get; set; } = string.Empty;
        public string PatientName { get; set; } = string.Empty;
        public int? OutsideLabId { get; set; }
        public string? OutsideLabName { get; set; }
        public List<string> Tests { get; set; } = [];
    }
}
