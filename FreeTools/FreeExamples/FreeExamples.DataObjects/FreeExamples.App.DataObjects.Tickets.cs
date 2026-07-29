namespace FreeExamples;

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
