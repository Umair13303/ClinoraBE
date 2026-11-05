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
        public const string Performed = "Performed";
        public const string Imaged = "Imaged";
        public const string AwaitingAdministration = "AwaitingAdministration";
        public const string Administered = "Administered";
        public const string AwaitingExamination = "AwaitingExamination";
        public const string Examined = "Examined";
        public const string Finalized = "Finalized";
        public const string Rejected = "Rejected";
        public const string Cancelled = "Cancelled";
    }

    public static class VisitStatuses
    {
        public const string Active = "Active";
        public const string Cancelled = "Cancelled";
    }

    public static class VisitPurposes
    {
        public const string Standard = "Standard";
        public const string MedicalCertificate = "MedicalCertificate";
    }

    public static class ServicePathways
    {
        public const string Consultation = "Consultation";
        public const string Laboratory = "Laboratory";
        public const string Radiology = "Radiology";
        public const string Vaccination = "Vaccination";
        public const string Physical = "Physical";
    }

    public static class ReportProfiles
    {
        public const string Standard = "Standard";
        public const string Narrative = "Narrative";
        public const string Semen = "Semen";
        public const string Ihc = "Ihc";
        public const string Radiology = "Radiology";
        public const string PhysicalExam = "PhysicalExam";

        public static string Normalize(string? value) =>
            string.IsNullOrWhiteSpace(value) ? Standard : value.Trim();
    }

    public static class CertificateFitness
    {
        public const string Pending = "Pending";
        public const string Fit = "Fit";
        public const string Unfit = "Unfit";
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
        public List<LookupItemDto> Antibodies { get; set; } = [];
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
        public string SampleStatus { get; set; } = "Collected";
        public DateTime CollectedAt { get; set; }
        public DateTime? ReceivedAt { get; set; }
        public string? ReceivedBy { get; set; }
        public List<string> Tests { get; set; } = [];
    }

    public class ReceiveSampleRequest
    {
        public string? ReceivedBy { get; set; }
    }

    public class ReceiveByBarcodeRequest
    {
        public string Barcode { get; set; } = string.Empty;
        public string? ReceivedBy { get; set; }
    }

    public class ReceivingSampleDto
    {
        public int SampleId { get; set; }
        public string Barcode { get; set; } = string.Empty;
        public string VisitNumber { get; set; } = string.Empty;
        public string PatientName { get; set; } = string.Empty;
        public string? SampleGroupName { get; set; }
        public string SampleTypeName { get; set; } = string.Empty;
        public string ContainerName { get; set; } = string.Empty;
        public string BottleColorName { get; set; } = string.Empty;
        public DateTime CollectedAt { get; set; }
        public string? PhlebotomistName { get; set; }
        public List<string> Tests { get; set; } = [];
    }

    public class ExistingResultDto
    {
        public string? ParameterName { get; set; }
        public string? ResultText { get; set; }
        public string? Units { get; set; }
        public string? ReferenceRange { get; set; }
        public bool IsAbnormal { get; set; }
        public bool IsCritical { get; set; }
    }

    public class ResultWorkItemDto
    {
        public int VisitServiceId { get; set; }
        public int VisitId { get; set; }
        public int ServiceId { get; set; }
        public string VisitNumber { get; set; } = string.Empty;
        public string PatientName { get; set; } = string.Empty;
        public string ServiceName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string ReportProfile { get; set; } = ReportProfiles.Standard;
        public bool Urgent { get; set; }
        public string? Barcode { get; set; }
        public bool IsSendOut { get; set; }
        public string? ExistingResultText { get; set; }
        public string? ExistingUnits { get; set; }
        public string? ExistingReferenceRange { get; set; }
        public string? ExistingParameterName { get; set; }
        public List<ExistingResultDto> ExistingResults { get; set; } = [];
        public bool IsAbnormal { get; set; }
        public bool IsCritical { get; set; }
    }

    public class SaveResultRequest
    {
        public int VisitServiceId { get; set; }
        public string ResultText { get; set; } = string.Empty;
        public string? Units { get; set; }
        public string? ReferenceRange { get; set; }
        public string? EnteredByName { get; set; }
        public string Source { get; set; } = "InHouse";
        public string? ParameterName { get; set; }
        public decimal? RefLow { get; set; }
        public decimal? RefHigh { get; set; }
        public bool IsCritical { get; set; }
        public bool Amend { get; set; }
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
        public string? ParameterName { get; set; }
        public string? ResultText { get; set; }
        public string? Units { get; set; }
        public string? ReferenceRange { get; set; }
        public string ReportProfile { get; set; } = ReportProfiles.Standard;
        public bool Urgent { get; set; }
        public bool IsAbnormal { get; set; }
        public bool IsCritical { get; set; }
    }

    public class RadiologyWorkItemDto
    {
        public int VisitServiceId { get; set; }
        public int VisitId { get; set; }
        public int ServiceId { get; set; }
        public string VisitNumber { get; set; } = string.Empty;
        public string PatientName { get; set; } = string.Empty;
        public string ServiceName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public bool Urgent { get; set; }
        public string? ReadyBy { get; set; }
        public string? ExistingFindings { get; set; }
        public string? TemplateName { get; set; }
    }

    public class RadiologyPerformRequest
    {
        public int VisitServiceId { get; set; }
        public string? PerformedByName { get; set; }
    }

    public class RadiologyCompleteRequest
    {
        public int VisitServiceId { get; set; }
        public string? Findings { get; set; }
        public string? PerformedByName { get; set; }
        public int? TemplateId { get; set; }
        public string? TemplateName { get; set; }
    }

    public class RadiologyTemplateDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Modality { get; set; } = "XRay";
        public string Profile { get; set; } = ReportProfiles.Radiology;
        public List<int> ServiceIds { get; set; } = [];
        public List<string> ServiceNames { get; set; } = [];
        public string? ServiceName { get; set; }
        public string Body { get; set; } = string.Empty;
        public bool Status { get; set; } = true;
    }

    public class UpsertRadiologyTemplateRequest
    {
        public string? Name { get; set; }
        public string? Modality { get; set; }
        public string? Profile { get; set; }
        public List<int>? ServiceIds { get; set; }
        public string? Body { get; set; }
        public bool Status { get; set; } = true;
    }

    public class CreateShipmentRequest
    {
        public int OutsideLabId { get; set; }
        public string? Awb { get; set; }
        public string? Notes { get; set; }
        public DateTime? ExpectedAt { get; set; }
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
        public DateTime? ExpectedAt { get; set; }
        public bool IsOverdue { get; set; }
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
        public string? ParameterName { get; set; }
    }

    public class VisitSlipsDto
    {
        public int VisitId { get; set; }
        public string VisitNumber { get; set; } = string.Empty;
        public int? TokenNo { get; set; }
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

    public class WorklistSummaryDto
    {
        public int AwaitingCollection { get; set; }
        public int Collected { get; set; }
        public int InLab { get; set; }
        public int SentOut { get; set; }
        public int AwaitingFinalize { get; set; }
        public int Radiology { get; set; }
        public int Vaccination { get; set; }
        public int OutsideOverdue { get; set; }
    }

    public class VaccinationWorkItemDto
    {
        public int VisitServiceId { get; set; }
        public int VisitId { get; set; }
        public int ServiceId { get; set; }
        public string VisitNumber { get; set; } = string.Empty;
        public string PatientName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public int Age { get; set; }
        public string Gender { get; set; } = string.Empty;
        public string ServiceName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public bool Urgent { get; set; }
        public DateTime CreatedAt { get; set; }
        public VaccinationRecordDto? Record { get; set; }
    }

    public class VaccinationRecordDto
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

    public class AdministerVaccinationRequest
    {
        public int VisitServiceId { get; set; }
        public string? BatchNo { get; set; }
        public string? LotNo { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string? DoseNumber { get; set; }
        public string? Site { get; set; }
        public string? Route { get; set; }
        public DateTime? AdministeredAt { get; set; }
        public string? AdministeredByName { get; set; }
        public DateTime? NextDueAt { get; set; }
        public string? Notes { get; set; }
    }

    public class VaccinationCardDto
    {
        public string VisitNumber { get; set; } = string.Empty;
        public string PatientName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public int Age { get; set; }
        public string Gender { get; set; } = string.Empty;
        public string VaccineName { get; set; } = string.Empty;
        public VaccinationRecordDto Record { get; set; } = new();
    }

    public class MedicalCertificateWorkItemDto
    {
        public int VisitId { get; set; }
        public string VisitNumber { get; set; } = string.Empty;
        public string PatientName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public int Age { get; set; }
        public string Gender { get; set; } = string.Empty;
        public string? PassportNo { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Fitness { get; set; } = CertificateFitness.Pending;
        public string? DestinationCountry { get; set; }
        public DateTime? IssuedAt { get; set; }
        public int TotalLines { get; set; }
        public int CompletedLines { get; set; }
        public bool ReadyToIssue { get; set; }
    }

    public class MedicalCertificateLineDto
    {
        public int VisitServiceId { get; set; }
        public int ServiceId { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public string Pathway { get; set; } = string.Empty;
        public string ReportProfile { get; set; } = ReportProfiles.Standard;
        public string Status { get; set; } = string.Empty;
        public bool Complete { get; set; }
        public string? ResultPreview { get; set; }
    }

    public class MedicalCertificateDetailDto
    {
        public int VisitId { get; set; }
        public string VisitNumber { get; set; } = string.Empty;
        public string PatientName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public int Age { get; set; }
        public string Gender { get; set; } = string.Empty;
        public string? PassportNo { get; set; }
        public string? Cnic { get; set; }
        public string? PhotoUrl { get; set; }
        public string Purpose { get; set; } = VisitPurposes.MedicalCertificate;
        public string Fitness { get; set; } = CertificateFitness.Pending;
        public string? DestinationCountry { get; set; }
        public string? Remarks { get; set; }
        public string? IssuedByName { get; set; }
        public DateTime? IssuedAt { get; set; }
        public bool ReadyToIssue { get; set; }
        public string? PhysicalExamJson { get; set; }
        public int? PhysicalVisitServiceId { get; set; }
        public List<MedicalCertificateLineDto> Lines { get; set; } = [];
    }

    public class SavePhysicalExamRequest
    {
        public int VisitId { get; set; }
        public string ExamJson { get; set; } = string.Empty;
        public string? ExaminedByName { get; set; }
    }

    public class IssueMedicalCertificateRequest
    {
        public int VisitId { get; set; }
        public string Fitness { get; set; } = CertificateFitness.Fit;
        public string? DestinationCountry { get; set; }
        public string? Remarks { get; set; }
        public string? IssuedByName { get; set; }
    }

    public class CertificatePackageDto
    {
        public string Code { get; set; } = "WAFID";
        public string Name { get; set; } = "Wafid / Gulf medical";
        public List<ServiceDto> Services { get; set; } = [];
    }

    public class RejectSampleRequest
    {
        public string Reason { get; set; } = string.Empty;
        public string? RejectedByName { get; set; }
        public bool Redraw { get; set; } = true;
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
