/*
    Role: DynamicForms feature orchestration service.
    Purpose: Owns in-memory form definitions, form submissions, validation, and conditional-visibility evaluation.
    Contains: CRUD operations for definitions and submissions plus the rule parsing used by the builder and runtime renderer.
    Host expectations: Register in DI as the feature service backing DynamicForms authoring and runtime workflows.
*/
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace FreeBlazorExtended.DynamicForms;

public partial class FormService
{
    private static readonly ConcurrentDictionary<Guid, FormDefinition> _formDefinitions = new();
    private static readonly ConcurrentDictionary<Guid, FormSubmission> _formSubmissions = new();

    public Task<List<FormDefinition>> GetFormDefinitions(Guid TenantId)
    {
        var forms = _formDefinitions.Values
            .Where(f => f.TenantId == TenantId && !f.Deleted)
            .ToList();
        return Task.FromResult(forms);
    }

    public Task<FormDefinition?> GetFormDefinition(Guid FormId)
    {
        _formDefinitions.TryGetValue(FormId, out var form);
        return Task.FromResult(form?.Deleted == false ? form : null);
    }

    public Task<FormDefinition?> GetPublishedForm(Guid FormId)
    {
        _formDefinitions.TryGetValue(FormId, out var form);
        if (form?.IsPublished == true && form.Deleted == false)
            return Task.FromResult(form);
        return Task.FromResult((FormDefinition?)null);
    }

    public Task<FormDefinition> SaveFormDefinition(FormDefinition form, string? UserId = null)
    {
        if (form.FormDefinitionId == Guid.Empty)
            form.FormDefinitionId = Guid.NewGuid();

        form.LastModified = DateTime.UtcNow;
        form.LastModifiedBy = UserId;

        if (!_formDefinitions.ContainsKey(form.FormDefinitionId)) {
            form.Added = DateTime.UtcNow;
            form.AddedBy = UserId;
        }

        // Re-order fields
        for (int i = 0; i < form.Fields.Count; i++)
            form.Fields[i].Order = i;

        _formDefinitions[form.FormDefinitionId] = form;
        return Task.FromResult(form);
    }

    public Task<DataObjects.BooleanResponse> DeleteFormDefinition(Guid FormId, DataObjects.User? CurrentUser = null)
    {
        var output = new DataObjects.BooleanResponse();

        if (_formDefinitions.TryGetValue(FormId, out var form)) {
            form.Deleted = true;
            form.LastModified = DateTime.UtcNow;
            output.Result = true;
        } else {
            output.Messages.Add("Form definition not found.");
        }

        return Task.FromResult(output);
    }

    public Task<List<FormSubmission>> GetFormSubmissions(Guid FormId, Guid TenantId)
    {
        var submissions = _formSubmissions.Values
            .Where(s => s.FormDefinitionId == FormId && s.TenantId == TenantId && !s.Deleted)
            .OrderByDescending(s => s.SubmittedAt)
            .ToList();
        return Task.FromResult(submissions);
    }

    public Task<FormSubmission?> GetFormSubmission(Guid SubmissionId)
    {
        _formSubmissions.TryGetValue(SubmissionId, out var submission);
        return Task.FromResult(submission?.Deleted == false ? submission : null);
    }

    public async Task<(bool success, string? error)> SaveFormSubmission(
        FormSubmission submission,
        FormDefinition formDef,
        Dictionary<Guid, string?> fieldValues,
        string? UserId = null)
    {
        // Validate required fields and rules
        var validationErrors = await ValidateSubmission(formDef, fieldValues);
        if (validationErrors.Any())
            return (false, string.Join("; ", validationErrors));

        if (submission.FormSubmissionId == Guid.Empty)
            submission.FormSubmissionId = Guid.NewGuid();

        submission.Added = DateTime.UtcNow;
        submission.AddedBy = UserId;
        submission.LastModified = DateTime.UtcNow;
        submission.LastModifiedBy = UserId;
        submission.FormVersion = formDef.Version;
        submission.SubmittedAt = DateTime.UtcNow;
        submission.SubmittedBy = UserId;
        submission.DataJson = JsonSerializer.Serialize(fieldValues);

        _formSubmissions[submission.FormSubmissionId] = submission;
        return (true, null);
    }

    public Task<List<string>> ValidateSubmission(
        FormDefinition formDef,
        Dictionary<Guid, string?> fieldValues)
    {
        var errors = new List<string>();

        foreach (var field in formDef.Fields) {
            var value = fieldValues.ContainsKey(field.FieldId) ? fieldValues[field.FieldId] : null;

            // Check required
            if (field.IsRequired && string.IsNullOrWhiteSpace(value)) {
                errors.Add($"{field.Label} is required.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(value))
                continue;

            // Parse validation rules
            if (!string.IsNullOrEmpty(field.ValidationRulesJson)) {
                try {
                    var rules = JsonSerializer.Deserialize<ValidationRules>(field.ValidationRulesJson);
                    if (rules != null) {
                        // Min/max length
                        if (rules.MinLength.HasValue && value.Length < rules.MinLength.Value)
                            errors.Add($"{field.Label} must be at least {rules.MinLength} characters.");
                        if (rules.MaxLength.HasValue && value.Length > rules.MaxLength.Value)
                            errors.Add($"{field.Label} must be no more than {rules.MaxLength} characters.");

                        // Regex pattern
                        if (!string.IsNullOrEmpty(rules.Pattern)) {
                            if (!Regex.IsMatch(value, rules.Pattern))
                                errors.Add(rules.CustomMessage ?? $"{field.Label} format is invalid.");
                        }
                    }
                }
                catch (Exception) {

                    // JSON parse failed; treat as malformed data and fall through to default.

                }
            }
        }

        return Task.FromResult(errors);
    }

    public Task<bool> IsFieldVisible(
        FormField field,
        Dictionary<Guid, string?> currentValues)
    {
        if (string.IsNullOrEmpty(field.ConditionalVisibilityJson))
            return Task.FromResult(true);

        try {
            var visibility = JsonSerializer.Deserialize<ConditionalVisibility>(field.ConditionalVisibilityJson);
            if (visibility?.ShowWhen == null || visibility.ShowWhen.Count == 0)
                return Task.FromResult(true);

            return Task.FromResult(EvaluateVisibilityConditions(visibility.ShowWhen, currentValues));
        }
        catch {
            return Task.FromResult(true);
        }
    }

    private bool EvaluateVisibilityConditions(
        List<VisibilityCondition> conditions,
        Dictionary<Guid, string?> currentValues)
    {
        // All conditions must be true (AND logic)
        foreach (var condition in conditions) {
            var fieldValue = currentValues.ContainsKey(condition.FieldId)
                ? currentValues[condition.FieldId]
                : null;

            if (!EvaluateCondition(fieldValue, condition.Operator, condition.Value))
                return false;
        }
        return true;
    }

    private bool EvaluateCondition(string? fieldValue, string op, string conditionValue)
    {
        fieldValue = fieldValue ?? string.Empty;

        return op switch
        {
            "eq" => fieldValue.Equals(conditionValue, StringComparison.OrdinalIgnoreCase),
            "neq" => !fieldValue.Equals(conditionValue, StringComparison.OrdinalIgnoreCase),
            "contains" => fieldValue.Contains(conditionValue, StringComparison.OrdinalIgnoreCase),
            "gt" => decimal.TryParse(fieldValue, out var val1) && decimal.TryParse(conditionValue, out var val2) && val1 > val2,
            "lt" => decimal.TryParse(fieldValue, out var val3) && decimal.TryParse(conditionValue, out var val4) && val3 < val4,
            _ => true
        };
    }

    public Task<DataObjects.BooleanResponse> DeleteFormSubmission(Guid SubmissionId, DataObjects.User? CurrentUser = null)
    {
        var output = new DataObjects.BooleanResponse();

        if (_formSubmissions.TryGetValue(SubmissionId, out var submission)) {
            submission.Deleted = true;
            submission.LastModified = DateTime.UtcNow;
            output.Result = true;
        } else {
            output.Messages.Add("Form submission not found.");
        }

        return Task.FromResult(output);
    }
}

public class ValidationRules
{
    public int? MinLength { get; set; }
    public int? MaxLength { get; set; }
    public string? Pattern { get; set; }
    public string? CustomMessage { get; set; }
}

public class ConditionalVisibility
{
    public List<VisibilityCondition> ShowWhen { get; set; } = new();
}

public class VisibilityCondition
{
    public Guid FieldId { get; set; }
    public string Operator { get; set; } = "eq"; // eq, neq, contains, gt, lt
    public string Value { get; set; } = string.Empty;
}
