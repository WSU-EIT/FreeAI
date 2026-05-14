namespace FreeA11yChecker;

public partial interface IDataAccess
{
    List<DataObjects.WcagCriterionDefinition> GetWcagCriteria();
    Task<DataObjects.AuditReport> GenerateAuditReport(Guid SiteId, DataObjects.User? CurrentUser = null);
    Task<string> ExportAuditCsv(Guid SiteId);
}

public partial class DataAccess
{
    public List<DataObjects.WcagCriterionDefinition> GetWcagCriteria()
    {
        return new List<DataObjects.WcagCriterionDefinition> {
            // Perceivable
            new() { Criterion = "1.1.1", Name = "Non-text Content", Principle = "Perceivable", Level = "A", CanBeAutomated = true },
            new() { Criterion = "1.2.1", Name = "Audio-only and Video-only", Principle = "Perceivable", Level = "A", CanBeAutomated = false },
            new() { Criterion = "1.2.2", Name = "Captions (Prerecorded)", Principle = "Perceivable", Level = "A", CanBeAutomated = false },
            new() { Criterion = "1.2.3", Name = "Audio Description or Media Alternative", Principle = "Perceivable", Level = "A", CanBeAutomated = false },
            new() { Criterion = "1.2.5", Name = "Audio Description (Prerecorded)", Principle = "Perceivable", Level = "AA", CanBeAutomated = false },
            new() { Criterion = "1.3.1", Name = "Info and Relationships", Principle = "Perceivable", Level = "A", CanBeAutomated = true },
            new() { Criterion = "1.3.2", Name = "Meaningful Sequence", Principle = "Perceivable", Level = "A", CanBeAutomated = false },
            new() { Criterion = "1.3.3", Name = "Sensory Characteristics", Principle = "Perceivable", Level = "A", CanBeAutomated = false },
            new() { Criterion = "1.3.4", Name = "Orientation", Principle = "Perceivable", Level = "AA", CanBeAutomated = false },
            new() { Criterion = "1.3.5", Name = "Identify Input Purpose", Principle = "Perceivable", Level = "AA", CanBeAutomated = true },
            new() { Criterion = "1.4.1", Name = "Use of Color", Principle = "Perceivable", Level = "A", CanBeAutomated = false },
            new() { Criterion = "1.4.2", Name = "Audio Control", Principle = "Perceivable", Level = "A", CanBeAutomated = false },
            new() { Criterion = "1.4.3", Name = "Contrast (Minimum)", Principle = "Perceivable", Level = "AA", CanBeAutomated = true },
            new() { Criterion = "1.4.4", Name = "Resize Text", Principle = "Perceivable", Level = "AA", CanBeAutomated = false },
            new() { Criterion = "1.4.5", Name = "Images of Text", Principle = "Perceivable", Level = "AA", CanBeAutomated = false },
            new() { Criterion = "1.4.10", Name = "Reflow", Principle = "Perceivable", Level = "AA", CanBeAutomated = false },
            new() { Criterion = "1.4.11", Name = "Non-text Contrast", Principle = "Perceivable", Level = "AA", CanBeAutomated = true },
            new() { Criterion = "1.4.12", Name = "Text Spacing", Principle = "Perceivable", Level = "AA", CanBeAutomated = false },
            new() { Criterion = "1.4.13", Name = "Content on Hover or Focus", Principle = "Perceivable", Level = "AA", CanBeAutomated = false },
            // Operable
            new() { Criterion = "2.1.1", Name = "Keyboard", Principle = "Operable", Level = "A", CanBeAutomated = false },
            new() { Criterion = "2.1.2", Name = "No Keyboard Trap", Principle = "Operable", Level = "A", CanBeAutomated = false },
            new() { Criterion = "2.1.4", Name = "Character Key Shortcuts", Principle = "Operable", Level = "A", CanBeAutomated = false },
            new() { Criterion = "2.2.1", Name = "Timing Adjustable", Principle = "Operable", Level = "A", CanBeAutomated = false },
            new() { Criterion = "2.2.2", Name = "Pause, Stop, Hide", Principle = "Operable", Level = "A", CanBeAutomated = false },
            new() { Criterion = "2.3.1", Name = "Three Flashes or Below Threshold", Principle = "Operable", Level = "A", CanBeAutomated = false },
            new() { Criterion = "2.4.1", Name = "Bypass Blocks", Principle = "Operable", Level = "A", CanBeAutomated = true },
            new() { Criterion = "2.4.2", Name = "Page Titled", Principle = "Operable", Level = "A", CanBeAutomated = true },
            new() { Criterion = "2.4.3", Name = "Focus Order", Principle = "Operable", Level = "A", CanBeAutomated = false },
            new() { Criterion = "2.4.4", Name = "Link Purpose (In Context)", Principle = "Operable", Level = "A", CanBeAutomated = true },
            new() { Criterion = "2.4.5", Name = "Multiple Ways", Principle = "Operable", Level = "AA", CanBeAutomated = false },
            new() { Criterion = "2.4.6", Name = "Headings and Labels", Principle = "Operable", Level = "AA", CanBeAutomated = true },
            new() { Criterion = "2.4.7", Name = "Focus Visible", Principle = "Operable", Level = "AA", CanBeAutomated = false },
            new() { Criterion = "2.4.11", Name = "Focus Not Obscured (Minimum)", Principle = "Operable", Level = "AA", CanBeAutomated = false },
            new() { Criterion = "2.5.1", Name = "Pointer Gestures", Principle = "Operable", Level = "A", CanBeAutomated = false },
            new() { Criterion = "2.5.2", Name = "Pointer Cancellation", Principle = "Operable", Level = "A", CanBeAutomated = false },
            new() { Criterion = "2.5.3", Name = "Label in Name", Principle = "Operable", Level = "A", CanBeAutomated = true },
            new() { Criterion = "2.5.4", Name = "Motion Actuation", Principle = "Operable", Level = "A", CanBeAutomated = false },
            new() { Criterion = "2.5.7", Name = "Dragging Movements", Principle = "Operable", Level = "AA", CanBeAutomated = false },
            new() { Criterion = "2.5.8", Name = "Target Size (Minimum)", Principle = "Operable", Level = "AA", CanBeAutomated = true },
            // Understandable
            new() { Criterion = "3.1.1", Name = "Language of Page", Principle = "Understandable", Level = "A", CanBeAutomated = true },
            new() { Criterion = "3.1.2", Name = "Language of Parts", Principle = "Understandable", Level = "AA", CanBeAutomated = true },
            new() { Criterion = "3.2.1", Name = "On Focus", Principle = "Understandable", Level = "A", CanBeAutomated = false },
            new() { Criterion = "3.2.2", Name = "On Input", Principle = "Understandable", Level = "A", CanBeAutomated = false },
            new() { Criterion = "3.2.6", Name = "Consistent Help", Principle = "Understandable", Level = "A", CanBeAutomated = false },
            new() { Criterion = "3.3.1", Name = "Error Identification", Principle = "Understandable", Level = "A", CanBeAutomated = false },
            new() { Criterion = "3.3.2", Name = "Labels or Instructions", Principle = "Understandable", Level = "A", CanBeAutomated = true },
            new() { Criterion = "3.3.3", Name = "Error Suggestion", Principle = "Understandable", Level = "AA", CanBeAutomated = false },
            new() { Criterion = "3.3.4", Name = "Error Prevention (Legal, Financial, Data)", Principle = "Understandable", Level = "AA", CanBeAutomated = false },
            new() { Criterion = "3.3.7", Name = "Redundant Entry", Principle = "Understandable", Level = "A", CanBeAutomated = false },
            // Robust
            new() { Criterion = "4.1.2", Name = "Name, Role, Value", Principle = "Robust", Level = "A", CanBeAutomated = true },
            new() { Criterion = "4.1.3", Name = "Status Messages", Principle = "Robust", Level = "AA", CanBeAutomated = false },
        };
    }

    public async Task<DataObjects.AuditReport> GenerateAuditReport(Guid SiteId, DataObjects.User? CurrentUser = null)
    {
        DataObjects.AuditReport output = new DataObjects.AuditReport();

        try {
            List<DataObjects.WcagCriterionDefinition> definitions = GetWcagCriteria();
            output.SiteId = SiteId;
            output.GeneratedAt = DateTime.UtcNow;
            output.TotalCriteria = definitions.Count;

            // Load site info
            List<DataObjects.Site> sites = await GetSites(new List<Guid> { SiteId }, Guid.Empty, CurrentUser);
            DataObjects.Site? site = sites?.FirstOrDefault();
            if (site != null) {
                output.SiteName = site.Name;
                output.SiteUrl = site.BaseUrl;
                output.LastScanAt = site.LastScanAt;
            }

            // Load manual checks
            List<DataObjects.ManualCheckResult> manualChecks = await GetManualChecks(null, SiteId);

            // Load automated violations from the latest scan
            List<DataObjects.A11yViolation> violations = new List<DataObjects.A11yViolation>();
            if (site != null && site.LastScanRunId.HasValue) {
                violations = await GetViolationsByRule(site.LastScanRunId.Value, null);
            }

            // Build per-criterion conformance using WcagCriterionStatus
            Dictionary<string, DataObjects.WcagCriterionStatus> statusMap = new();

            // Initialize all criteria as Not Evaluated
            foreach (var def in definitions) {
                statusMap[def.Criterion] = new DataObjects.WcagCriterionStatus {
                    Criterion = def.Criterion,
                    Name = def.Name,
                    Principle = def.Principle,
                    Level = def.Level,
                    CanBeAutomated = def.CanBeAutomated,
                    ConformanceLevel = "Not Evaluated",
                    Source = "None",
                };
            }

            // Map automated violations to criteria
            foreach (var violation in violations) {
                if (!String.IsNullOrWhiteSpace(violation.WcagCriteria)) {
                    var criteriaList = violation.WcagCriteria.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    foreach (var criterion in criteriaList) {
                        string normalized = NormalizeCriterionNumber(criterion);
                        if (statusMap.ContainsKey(normalized)) {
                            statusMap[normalized].ConformanceLevel = "Does Not Support";
                            statusMap[normalized].Source = "Automated";
                            statusMap[normalized].AutomatedViolationCount++;
                        }
                    }
                }
            }

            // Layer manual checks on top (manual overrides automated)
            foreach (var check in manualChecks) {
                if (!String.IsNullOrWhiteSpace(check.Status) && check.Status != "Not Tested") {
                    string normalized = check.WcagCriterion ?? "";
                    if (statusMap.ContainsKey(normalized)) {
                        if (check.Status == "Pass") {
                            statusMap[normalized].ConformanceLevel = "Supports";
                        } else if (check.Status == "Fail") {
                            statusMap[normalized].ConformanceLevel = "Does Not Support";
                        } else if (check.Status == "N/A") {
                            statusMap[normalized].ConformanceLevel = "Not Applicable";
                        }
                        statusMap[normalized].Source = "Manual";
                        statusMap[normalized].ManualStatus = check.Status;
                        statusMap[normalized].Remarks = check.Notes;
                    }
                }
            }

            // For automatable criteria with no violations and a scan has run, mark as Supports
            if (site != null && site.LastScanAt.HasValue) {
                foreach (var def in definitions.Where(x => x.CanBeAutomated)) {
                    if (statusMap.ContainsKey(def.Criterion) && statusMap[def.Criterion].ConformanceLevel == "Not Evaluated") {
                        statusMap[def.Criterion].ConformanceLevel = "Supports";
                        statusMap[def.Criterion].Source = "Automated";
                    }
                }
            }

            output.Criteria = statusMap.Values.OrderBy(x => x.Criterion).ToList();
            output.EvaluatedCount = output.Criteria.Count(x => x.ConformanceLevel != "Not Evaluated");
            output.SupportsCount = output.Criteria.Count(x => x.ConformanceLevel == "Supports");
            output.DoesNotSupportCount = output.Criteria.Count(x => x.ConformanceLevel == "Does Not Support");
            output.PartialCount = output.Criteria.Count(x => x.ConformanceLevel == "Partially Supports");
            output.NotEvaluatedCount = output.Criteria.Count(x => x.ConformanceLevel == "Not Evaluated");
        } catch (Exception ex) {
            Console.WriteLine("Error in GenerateAuditReport: " + RecurseExceptionAsString(ex));
        }

        return output;
    }

    public async Task<string> ExportAuditCsv(Guid SiteId)
    {
        string output = String.Empty;

        try {
            DataObjects.AuditReport report = await GenerateAuditReport(SiteId);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Criterion,Name,Principle,Level,Conformance Status,Source,Remarks");

            foreach (var item in report.Criteria) {
                sb.AppendLine(
                    "\"" + (item.Criterion ?? "") + "\"," +
                    "\"" + (item.Name ?? "").Replace("\"", "\"\"") + "\"," +
                    "\"" + (item.Principle ?? "") + "\"," +
                    "\"" + (item.Level ?? "") + "\"," +
                    "\"" + (item.ConformanceLevel ?? "") + "\"," +
                    "\"" + (item.Source ?? "") + "\"," +
                    "\"" + (item.Remarks ?? "").Replace("\"", "\"\"") + "\""
                );
            }

            output = sb.ToString();
        } catch (Exception ex) {
            Console.WriteLine("Error in ExportAuditCsv: " + RecurseExceptionAsString(ex));
        }

        return output;
    }

    private static string NormalizeCriterionNumber(string raw)
    {
        string cleaned = raw.Trim().ToLower()
            .Replace("wcag", "").Replace("sc", "").Replace("-", ".").Replace("_", ".").Trim('.');

        var match = Regex.Match(cleaned, @"(\d+)\.?(\d+)\.?(\d+)");
        if (match.Success) {
            return match.Groups[1].Value + "." + match.Groups[2].Value + "." + match.Groups[3].Value;
        }
        return raw.Trim();
    }
}
