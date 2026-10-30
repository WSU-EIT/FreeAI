namespace FreeExamples;

// ---- DataObjects.JsonStore ----

public partial class DataObjects
{
    /// <summary>
    /// Generic JSON envelope — wraps any entity type as a JSON blob with metadata.
    /// All entity types share one ConcurrentDictionary&lt;Guid, JsonRecord&gt;.
    /// Two-phase parse: check metadata first, then deserialize Contents.
    /// See doc 113 for full architecture.
    /// </summary>
    public class JsonRecord
    {
        // --- Payload (the actual entity, JSON-serialized) ---
        public string Contents { get; set; } = "";

        // --- Audit (tracked by the store, not by the entity) ---
        public DateTime Created { get; set; }
        public string? CreatedBy { get; set; }
        public bool Deleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        public string Format { get; set; } = "json";
        public DateTime Modified { get; set; }
        public string? ModifiedBy { get; set; }
        public Guid RecordId { get; set; }

        // --- Metadata (parsed first, before touching Contents) ---
        public string RecordType { get; set; } = "";
        public int SchemaVersion { get; set; } = 1;
        public Guid TenantId { get; set; }
    }

    /// <summary>
    /// Contract for all entities stored in the generic JSON store.
    /// Each entity class provides EntityType and CurrentSchemaVersion as static members.
    /// </summary>
    public interface IJsonEntity
    {
        static abstract int CurrentSchemaVersion { get; }
        static abstract string EntityType { get; }
        Guid RecordId { get; set; }
        Guid TenantId { get; set; }
    }

    /// <summary>
    /// Generic filter DTO for paginated/filtered entity queries.
    /// Entity-specific filters inherit from this and add their own filter properties.
    /// </summary>
    public class FilterJsonRecords<T> : Filter where T : class
    {
        public List<T>? Records { get; set; }
    }
}

// ---- DataObjects.JsonStore.BudgetRequests ----

public partial class DataObjects
{
    // ── Category 7: Budget & Approvals ──

    public class BudgetRequest : IJsonEntity
    {
        public string AccountCode { get; set; } = "";
        public decimal? ApprovedAmount { get; set; }
        public static int CurrentSchemaVersion => 1;
        public string? DenialReason { get; set; }
        public string Department { get; set; } = "";
        public static string EntityType => "BudgetRequest";
        public DateTime? FinanceDate { get; set; }
        public string? FinanceReviewerName { get; set; }
        public string FiscalYear { get; set; } = "";
        public string Justification { get; set; } = "";
        public List<BudgetLineItem> LineItems { get; set; } = [];
        public Guid? ProjectId { get; set; }
        public Guid RecordId { get; set; }
        public string RequestedBy { get; set; } = "";
        public DateTime RequestedDate { get; set; }
        public BudgetRequestStatus Status { get; set; }
        public DateTime? SupervisorDate { get; set; }
        public string? SupervisorName { get; set; }
        public Guid TenantId { get; set; }

        public string Title { get; set; } = "";
        public decimal TotalAmount { get; set; }
    }

    public enum BudgetRequestStatus { Draft, Submitted, SupervisorApproved, FinanceReview, Approved, Denied, Completed }

    public class BudgetLineItem
    {
        public BudgetLineCategory Category { get; set; }
        public string Description { get; set; } = "";
        public Guid LineItemId { get; set; }
        public string? Notes { get; set; }
        public int Quantity { get; set; } = 1;
        public decimal UnitPrice { get; set; }
        public string? Vendor { get; set; }
    }

    public enum BudgetLineCategory { Supplies, Equipment, Software, Travel, Services, Other }

    public class FilterBudgetRequests : FilterJsonRecords<BudgetRequest>
    {
        public string? Department { get; set; }
        public string? FiscalYear { get; set; }
        public string? Status { get; set; }
    }
}

// ---- DataObjects.JsonStore.Equipment ----

public partial class DataObjects
{
    // ── Category 8: Equipment Checkout ──

    public class Equipment : IJsonEntity
    {
        public string AssetTag { get; set; } = "";
        public EquipmentCategory Category { get; set; }
        public List<EquipmentCheckout> Checkouts { get; set; } = [];
        public EquipmentCondition Condition { get; set; }
        public static int CurrentSchemaVersion => 1;
        public string? Description { get; set; }
        public static string EntityType => "Equipment";
        public string Location { get; set; } = "";

        public string Name { get; set; } = "";
        public DateTime? PurchaseDate { get; set; }
        public decimal? PurchasePrice { get; set; }
        public Guid RecordId { get; set; }
        public string? SerialNumber { get; set; }
        public Guid TenantId { get; set; }
    }

    public enum EquipmentCategory { Laptop, Projector, Camera, Microphone, Tablet, Hotspot, Adapter, Other }
    public enum EquipmentCondition { New, Good, Fair, NeedsRepair, Retired }

    public class EquipmentCheckout
    {
        public string? BorrowerDepartment { get; set; }
        public string BorrowerEmail { get; set; } = "";
        public string BorrowerName { get; set; } = "";
        public DateTime CheckoutDate { get; set; }
        public Guid CheckoutId { get; set; }
        public EquipmentCondition ConditionAtCheckout { get; set; }
        public EquipmentCondition? ConditionAtReturn { get; set; }
        public DateTime DueDate { get; set; }
        public string? Notes { get; set; }
        public DateTime? ReturnDate { get; set; }
    }

    public class FilterEquipment : FilterJsonRecords<Equipment>
    {
        public string? Availability { get; set; }
        public string? Category { get; set; }
        public string? Condition { get; set; }
    }
}

// ---- DataObjects.JsonStore.Evaluations ----

public partial class DataObjects
{
    // ── Category 9: Course Evaluations ──

    public class Evaluation : IJsonEntity
    {
        public DateTime CloseDate { get; set; }
        public string CourseCode { get; set; } = "";
        public string CourseName { get; set; } = "";
        public static int CurrentSchemaVersion => 1;
        public string Department { get; set; } = "";
        public int EnrollmentCount { get; set; }
        public static string EntityType => "Evaluation";
        public string InstructorName { get; set; } = "";
        public bool IsAnonymous { get; set; } = true;
        public DateTime OpenDate { get; set; }
        public List<EvalQuestion> Questions { get; set; } = [];
        public Guid RecordId { get; set; }
        public List<EvalResponse> Responses { get; set; } = [];
        public Guid TemplateId { get; set; }
        public Guid TenantId { get; set; }
        public string Term { get; set; } = "";

        public string Title { get; set; } = "";
    }

    public class EvalQuestion
    {
        public int DisplayOrder { get; set; }
        public bool IsRequired { get; set; } = true;
        public string? Options { get; set; }
        public Guid QuestionId { get; set; }
        public string QuestionText { get; set; } = "";
        public EvalQuestionType QuestionType { get; set; }
    }

    public enum EvalQuestionType { Likert5, Likert7, MultipleChoice, YesNo, FreeText, Rating10 }

    public class EvalResponse
    {
        public List<EvalAnswer> Answers { get; set; } = [];
        public Guid ResponseId { get; set; }
        public DateTime SubmittedDate { get; set; }
    }

    public class EvalAnswer
    {
        public Guid QuestionId { get; set; }
        public string Value { get; set; } = "";
    }

    public class FilterEvaluations : FilterJsonRecords<Evaluation>
    {
        public string? Department { get; set; }
        public string? Status { get; set; }
        public string? Term { get; set; }
    }
}

// ---- DataObjects.JsonStore.Onboarding ----

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

// ---- DataObjects.JsonStore.Projects ----

public partial class DataObjects
{
    // ── Category 1: Projects & Hierarchy ──

    public class Project : IJsonEntity
    {
        public string? Color { get; set; }
        public static int CurrentSchemaVersion => 1;
        public string? Department { get; set; }
        public string? Description { get; set; }
        public static string EntityType => "Project";
        public string? LeadEmail { get; set; }
        public string LeadName { get; set; } = "";
        public string Name { get; set; } = "";
        public int NextTicketNumber { get; set; } = 1;

        public Guid? ParentProjectId { get; set; }
        public string ProjectKey { get; set; } = "";
        public Guid RecordId { get; set; }
        public int SortOrder { get; set; }
        public DateTime? StartDate { get; set; }
        public ProjectStatus Status { get; set; }
        public DateTime? TargetEndDate { get; set; }
        public Guid TenantId { get; set; }
    }

    public enum ProjectStatus { Planning, Active, OnHold, Completed, Archived }

    public class FilterProjects : FilterJsonRecords<Project>
    {
        public string? Department { get; set; }
        public Guid? ParentProjectId { get; set; }
        public string? Status { get; set; }
    }
}

// ---- DataObjects.JsonStore.Sprints ----

public partial class DataObjects
{
    // ── Category 3: Board Views (configuration entity) ──

    public class BoardConfig : IJsonEntity
    {
        public string BoardName { get; set; } = "";
        public BoardType BoardType { get; set; }
        public string? ColumnConfig { get; set; }
        public string CreatedBy { get; set; } = "";
        public static int CurrentSchemaVersion => 1;
        public static string EntityType => "BoardConfig";
        public string? FilterPreset { get; set; }

        public Guid ProjectId { get; set; }
        public Guid RecordId { get; set; }
        public string? SwimlaneField { get; set; }
        public Guid TenantId { get; set; }
        public string? WipLimits { get; set; }
    }

    public enum BoardType { Kanban, Sprint }

    // ── Category 4: Sprint Planning ──

    public class Sprint : IJsonEntity
    {
        public int? CapacityPoints { get; set; }
        public static int CurrentSchemaVersion => 1;
        public DateTime? EndDate { get; set; }
        public static string EntityType => "Sprint";
        public string? Goal { get; set; }
        public string Name { get; set; } = "";

        public Guid ProjectId { get; set; }
        public Guid RecordId { get; set; }
        public DateTime? StartDate { get; set; }
        public SprintStatus Status { get; set; }
        public Guid TenantId { get; set; }
    }

    public enum SprintStatus { Planning, Active, Completed, Cancelled }

    public class FilterSprints : FilterJsonRecords<Sprint>
    {
        public Guid? ProjectId { get; set; }
        public string? Status { get; set; }
    }

    // ── Category 5: Backlog Saved Views ──

    public class SavedView : IJsonEntity
    {
        public string CreatedBy { get; set; } = "";
        public static int CurrentSchemaVersion => 1;
        public static string EntityType => "SavedView";
        public string? FilterJson { get; set; }
        public string? GroupByField { get; set; }

        public string Name { get; set; } = "";
        public Guid? ProjectId { get; set; }
        public Guid RecordId { get; set; }
        public string? SortJson { get; set; }
        public Guid TenantId { get; set; }
    }
}

// ---- DataObjects.JsonStore.Tickets ----

public partial class DataObjects
{
    // ── Category 2: Tickets ──

    public class Ticket : IJsonEntity
    {
        public string? AssignedTo { get; set; }
        public List<TicketComment> Comments { get; set; } = [];
        public DateTime? CompletedDate { get; set; }
        public static int CurrentSchemaVersion => 1;
        public string? Description { get; set; }
        public DateTime? DueDate { get; set; }
        public static string EntityType => "Ticket";
        public string? Labels { get; set; }
        public Guid? ParentTicketId { get; set; }
        public TicketPriority Priority { get; set; }

        public Guid ProjectId { get; set; }
        public Guid RecordId { get; set; }
        public string ReporterName { get; set; } = "";
        public int SortOrder { get; set; }
        public Guid? SprintId { get; set; }
        public DateTime? StartedDate { get; set; }
        public TicketStatus Status { get; set; }
        public int? StoryPoints { get; set; }
        public Guid TenantId { get; set; }
        public string TicketNumber { get; set; } = "";
        public string Title { get; set; } = "";
        public TicketType Type { get; set; }
    }

    public enum TicketType { Epic, Story, Task, Bug, Improvement, SubTask }
    public enum TicketStatus { Backlog, ToDo, InProgress, InReview, Testing, Done, Closed, Wontfix }
    public enum TicketPriority { Critical, High, Medium, Low, Trivial }

    public class TicketComment
    {
        public string AuthorName { get; set; } = "";
        public string Body { get; set; } = "";
        public Guid CommentId { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? EditedDate { get; set; }
        public bool IsInternal { get; set; }
    }

    public class FilterTickets : FilterJsonRecords<Ticket>
    {
        public string? AssignedTo { get; set; }
        public string? Priority { get; set; }
        public Guid? ProjectId { get; set; }
        public Guid? SprintId { get; set; }
        public string? Status { get; set; }
        public string? Type { get; set; }
    }
}

// ---- DataObjects.JsonStore.WorkOrders ----

public partial class DataObjects
{
    // ── Category 6: Work Orders ──

    public class WorkOrder : IJsonEntity
    {
        public decimal? ActualHours { get; set; }
        public string? AssignedTeam { get; set; }
        public string? AssignedTo { get; set; }
        public string Building { get; set; } = "";
        public WorkOrderCategory Category { get; set; }
        public DateTime? CompletedDate { get; set; }
        public string? CompletionNotes { get; set; }
        public static int CurrentSchemaVersion => 1;
        public string Description { get; set; } = "";
        public static string EntityType => "WorkOrder";
        public decimal? EstimatedHours { get; set; }
        public string Floor { get; set; } = "";
        public Guid RecordId { get; set; }
        public string RequestedBy { get; set; } = "";
        public string RequestedByEmail { get; set; } = "";
        public DateTime RequestedDate { get; set; }
        public string RoomNumber { get; set; } = "";
        public WorkOrderStatus Status { get; set; }
        public Guid TenantId { get; set; }

        public string Title { get; set; } = "";
        public WorkOrderUrgency Urgency { get; set; }
    }

    public enum WorkOrderCategory { Plumbing, Electrical, HVAC, Custodial, Grounds, Locksmith, Other }
    public enum WorkOrderUrgency { Low, Normal, High, Emergency }
    public enum WorkOrderStatus { Submitted, Assigned, InProgress, OnHold, Completed, Closed }

    public class FilterWorkOrders : FilterJsonRecords<WorkOrder>
    {
        public string? Building { get; set; }
        public string? Category { get; set; }
        public string? Status { get; set; }
        public string? Urgency { get; set; }
    }
}

// ---- DataObjects.SampleData ----

public partial class DataObjects
{
    /// <summary>
    /// A sample item used across all FreeExamples demo pages.
    /// Demonstrates common field types: string, bool, int, decimal, DateTime, Guid, enum.
    /// </summary>
    public class SampleItem
    {
        public DateTime Added { get; set; }
        public string? AddedBy { get; set; }
        public decimal Amount { get; set; }
        public string? Category { get; set; }
        public bool Deleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        public string? Description { get; set; }
        public DateTime? DueDate { get; set; }
        public bool Enabled { get; set; }
        public DateTime LastModified { get; set; }
        public string? LastModifiedBy { get; set; }
        public string Name { get; set; } = "";
        public int Priority { get; set; }
        public Guid SampleItemId { get; set; }
        public SampleItemStatus Status { get; set; }
        public Guid TenantId { get; set; }
    }

    public enum SampleItemStatus
    {
        Draft = 0,
        Active = 1,
        Completed = 2,
        Archived = 3,
    }

    /// <summary>
    /// Filter DTO extending the base Filter class for SampleItems list page.
    /// </summary>
    public partial class FilterSampleItems : Filter
    {
        public List<string> AvailableCategories { get; set; } = [];
        public string? Category { get; set; }
        public string? Enabled { get; set; }
        public List<SampleItem>? Records { get; set; }
        public string? Status { get; set; }
    }

    /// <summary>
    /// Response wrapper for the three-endpoint CRUD pattern.
    /// </summary>
    public class SampleDataResponse : ActionResponseObject
    {
        public List<SampleItem> Items { get; set; } = [];
    }

    /// <summary>
    /// Response for server-side file generation endpoints.
    /// </summary>
    public class SampleFileResponse
    {
        public byte[] FileData { get; set; } = [];
        public string FileName { get; set; } = "";
    }

    /// <summary>
    /// Network graph node for the vis.js demo page.
    /// </summary>
    public class SampleGraphNode
    {
        public string? Group { get; set; }
        public int Id { get; set; }
        public string Label { get; set; } = "";
    }

    /// <summary>
    /// Network graph edge for the vis.js demo page.
    /// </summary>
    public class SampleGraphEdge
    {
        public int From { get; set; }
        public string? Label { get; set; }
        public int To { get; set; }
    }

    /// <summary>
    /// Full graph data for the network visualization demo.
    /// </summary>
    public class SampleGraphData
    {
        public List<SampleGraphEdge> Edges { get; set; } = [];
        public List<SampleGraphNode> Nodes { get; set; } = [];
    }

    /// <summary>
    /// Dashboard summary data returned from a single endpoint.
    /// Reused by the dashboard, charts, and reporting demo pages.
    /// </summary>
    public class SampleDashboard
    {
        public int ActiveItems { get; set; }
        public int ArchivedItems { get; set; }
        public List<SampleCategorySummary> ByCategory { get; set; } = [];
        public List<SampleTimelineSummary> ByMonth { get; set; } = [];
        public int CompletedItems { get; set; }
        public int DraftItems { get; set; }
        public int TotalItems { get; set; }
    }

    public class SampleCategorySummary
    {
        public string Category { get; set; } = "";
        public int Count { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class SampleTimelineSummary
    {
        public int Added { get; set; }
        public int Completed { get; set; }
        public string Month { get; set; } = "";
    }

    public partial class SignalRUpdateType
    {
        public const string SampleItemSaved = "SampleItemSaved";
        public const string SampleItemDeleted = "SampleItemDeleted";
        public const string CommentSaved = "CommentSaved";
        public const string CommentDeleted = "CommentDeleted";
    }
}

// ---- DataObjects.ApiKeyDemo ----

public partial class DataObjects
{
    /// <summary>
    /// Represents a registered API key with metadata.
    /// </summary>
    public class ApiKeyInfo
    {
        public Guid ApiKeyId { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; } = true;
        public string KeyHash { get; set; } = "";
        public string KeyPrefix { get; set; } = "";
        public string Name { get; set; } = "";
        public int RequestCount { get; set; }
    }

    /// <summary>
    /// Request DTO for testing the protected endpoint.
    /// </summary>
    public class ApiKeyTestRequest
    {
        public string? Message { get; set; }
    }

    /// <summary>
    /// Response from the protected endpoint.
    /// </summary>
    public class ApiKeyTestResponse
    {
        public string? AuthenticatedAs { get; set; }
        public string? Message { get; set; }
        public bool Success { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// A log entry from an API request hitting the middleware.
    /// </summary>
    public class ApiKeyRequestLog
    {
        public string? Detail { get; set; }
        public string? KeyName { get; set; }
        public string Method { get; set; } = "";
        public string Path { get; set; } = "";
        public int StatusCode { get; set; }
        public DateTime Timestamp { get; set; }
    }
}

// ---- DataObjects.CodePlayground ----

public partial class DataObjects
{
    /// <summary>
    /// A saved code snippet for the Code Playground demo.
    /// Stored in-memory via CodeSnippetService.
    /// </summary>
    public class CodeSnippet
    {
        public string Content { get; set; } = "";
        public string Language { get; set; } = "plaintext";
        public DateTime LastSaved { get; set; }
        public Guid SnippetId { get; set; }
        public string Title { get; set; } = "";
    }

    /// <summary>
    /// Request DTO for the API Notebook "execute" feature.
    /// Sends editor content to a selected endpoint and returns the response.
    /// </summary>
    public class CodePlaygroundRequest
    {
        public string Body { get; set; } = "";
        public string Endpoint { get; set; } = "";
    }

    /// <summary>
    /// Response DTO wrapping whatever the target endpoint returns.
    /// </summary>
    public class CodePlaygroundResponse
    {
        public long DurationMs { get; set; }
        public string ResponseBody { get; set; } = "";
        public int StatusCode { get; set; }
        public bool Success { get; set; }
    }
}

// ---- DataObjects.CommentThread ----

public partial class DataObjects
{
    /// <summary>
    /// A comment in a thread attached to a SampleItem.
    /// In-memory via CommentService (same pattern as ApiKeyDemoService).
    /// </summary>
    public class SampleComment
    {
        public string AuthorInitials { get; set; } = "";
        public string AuthorName { get; set; } = "";
        public Guid CommentId { get; set; }
        public DateTime Created { get; set; }
        public bool Deleted { get; set; }
        public DateTime? Edited { get; set; }
        public Guid SampleItemId { get; set; }
        public string Text { get; set; } = "";
    }
}

// ---- DataObjects.GitBrowser ----

public partial class DataObjects
{
    /// <summary>
    /// Represents a single entry (file or folder) in a git repository tree.
    /// </summary>
    public class GitRepoEntry
    {
        public string Extension { get; set; } = "";
        public bool IsDirectory { get; set; }
        public string Name { get; set; } = "";
        public string Path { get; set; } = "";
        public long Size { get; set; }
    }

    /// <summary>
    /// Represents the contents of a file in a git repository.
    /// </summary>
    public class GitFileContent
    {
        public string Content { get; set; } = "";
        public string Extension { get; set; } = "";
        public bool IsBinary { get; set; }
        public string Name { get; set; } = "";
        public string Path { get; set; } = "";
        public long Size { get; set; }
    }

    /// <summary>
    /// Request DTO for browsing a git repository.
    /// </summary>
    public class GitBrowseRequest
    {
        public string? Path { get; set; }
        public string? RepoUrl { get; set; }
    }

    /// <summary>
    /// Request DTO for reading a file from a git repository.
    /// </summary>
    public class GitFileRequest
    {
        public string? FilePath { get; set; }
        public string? RepoUrl { get; set; }
    }

    public partial class SignalRUpdateType
    {
        public const string GitCloneProgress = "GitCloneProgress";
    }
}
