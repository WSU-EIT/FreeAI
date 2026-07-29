namespace FreeExamples;

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
