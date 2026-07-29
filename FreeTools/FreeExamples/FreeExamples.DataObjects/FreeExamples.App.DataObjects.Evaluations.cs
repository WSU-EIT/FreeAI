namespace FreeExamples;

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
