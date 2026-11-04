namespace ClinoraBE.Model.ViewModel
{
    public class ServiceDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public decimal Fee { get; set; }
        public int NormalMinutes { get; set; }
        public int UrgentMinutes { get; set; }
        public string Pathway { get; set; } = "Laboratory";
        public int? SampleTypeId { get; set; }
        public int? ContainerId { get; set; }
        public int? BottleColorId { get; set; }
        public int? SampleGroupId { get; set; }
        public string? SampleGroupCode { get; set; }
        public bool CanSendOut { get; set; }
        public int? DefaultOutsideLabId { get; set; }
    }

    public class StaffDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Department { get; set; }
    }

    public class PatientDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public int Age { get; set; }
        public string Gender { get; set; } = string.Empty;
        public string? Cnic { get; set; }
        public string? PassportNo { get; set; }
        public string? PhotoUrl { get; set; }
        public bool IsPanel { get; set; }
        public string? PanelName { get; set; }
        public int VisitCount { get; set; }
    }

    public class VisitSummaryDto
    {
        public int Id { get; set; }
        public string VisitNumber { get; set; } = string.Empty;
        public int PatientId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string? ReferredBy { get; set; }
        public string? PanelName { get; set; }
        public decimal Net { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class VisitServiceLineRequest
    {
        public int ServiceId { get; set; }
        public decimal Discount { get; set; }
        public bool Urgent { get; set; }
        public int? StaffId { get; set; }
        public string? StaffName { get; set; }
        public string? ReadyBy { get; set; }
    }

    public class CreateVisitRequest
    {
        public int? PatientId { get; set; }
        public string? FullName { get; set; }
        public string? Phone { get; set; }
        public int? Age { get; set; }
        public string? Gender { get; set; }
        public string? Cnic { get; set; }
        public string? PassportNo { get; set; }
        public string? PhotoBase64 { get; set; }
        public bool IsPanel { get; set; }
        public string? PanelName { get; set; }
        public decimal PercentOff { get; set; }
        public decimal VisitOff { get; set; }
        public decimal AmountReceived { get; set; }
        public bool PaymentReceived { get; set; }
        public bool IsReferred { get; set; }
        public string? ReferredBy { get; set; }
        public string? SlipNo { get; set; }
        public string? Notes { get; set; }
        public List<VisitServiceLineRequest> Services { get; set; } = [];
    }

    public class VisitReceiptDto
    {
        public int Id { get; set; }
        public string VisitNumber { get; set; } = string.Empty;
        public string PatientName { get; set; } = string.Empty;
        public decimal Net { get; set; }
        public decimal AmountReceived { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
