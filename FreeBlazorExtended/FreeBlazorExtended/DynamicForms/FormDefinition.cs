/*
    Purpose: DynamicForms model bundle for schema definition, field metadata, and submissions.
    Contains: Field-type enums plus the definition and submission shapes shared by the builder and runtime renderer.
    Used by: FormService and the DynamicForms component surfaces.
*/
namespace FreeBlazorExtended.DynamicForms;

public enum FormFieldType
{
    Text,
    Textarea,
    Select,
    Multiselect,
    Radio,
    Checkbox,
    Date,
    DateTime,
    Number,
    File,
    Html,
    Signature,
    Confirmation,
    Email,
    Telephone,
    Url
}

public class FormField
{
    /// <summary>Unique identifier for this field within its form definition. Generated on creation.</summary>
    public Guid FieldId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Zero-based display position within the form. The renderer sorts fields by
    /// ascending <c>Order</c> before rendering; drag-and-drop in the builder
    /// resets these values.
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// Visible label rendered above or beside the input. Should be concise and
    /// meaningful — it is also used as the column header in submission exports.
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Optional instructional text displayed below the input to guide the user.
    /// Use this to explain expected format, allowed values, or why the field exists.
    /// </summary>
    public string? HelpText { get; set; }

    /// <summary>
    /// Determines which input control is rendered. Drives the entire field template —
    /// changing this value after data has been collected may make stored answers unreadable.
    /// </summary>
    public FormFieldType FieldType { get; set; }

    /// <summary>
    /// When <c>true</c>, the form renderer blocks submission and highlights this field
    /// if the user leaves it blank or selects no option.
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>
    /// Pre-populated value placed in the input when a new (unsaved) form is rendered.
    /// Useful for seeding sensible defaults so users only need to change exceptional cases.
    /// </summary>
    public string? DefaultValue { get; set; }

    /// <summary>
    /// Ghost text shown inside an empty input to hint at expected format or an example
    /// value (e.g. <c>"e.g. John Smith"</c>). Cleared on first keystroke by the browser.
    /// </summary>
    public string? PlaceholderText { get; set; }

    /// <summary>
    /// Key/label pairs used by Select, Multiselect, Radio, and Checkbox fields.
    /// <c>key</c> is stored in the submission; <c>label</c> is shown to the user.
    /// Order of items in this list controls render order in the UI.
    /// </summary>
    public List<(string key, string label)> Options { get; set; } = new();

    /// <summary>
    /// JSON-serialized validation rules applied to this field beyond simple required-ness
    /// (e.g. min/max length, regex pattern, numeric range). Parsed by <c>FormService</c>
    /// during submission validation.
    /// </summary>
    public string? ValidationRulesJson { get; set; }

    /// <summary>
    /// JSON-serialized rule that controls whether this field is shown or hidden based on
    /// the current value of another field. Evaluated in real time by <c>DynamicFormRenderer</c>
    /// on every user input change. <c>null</c> means the field is always visible.
    /// Example: <c>{"field":"role","operator":"equals","value":"admin"}</c>
    /// </summary>
    public string? ConditionalVisibilityJson { get; set; }

    /// <summary>
    /// Optional extra Bootstrap or custom CSS class names appended to the field wrapper div.
    /// Use to control width (<c>"col-md-6"</c>), spacing, or custom styling per field.
    /// </summary>
    public string? CssClass { get; set; }
}

public class FormDefinition
{
    /// <summary>Unique identifier for this form schema. Generated on creation.</summary>
    public Guid FormDefinitionId { get; set; } = Guid.NewGuid();

    /// <summary>Tenant scope; limits form visibility and submissions to the owning tenant.</summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Internal name used in the builder list and admin views.
    /// Should be descriptive enough to distinguish this form from others (e.g. "IT Access Request v2").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional longer explanation of the form's purpose, shown at the top of the rendered form
    /// and in the builder detail panel.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Monotonically incremented each time the schema is updated. Stored on each
    /// <see cref="FormSubmission"/> so historical submissions can be re-rendered
    /// against the schema version that was active when they were collected.
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    /// Controls whether this form is available to end users. When <c>false</c>, the form
    /// is in draft mode — only visible in the builder and excluded from consumer-facing
    /// form lists. Set to <c>true</c> to make the form live.
    /// </summary>
    public bool IsPublished { get; set; }

    /// <summary>
    /// Label on the primary submit button at the bottom of the rendered form.
    /// Defaults to <c>"Submit"</c>; override for domain-specific language (e.g. "Request Access").
    /// </summary>
    public string SubmitButtonText { get; set; } = "Submit";

    /// <summary>
    /// Optional confirmation message shown to the user after a successful submission.
    /// When <c>null</c>, the renderer falls back to a generic "Thank you" message.
    /// </summary>
    public string? SuccessMessage { get; set; }

    /// <summary>
    /// Ordered list of fields that make up this form. The renderer sorts by
    /// <see cref="FormField.Order"/> before display; the builder manages insertion and reordering.
    /// </summary>
    public List<FormField> Fields { get; set; } = new();

    /// <summary>UTC timestamp of when this form schema was first created.</summary>
    public DateTime Added { get; set; } = DateTime.UtcNow;
    /// <summary>Display name of the user who created this form.</summary>
    public string? AddedBy { get; set; }
    /// <summary>UTC timestamp of the most recent schema update.</summary>
    public DateTime LastModified { get; set; } = DateTime.UtcNow;
    /// <summary>Display name of the user who last modified this form schema.</summary>
    public string? LastModifiedBy { get; set; }
    /// <summary>Soft-delete flag; excluded from builder lists and consumer views when <c>true</c>.</summary>
    public bool Deleted { get; set; }
}

public class FormSubmission
{
    /// <summary>Unique identifier for this individual submission. Generated on creation.</summary>
    public Guid FormSubmissionId { get; set; } = Guid.NewGuid();

    /// <summary>Tenant scope; keeps submissions isolated between tenants.</summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// The <see cref="FormDefinition.FormDefinitionId"/> this submission was collected against.
    /// Used to look up the schema when re-rendering or exporting a historical response.
    /// </summary>
    public Guid FormDefinitionId { get; set; }

    /// <summary>
    /// Snapshot of <see cref="FormDefinition.Version"/> at the moment of submission.
    /// Stored so the form can be re-rendered with the exact field set the user saw,
    /// even if the schema has since been updated.
    /// </summary>
    public int FormVersion { get; set; }

    /// <summary>
    /// Identity of the submitter — typically their email or display name from the host auth layer.
    /// <c>null</c> for anonymous submissions.
    /// </summary>
    public string? SubmittedBy { get; set; }

    /// <summary>UTC timestamp of when the user clicked the submit button.</summary>
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// JSON object mapping <see cref="FormField.FieldId"/> (string) to the user's answer.
    /// Keyed by field ID so answers remain linked to their field even if label text changes
    /// in a later schema version.
    /// </summary>
    public string DataJson { get; set; } = "{}";

    /// <summary>UTC timestamp of when this submission record was created.</summary>
    public DateTime Added { get; set; } = DateTime.UtcNow;
    /// <summary>Display name of the user who created this record (usually same as <see cref="SubmittedBy"/>).</summary>
    public string? AddedBy { get; set; }
    /// <summary>UTC timestamp of the most recent update (e.g. admin correction).</summary>
    public DateTime LastModified { get; set; } = DateTime.UtcNow;
    /// <summary>Display name of the user who last modified this submission record.</summary>
    public string? LastModifiedBy { get; set; }
    /// <summary>Soft-delete flag; excluded from all submission queries and exports when <c>true</c>.</summary>
    public bool Deleted { get; set; }
}
