namespace FreeExamples;

public partial class DataObjects
{
    // ── Category 10: Employee Onboarding ──

    public class Onboarding : IJsonEntity
    {
        public List<ChecklistItem> ChecklistItems { get; set; } = [];
        public static int CurrentSchemaVersion => 1;
        public string Department { get; set; } = "";
        public string EmployeeEmail { get; set; } = "";

        public string EmployeeName { get; set; } = "";
        public string EmployeeTitle { get; set; } = "";
        public EmploymentType EmploymentType { get; set; }
        public static string EntityType => "Onboarding";
        public DateTime HireDate { get; set; }
        public string? MentorName { get; set; }
        public string? Notes { get; set; }
        public Guid RecordId { get; set; }
        public DateTime StartDate { get; set; }
        public OnboardingStatus Status { get; set; }
        public string SupervisorName { get; set; } = "";
        public Guid TenantId { get; set; }
    }

    public enum EmploymentType { FullTime, PartTime, Temporary, GradAssistant, StudentWorker }
    public enum OnboardingStatus { Pending, InProgress, Completed, Withdrawn }

    public class ChecklistItem
    {
        public string AssignedTo { get; set; } = "";
        public ChecklistCategory Category { get; set; }
        public Guid ChecklistItemId { get; set; }
        public string? CompletedBy { get; set; }
        public DateTime? CompletedDate { get; set; }
        public string? Description { get; set; }
        public int DisplayOrder { get; set; }
        public DateTime? DueDate { get; set; }
        public bool IsCompleted { get; set; }
        public bool IsRequired { get; set; } = true;
        public string? Notes { get; set; }
        public string TaskName { get; set; } = "";
    }

    public enum ChecklistCategory { HR, IT, Facilities, Department, Training, Compliance }

    public class FilterOnboarding : FilterJsonRecords<Onboarding>
    {
        public string? Department { get; set; }
        public string? EmploymentType { get; set; }
        public string? Status { get; set; }
    }
}
