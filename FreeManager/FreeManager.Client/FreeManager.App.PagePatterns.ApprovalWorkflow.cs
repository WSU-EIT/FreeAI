namespace FreeManager.Client;

#region PagePatterns - ApprovalWorkflow
// ============================================================================
// PAGE PATTERN: APPROVAL WORKFLOW
// Multi-step approval process with status tracking.
// Use case: Purchase requests, leave approval, document review.
//
// Generated Files:
// - ApprovalDetail.App.razor    - Main detail view with workflow info
// - WorkflowProgress.App.razor  - Visual workflow step indicator
// - ApprovalHistory.App.razor   - Timeline of approval actions
// - ApprovalActions.App.razor   - Approve/Reject/Request Changes buttons
// - ApprovalList.App.razor      - List of pending approvals
// ============================================================================

public static partial class PagePatterns
{
    /// <summary>
    /// Generate ApprovalWorkflow files for the given entity/project.
    /// </summary>
    public static List<(string FileName, string FileType, string Content)> GetApprovalWorkflowFiles(
        string projectName,
        DataObjects.EntityDefinition? entity = null)
    {
        List<(string, string, string)> files = new();
        string name = entity?.Name ?? projectName;

        files.Add(($"{projectName}.App.ApprovalDetail.razor", "RazorComponent", GetApprovalWorkflow_ApprovalDetail(name, entity)));
        files.Add(($"{projectName}.App.WorkflowProgress.razor", "RazorComponent", GetApprovalWorkflow_WorkflowProgress(name, entity)));
        files.Add(($"{projectName}.App.ApprovalHistory.razor", "RazorComponent", GetApprovalWorkflow_ApprovalHistory(name, entity)));
        files.Add(($"{projectName}.App.ApprovalActions.razor", "RazorComponent", GetApprovalWorkflow_ApprovalActions(name, entity)));
        files.Add(($"{projectName}.App.ApprovalList.razor", "RazorComponent", GetApprovalWorkflow_ApprovalList(name, entity)));

        return files;
    }

    // ============================================================
    // ApprovalDetail.App.razor - Main detail view with workflow info
    // ============================================================
    private static string GetApprovalWorkflow_ApprovalDetail(string name, DataObjects.EntityDefinition? entity) => $@"@* {name} - ApprovalDetail.App.razor *@
@* Approval Workflow Pattern - Main detail view *@
@page ""/approvals/{{Id:guid}}""
@page ""/{{TenantCode}}/approvals/{{Id:guid}}""
@implements IDisposable
@inject BlazorDataModel Model
@inject HttpClient Http

@if (Model.Loaded && Model.LoggedIn) {{
    <div class=""container-fluid"">
        <div class=""@Model.StickyMenuClass"">
            <div class=""d-flex justify-content-between align-items-center"">
                <div>
                    <nav aria-label=""breadcrumb"">
                        <ol class=""breadcrumb mb-1"">
                            <li class=""breadcrumb-item""><a href=""/approvals"">Approvals</a></li>
                            <li class=""breadcrumb-item active"">@_item.Title</li>
                        </ol>
                    </nav>
                    <h1 class=""page-title mb-0"">
                        <i class=""fa-solid fa-stamp me-2""></i>@_item.Title
                    </h1>
                </div>
                <div class=""d-flex gap-2"">
                    <span class=""badge @GetStatusBadgeClass(_item.Status) fs-6"">
                        <i class=""fa-solid @GetStatusIcon(_item.Status) me-1""></i>@_item.Status
                    </span>
                </div>
            </div>
        </div>

        @if (_loading) {{
            <div class=""text-center py-5"">
                <i class=""fa-solid fa-spinner fa-spin fa-2x""></i>
                <p class=""mt-2 text-muted"">Loading approval details...</p>
            </div>
        }} else {{
            <div class=""row mt-4"">
                <!-- Left Column: Workflow Progress & Details -->
                <div class=""col-lg-8"">
                    <!-- Workflow Progress -->
                    <div class=""card shadow-sm mb-4"">
                        <div class=""card-header bg-light"">
                            <h5 class=""mb-0"">
                                <i class=""fa-solid fa-diagram-project me-2""></i>Workflow Progress
                            </h5>
                        </div>
                        <div class=""card-body"">
                            <{name}_App_WorkflowProgress Steps=""@_item.WorkflowSteps"" CurrentStep=""@_item.CurrentStep"" />
                        </div>
                    </div>

                    <!-- Request Details -->
                    <div class=""card shadow-sm mb-4"">
                        <div class=""card-header bg-light"">
                            <h5 class=""mb-0"">
                                <i class=""fa-solid fa-file-lines me-2""></i>Request Details
                            </h5>
                        </div>
                        <div class=""card-body"">
                            <dl class=""row mb-0"">
                                <dt class=""col-sm-3"">Request Type</dt>
                                <dd class=""col-sm-9"">@_item.RequestType</dd>

                                <dt class=""col-sm-3"">Submitted By</dt>
                                <dd class=""col-sm-9"">
                                    <i class=""fa-solid fa-user me-1""></i>@_item.SubmittedBy
                                </dd>

                                <dt class=""col-sm-3"">Submitted On</dt>
                                <dd class=""col-sm-9"">@_item.SubmittedAt.ToString(""MMMM dd, yyyy 'at' h:mm tt"")</dd>

                                @if (!string.IsNullOrEmpty(_item.Description)) {{
                                    <dt class=""col-sm-3"">Description</dt>
                                    <dd class=""col-sm-9"">@_item.Description</dd>
                                }}

                                @if (_item.Amount.HasValue) {{
                                    <dt class=""col-sm-3"">Amount</dt>
                                    <dd class=""col-sm-9"">
                                        <strong class=""text-primary"">@_item.Amount?.ToString(""C"")</strong>
                                    </dd>
                                }}
                            </dl>
                        </div>
                    </div>

                    <!-- Approval History -->
                    <div class=""card shadow-sm mb-4"">
                        <div class=""card-header bg-light"">
                            <h5 class=""mb-0"">
                                <i class=""fa-solid fa-clock-rotate-left me-2""></i>Approval History
                            </h5>
                        </div>
                        <div class=""card-body p-0"">
                            <{name}_App_ApprovalHistory History=""@_item.History"" />
                        </div>
                    </div>
                </div>

                <!-- Right Column: Actions & Summary -->
                <div class=""col-lg-4"">
                    <!-- Actions Card -->
                    @if (_item.CanTakeAction) {{
                        <div class=""card shadow-sm mb-4 border-primary"">
                            <div class=""card-header bg-primary text-white"">
                                <h5 class=""mb-0"">
                                    <i class=""fa-solid fa-gavel me-2""></i>Your Action Required
                                </h5>
                            </div>
                            <div class=""card-body"">
                                <{name}_App_ApprovalActions
                                    Item=""@_item""
                                    OnApprove=""HandleApprove""
                                    OnReject=""HandleReject""
                                    OnRequestChanges=""HandleRequestChanges"" />
                            </div>
                        </div>
                    }}

                    <!-- Summary Card -->
                    <div class=""card shadow-sm mb-4"">
                        <div class=""card-header bg-light"">
                            <h5 class=""mb-0"">
                                <i class=""fa-solid fa-info-circle me-2""></i>Summary
                            </h5>
                        </div>
                        <div class=""card-body"">
                            <div class=""d-flex justify-content-between mb-2"">
                                <span class=""text-muted"">Priority</span>
                                <span class=""badge @GetPriorityBadgeClass(_item.Priority)"">@_item.Priority</span>
                            </div>
                            <div class=""d-flex justify-content-between mb-2"">
                                <span class=""text-muted"">Current Approver</span>
                                <span>@_item.CurrentApprover</span>
                            </div>
                            <div class=""d-flex justify-content-between mb-2"">
                                <span class=""text-muted"">Due Date</span>
                                <span class=""@(IsPastDue(_item.DueDate) ? ""text-danger"" : """")"">
                                    @_item.DueDate?.ToString(""MMM dd, yyyy"") ?? ""No deadline""
                                </span>
                            </div>
                            <div class=""d-flex justify-content-between"">
                                <span class=""text-muted"">Approvals</span>
                                <span>@_item.ApprovalCount / @_item.TotalSteps</span>
                            </div>
                        </div>
                    </div>

                    <!-- Attachments -->
                    @if (_item.Attachments?.Any() == true) {{
                        <div class=""card shadow-sm mb-4"">
                            <div class=""card-header bg-light"">
                                <h5 class=""mb-0"">
                                    <i class=""fa-solid fa-paperclip me-2""></i>Attachments
                                </h5>
                            </div>
                            <ul class=""list-group list-group-flush"">
                                @foreach (var attachment in _item.Attachments) {{
                                    <li class=""list-group-item d-flex justify-content-between align-items-center"">
                                        <span>
                                            <i class=""fa-solid fa-file me-2""></i>@attachment.FileName
                                        </span>
                                        <a href=""@attachment.Url"" class=""btn btn-sm btn-outline-primary"" target=""_blank"">
                                            <i class=""fa-solid fa-download""></i>
                                        </a>
                                    </li>
                                }}
                            </ul>
                        </div>
                    }}

                    <!-- Email Notification Settings -->
                    <div class=""card shadow-sm"">
                        <div class=""card-header bg-light"">
                            <h5 class=""mb-0"">
                                <i class=""fa-solid fa-bell me-2""></i>Notifications
                            </h5>
                        </div>
                        <div class=""card-body"">
                            <div class=""form-check form-switch mb-2"">
                                <input class=""form-check-input"" type=""checkbox"" @bind=""_notifyOnUpdate"" />
                                <label class=""form-check-label"">Notify on status change</label>
                            </div>
                            <div class=""form-check form-switch"">
                                <input class=""form-check-input"" type=""checkbox"" @bind=""_notifyOnComment"" />
                                <label class=""form-check-label"">Notify on new comment</label>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        }}
    </div>
}}

@code {{
    [Parameter] public Guid Id {{ get; set; }}

    private bool _loading = true;
    private ApprovalItem _item = new();
    private bool _notifyOnUpdate = true;
    private bool _notifyOnComment = true;

    public void Dispose() {{ Model.OnChange -= StateHasChanged; }}

    protected override void OnInitialized()
    {{
        Model.OnChange += StateHasChanged;
    }}

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {{
        if (firstRender) await LoadData();
    }}

    private async Task LoadData()
    {{
        _loading = true;
        StateHasChanged();

        // Load approval item by ID
        _item = await Helpers.GetOrPost<ApprovalItem>(
            $""api/approvals/{{Id}}"", null) ?? new();

        _loading = false;
        StateHasChanged();
    }}

    private async Task HandleApprove(ApprovalActionRequest request)
    {{
        await Helpers.GetOrPost<bool>(""api/approvals/approve"", request);
        await LoadData();
    }}

    private async Task HandleReject(ApprovalActionRequest request)
    {{
        await Helpers.GetOrPost<bool>(""api/approvals/reject"", request);
        await LoadData();
    }}

    private async Task HandleRequestChanges(ApprovalActionRequest request)
    {{
        await Helpers.GetOrPost<bool>(""api/approvals/request-changes"", request);
        await LoadData();
    }}

    private string GetStatusBadgeClass(string status) => status switch
    {{
        ""Pending"" => ""bg-warning text-dark"",
        ""Approved"" => ""bg-success"",
        ""Rejected"" => ""bg-danger"",
        ""Changes Requested"" => ""bg-info"",
        ""Cancelled"" => ""bg-secondary"",
        _ => ""bg-light text-dark""
    }};

    private string GetStatusIcon(string status) => status switch
    {{
        ""Pending"" => ""fa-clock"",
        ""Approved"" => ""fa-check-circle"",
        ""Rejected"" => ""fa-times-circle"",
        ""Changes Requested"" => ""fa-edit"",
        ""Cancelled"" => ""fa-ban"",
        _ => ""fa-question-circle""
    }};

    private string GetPriorityBadgeClass(string priority) => priority switch
    {{
        ""High"" => ""bg-danger"",
        ""Medium"" => ""bg-warning text-dark"",
        ""Low"" => ""bg-info"",
        _ => ""bg-secondary""
    }};

    private bool IsPastDue(DateTime? dueDate) => dueDate.HasValue && dueDate.Value < DateTime.Now;

    // DTOs for this component
    public class ApprovalItem
    {{
        public Guid Id {{ get; set; }}
        public string Title {{ get; set; }} = string.Empty;
        public string RequestType {{ get; set; }} = string.Empty;
        public string Description {{ get; set; }} = string.Empty;
        public string Status {{ get; set; }} = ""Pending"";
        public string Priority {{ get; set; }} = ""Medium"";
        public string SubmittedBy {{ get; set; }} = string.Empty;
        public DateTime SubmittedAt {{ get; set; }}
        public DateTime? DueDate {{ get; set; }}
        public decimal? Amount {{ get; set; }}
        public string CurrentApprover {{ get; set; }} = string.Empty;
        public int CurrentStep {{ get; set; }}
        public int TotalSteps {{ get; set; }}
        public int ApprovalCount {{ get; set; }}
        public bool CanTakeAction {{ get; set; }}
        public List<WorkflowStep> WorkflowSteps {{ get; set; }} = new();
        public List<ApprovalHistoryEntry> History {{ get; set; }} = new();
        public List<AttachmentInfo> Attachments {{ get; set; }} = new();
    }}

    public class WorkflowStep
    {{
        public int StepNumber {{ get; set; }}
        public string Name {{ get; set; }} = string.Empty;
        public string Approver {{ get; set; }} = string.Empty;
        public string Status {{ get; set; }} = ""Pending"";
        public DateTime? CompletedAt {{ get; set; }}
    }}

    public class ApprovalHistoryEntry
    {{
        public DateTime Timestamp {{ get; set; }}
        public string Action {{ get; set; }} = string.Empty;
        public string User {{ get; set; }} = string.Empty;
        public string Comment {{ get; set; }} = string.Empty;
        public string StepName {{ get; set; }} = string.Empty;
    }}

    public class AttachmentInfo
    {{
        public string FileName {{ get; set; }} = string.Empty;
        public string Url {{ get; set; }} = string.Empty;
    }}

    public class ApprovalActionRequest
    {{
        public Guid ApprovalId {{ get; set; }}
        public string Comment {{ get; set; }} = string.Empty;
    }}
}}
";

    // ============================================================
    // WorkflowProgress.App.razor - Visual workflow step indicator
    // ============================================================
    private static string GetApprovalWorkflow_WorkflowProgress(string name, DataObjects.EntityDefinition? entity) => $@"@* {name} - WorkflowProgress.App.razor *@
@* Approval Workflow Pattern - Visual step indicator *@

<div class=""workflow-progress"">
    <div class=""d-flex justify-content-between position-relative"">
        <!-- Progress Line -->
        <div class=""workflow-line"" style=""position: absolute; top: 20px; left: 40px; right: 40px; height: 3px; background: #e9ecef; z-index: 0;"">
            <div class=""workflow-line-progress"" style=""height: 100%; background: #0d6efd; width: @(GetProgressPercentage())%;""></div>
        </div>

        @foreach (var step in Steps)
        {{
            <div class=""workflow-step text-center position-relative"" style=""z-index: 1; flex: 1;"">
                <div class=""workflow-step-icon mx-auto mb-2 rounded-circle d-flex align-items-center justify-content-center @GetStepClass(step)""
                     style=""width: 40px; height: 40px; border: 3px solid;"">
                    @if (step.Status == ""Completed"")
                    {{
                        <i class=""fa-solid fa-check""></i>
                    }}
                    else if (step.Status == ""Rejected"")
                    {{
                        <i class=""fa-solid fa-times""></i>
                    }}
                    else if (step.StepNumber == CurrentStep)
                    {{
                        <i class=""fa-solid fa-hourglass-half""></i>
                    }}
                    else
                    {{
                        <span>@step.StepNumber</span>
                    }}
                </div>
                <div class=""workflow-step-label"">
                    <div class=""fw-semibold small"">@step.Name</div>
                    <div class=""text-muted small"">@step.Approver</div>
                    @if (step.CompletedAt.HasValue)
                    {{
                        <div class=""text-success small"">
                            <i class=""fa-solid fa-check me-1""></i>@step.CompletedAt?.ToString(""MMM dd"")
                        </div>
                    }}
                    else if (step.StepNumber == CurrentStep)
                    {{
                        <div class=""text-primary small"">
                            <i class=""fa-solid fa-arrow-right me-1""></i>Awaiting
                        </div>
                    }}
                </div>
            </div>
        }}
    </div>
</div>

<style>
    .workflow-step-icon {{
        transition: all 0.3s ease;
    }}
    .workflow-step-icon.step-completed {{
        background-color: #198754;
        border-color: #198754 !important;
        color: white;
    }}
    .workflow-step-icon.step-current {{
        background-color: #0d6efd;
        border-color: #0d6efd !important;
        color: white;
        animation: pulse 2s infinite;
    }}
    .workflow-step-icon.step-rejected {{
        background-color: #dc3545;
        border-color: #dc3545 !important;
        color: white;
    }}
    .workflow-step-icon.step-pending {{
        background-color: white;
        border-color: #dee2e6 !important;
        color: #6c757d;
    }}
    @@keyframes pulse {{
        0%, 100% {{ box-shadow: 0 0 0 0 rgba(13, 110, 253, 0.4); }}
        50% {{ box-shadow: 0 0 0 10px rgba(13, 110, 253, 0); }}
    }}
</style>

@code {{
    [Parameter] public List<WorkflowStep> Steps {{ get; set; }} = new();
    [Parameter] public int CurrentStep {{ get; set; }}

    private string GetStepClass(WorkflowStep step)
    {{
        if (step.Status == ""Completed"") return ""step-completed"";
        if (step.Status == ""Rejected"") return ""step-rejected"";
        if (step.StepNumber == CurrentStep) return ""step-current"";
        return ""step-pending"";
    }}

    private int GetProgressPercentage()
    {{
        if (Steps.Count == 0) return 0;
        var completedSteps = Steps.Count(s => s.Status == ""Completed"");
        return (int)((double)completedSteps / Steps.Count * 100);
    }}

    public class WorkflowStep
    {{
        public int StepNumber {{ get; set; }}
        public string Name {{ get; set; }} = string.Empty;
        public string Approver {{ get; set; }} = string.Empty;
        public string Status {{ get; set; }} = ""Pending"";
        public DateTime? CompletedAt {{ get; set; }}
    }}
}}
";

    // ============================================================
    // ApprovalHistory.App.razor - Timeline of approval actions
    // ============================================================
    private static string GetApprovalWorkflow_ApprovalHistory(string name, DataObjects.EntityDefinition? entity) => $@"@* {name} - ApprovalHistory.App.razor *@
@* Approval Workflow Pattern - History timeline *@

@if (History?.Any() != true)
{{
    <div class=""text-center py-4 text-muted"">
        <i class=""fa-solid fa-clock-rotate-left fa-2x mb-2 opacity-50""></i>
        <p class=""mb-0"">No approval history yet</p>
    </div>
}}
else
{{
    <div class=""approval-history"">
        @foreach (var entry in History.OrderByDescending(h => h.Timestamp))
        {{
            <div class=""history-entry d-flex p-3 @(entry != History.First() ? ""border-top"" : """")"">
                <div class=""history-icon me-3"">
                    <div class=""rounded-circle d-flex align-items-center justify-content-center @GetActionClass(entry.Action)""
                         style=""width: 36px; height: 36px;"">
                        <i class=""fa-solid @GetActionIcon(entry.Action)""></i>
                    </div>
                </div>
                <div class=""history-content flex-grow-1"">
                    <div class=""d-flex justify-content-between align-items-start"">
                        <div>
                            <strong>@entry.User</strong>
                            <span class=""text-muted"">@GetActionText(entry.Action)</span>
                        </div>
                        <small class=""text-muted"">
                            <i class=""fa-regular fa-clock me-1""></i>
                            @FormatTimestamp(entry.Timestamp)
                        </small>
                    </div>
                    @if (!string.IsNullOrEmpty(entry.StepName))
                    {{
                        <div class=""mt-1"">
                            <span class=""badge bg-light text-dark"">
                                <i class=""fa-solid fa-diagram-next me-1""></i>@entry.StepName
                            </span>
                        </div>
                    }}
                    @if (!string.IsNullOrEmpty(entry.Comment))
                    {{
                        <div class=""mt-2 p-2 bg-light rounded"">
                            <i class=""fa-solid fa-quote-left text-muted me-1 small""></i>
                            <span class=""fst-italic"">@entry.Comment</span>
                        </div>
                    }}
                </div>
            </div>
        }}
    </div>
}}

<style>
    .history-entry:hover {{
        background-color: #f8f9fa;
    }}
    .history-icon .action-approved {{
        background-color: #d1e7dd;
        color: #198754;
    }}
    .history-icon .action-rejected {{
        background-color: #f8d7da;
        color: #dc3545;
    }}
    .history-icon .action-changes {{
        background-color: #cff4fc;
        color: #0dcaf0;
    }}
    .history-icon .action-submitted {{
        background-color: #e2e3e5;
        color: #6c757d;
    }}
    .history-icon .action-comment {{
        background-color: #fff3cd;
        color: #ffc107;
    }}
</style>

@code {{
    [Parameter] public List<ApprovalHistoryEntry> History {{ get; set; }} = new();

    private string GetActionClass(string action) => action.ToLower() switch
    {{
        ""approved"" => ""action-approved"",
        ""rejected"" => ""action-rejected"",
        ""changes requested"" or ""requested changes"" => ""action-changes"",
        ""submitted"" => ""action-submitted"",
        ""commented"" => ""action-comment"",
        _ => ""action-submitted""
    }};

    private string GetActionIcon(string action) => action.ToLower() switch
    {{
        ""approved"" => ""fa-check"",
        ""rejected"" => ""fa-times"",
        ""changes requested"" or ""requested changes"" => ""fa-edit"",
        ""submitted"" => ""fa-paper-plane"",
        ""commented"" => ""fa-comment"",
        _ => ""fa-info""
    }};

    private string GetActionText(string action) => action.ToLower() switch
    {{
        ""approved"" => ""approved this request"",
        ""rejected"" => ""rejected this request"",
        ""changes requested"" or ""requested changes"" => ""requested changes"",
        ""submitted"" => ""submitted this request"",
        ""commented"" => ""added a comment"",
        _ => action.ToLower()
    }};

    private string FormatTimestamp(DateTime timestamp)
    {{
        var diff = DateTime.Now - timestamp;
        if (diff.TotalMinutes < 1) return ""Just now"";
        if (diff.TotalHours < 1) return $""{{(int)diff.TotalMinutes}}m ago"";
        if (diff.TotalDays < 1) return $""{{(int)diff.TotalHours}}h ago"";
        if (diff.TotalDays < 7) return $""{{(int)diff.TotalDays}}d ago"";
        return timestamp.ToString(""MMM dd, yyyy"");
    }}

    public class ApprovalHistoryEntry
    {{
        public DateTime Timestamp {{ get; set; }}
        public string Action {{ get; set; }} = string.Empty;
        public string User {{ get; set; }} = string.Empty;
        public string Comment {{ get; set; }} = string.Empty;
        public string StepName {{ get; set; }} = string.Empty;
    }}
}}
";

    // ============================================================
    // ApprovalActions.App.razor - Approve/Reject/Request Changes buttons
    // ============================================================
    private static string GetApprovalWorkflow_ApprovalActions(string name, DataObjects.EntityDefinition? entity) => $@"@* {name} - ApprovalActions.App.razor *@
@* Approval Workflow Pattern - Action buttons with role-based visibility *@

<div class=""approval-actions"">
    <p class=""text-muted mb-3"">
        <i class=""fa-solid fa-info-circle me-1""></i>
        You are the designated approver for this step. Please review and take action.
    </p>

    <!-- Comment Field -->
    <div class=""mb-3"">
        <label class=""form-label"">Comment <span class=""text-muted"">(optional for approval, required for rejection)</span></label>
        <textarea class=""form-control"" rows=""3""
                  placeholder=""Add your comments here...""
                  @bind=""_comment""></textarea>
    </div>

    <!-- Action Buttons -->
    <div class=""d-grid gap-2"">
        <button class=""btn btn-success btn-lg"" @onclick=""Approve"" disabled=""@_processing"">
            @if (_processing && _currentAction == ""approve"")
            {{
                <i class=""fa-solid fa-spinner fa-spin me-2""></i>
            }}
            else
            {{
                <i class=""fa-solid fa-check-circle me-2""></i>
            }}
            Approve
        </button>

        <button class=""btn btn-warning"" @onclick=""RequestChanges"" disabled=""@_processing"">
            @if (_processing && _currentAction == ""changes"")
            {{
                <i class=""fa-solid fa-spinner fa-spin me-2""></i>
            }}
            else
            {{
                <i class=""fa-solid fa-edit me-2""></i>
            }}
            Request Changes
        </button>

        <button class=""btn btn-danger"" @onclick=""Reject"" disabled=""@_processing"">
            @if (_processing && _currentAction == ""reject"")
            {{
                <i class=""fa-solid fa-spinner fa-spin me-2""></i>
            }}
            else
            {{
                <i class=""fa-solid fa-times-circle me-2""></i>
            }}
            Reject
        </button>
    </div>

    <!-- Delegation Option -->
    <hr class=""my-3"" />
    <div class=""text-center"">
        <button class=""btn btn-link btn-sm text-muted"" @onclick=""ToggleDelegation"">
            <i class=""fa-solid fa-user-tag me-1""></i>Delegate to someone else
        </button>
    </div>

    @if (_showDelegation)
    {{
        <div class=""mt-3 p-3 bg-light rounded"">
            <label class=""form-label"">Select delegate</label>
            <select class=""form-select mb-2"" @bind=""_delegateTo"">
                <option value="""">Choose a person...</option>
                @foreach (var user in _availableDelegates)
                {{
                    <option value=""@user.Id"">@user.Name - @user.Role</option>
                }}
            </select>
            <button class=""btn btn-outline-primary btn-sm"" @onclick=""Delegate"" disabled=""@(string.IsNullOrEmpty(_delegateTo))"">
                <i class=""fa-solid fa-share me-1""></i>Delegate Approval
            </button>
        </div>
    }}

    <!-- Validation Message -->
    @if (!string.IsNullOrEmpty(_validationMessage))
    {{
        <div class=""alert alert-warning mt-3 mb-0"">
            <i class=""fa-solid fa-exclamation-triangle me-1""></i>@_validationMessage
        </div>
    }}
</div>

@code {{
    [Parameter] public ApprovalItem Item {{ get; set; }} = new();
    [Parameter] public EventCallback<ApprovalActionRequest> OnApprove {{ get; set; }}
    [Parameter] public EventCallback<ApprovalActionRequest> OnReject {{ get; set; }}
    [Parameter] public EventCallback<ApprovalActionRequest> OnRequestChanges {{ get; set; }}

    private string _comment = string.Empty;
    private bool _processing = false;
    private string _currentAction = string.Empty;
    private string _validationMessage = string.Empty;
    private bool _showDelegation = false;
    private string _delegateTo = string.Empty;
    private List<DelegateUser> _availableDelegates = new()
    {{
        new() {{ Id = ""1"", Name = ""John Manager"", Role = ""Department Head"" }},
        new() {{ Id = ""2"", Name = ""Jane Director"", Role = ""Finance Director"" }},
        new() {{ Id = ""3"", Name = ""Bob Supervisor"", Role = ""Team Lead"" }}
    }};

    private async Task Approve()
    {{
        _validationMessage = string.Empty;
        _processing = true;
        _currentAction = ""approve"";
        StateHasChanged();

        var request = new ApprovalActionRequest
        {{
            ApprovalId = Item.Id,
            Comment = _comment
        }};

        await OnApprove.InvokeAsync(request);
        _processing = false;
        _comment = string.Empty;
    }}

    private async Task Reject()
    {{
        if (string.IsNullOrWhiteSpace(_comment))
        {{
            _validationMessage = ""Please provide a reason for rejection."";
            return;
        }}

        _validationMessage = string.Empty;
        _processing = true;
        _currentAction = ""reject"";
        StateHasChanged();

        var request = new ApprovalActionRequest
        {{
            ApprovalId = Item.Id,
            Comment = _comment
        }};

        await OnReject.InvokeAsync(request);
        _processing = false;
        _comment = string.Empty;
    }}

    private async Task RequestChanges()
    {{
        if (string.IsNullOrWhiteSpace(_comment))
        {{
            _validationMessage = ""Please specify what changes are needed."";
            return;
        }}

        _validationMessage = string.Empty;
        _processing = true;
        _currentAction = ""changes"";
        StateHasChanged();

        var request = new ApprovalActionRequest
        {{
            ApprovalId = Item.Id,
            Comment = _comment
        }};

        await OnRequestChanges.InvokeAsync(request);
        _processing = false;
        _comment = string.Empty;
    }}

    private void ToggleDelegation()
    {{
        _showDelegation = !_showDelegation;
    }}

    private async Task Delegate()
    {{
        // Delegation logic would go here
        _showDelegation = false;
        _delegateTo = string.Empty;
    }}

    public class ApprovalItem
    {{
        public Guid Id {{ get; set; }}
    }}

    public class ApprovalActionRequest
    {{
        public Guid ApprovalId {{ get; set; }}
        public string Comment {{ get; set; }} = string.Empty;
    }}

    public class DelegateUser
    {{
        public string Id {{ get; set; }} = string.Empty;
        public string Name {{ get; set; }} = string.Empty;
        public string Role {{ get; set; }} = string.Empty;
    }}
}}
";

    // ============================================================
    // ApprovalList.App.razor - List of pending approvals
    // ============================================================
    private static string GetApprovalWorkflow_ApprovalList(string name, DataObjects.EntityDefinition? entity) => $@"@* {name} - ApprovalList.App.razor *@
@* Approval Workflow Pattern - List of pending approvals *@
@page ""/approvals""
@page ""/{{TenantCode}}/approvals""
@implements IDisposable
@inject BlazorDataModel Model
@inject HttpClient Http

@if (Model.Loaded && Model.LoggedIn && Model.View == _pageName) {{
    <div class=""container-fluid"">
        <div class=""@Model.StickyMenuClass"">
            <div class=""d-flex justify-content-between align-items-center"">
                <h1 class=""page-title mb-0"">
                    <i class=""fa-solid fa-stamp me-2""></i>Approvals
                </h1>
                <div class=""d-flex gap-2"">
                    <span class=""badge bg-primary fs-6"">
                        <i class=""fa-solid fa-clock me-1""></i>@_pendingCount Pending
                    </span>
                </div>
            </div>
        </div>

        <!-- Filter Tabs -->
        <ul class=""nav nav-tabs mt-3"">
            <li class=""nav-item"">
                <button class=""nav-link @(_filter.Status == ""pending"" ? ""active"" : """")""
                        @onclick=""@(() => SetStatusFilter(""pending""))"">
                    <i class=""fa-solid fa-clock me-1""></i>Pending
                    @if (_pendingCount > 0)
                    {{
                        <span class=""badge bg-warning text-dark ms-1"">@_pendingCount</span>
                    }}
                </button>
            </li>
            <li class=""nav-item"">
                <button class=""nav-link @(_filter.Status == ""my-action"" ? ""active"" : """")""
                        @onclick=""@(() => SetStatusFilter(""my-action""))"">
                    <i class=""fa-solid fa-user-check me-1""></i>Needs My Action
                    @if (_myActionCount > 0)
                    {{
                        <span class=""badge bg-danger ms-1"">@_myActionCount</span>
                    }}
                </button>
            </li>
            <li class=""nav-item"">
                <button class=""nav-link @(_filter.Status == ""approved"" ? ""active"" : """")""
                        @onclick=""@(() => SetStatusFilter(""approved""))"">
                    <i class=""fa-solid fa-check-circle me-1""></i>Approved
                </button>
            </li>
            <li class=""nav-item"">
                <button class=""nav-link @(_filter.Status == ""rejected"" ? ""active"" : """")""
                        @onclick=""@(() => SetStatusFilter(""rejected""))"">
                    <i class=""fa-solid fa-times-circle me-1""></i>Rejected
                </button>
            </li>
            <li class=""nav-item"">
                <button class=""nav-link @(_filter.Status == ""all"" ? ""active"" : """")""
                        @onclick=""@(() => SetStatusFilter(""all""))"">
                    <i class=""fa-solid fa-list me-1""></i>All
                </button>
            </li>
        </ul>

        <!-- Filter Bar -->
        <div class=""card shadow-sm mt-3"">
            <div class=""card-body py-2"">
                <div class=""row g-2 align-items-center"">
                    <div class=""col-md-3"">
                        <div class=""input-group input-group-sm"">
                            <span class=""input-group-text""><i class=""fa-solid fa-search""></i></span>
                            <input type=""text"" class=""form-control"" placeholder=""Search approvals...""
                                   @bind=""_filter.Search"" @bind:event=""oninput"" @bind:after=""LoadData"" />
                        </div>
                    </div>
                    <div class=""col-md-2"">
                        <select class=""form-select form-select-sm"" @bind=""_filter.RequestType"" @bind:after=""LoadData"">
                            <option value="""">All Types</option>
                            <option value=""Purchase"">Purchase Request</option>
                            <option value=""Leave"">Leave Request</option>
                            <option value=""Expense"">Expense Report</option>
                            <option value=""Document"">Document Review</option>
                        </select>
                    </div>
                    <div class=""col-md-2"">
                        <select class=""form-select form-select-sm"" @bind=""_filter.Priority"" @bind:after=""LoadData"">
                            <option value="""">All Priorities</option>
                            <option value=""High"">High</option>
                            <option value=""Medium"">Medium</option>
                            <option value=""Low"">Low</option>
                        </select>
                    </div>
                    <div class=""col-md-2"">
                        <select class=""form-select form-select-sm"" @bind=""_filter.SortBy"" @bind:after=""LoadData"">
                            <option value=""newest"">Newest First</option>
                            <option value=""oldest"">Oldest First</option>
                            <option value=""priority"">By Priority</option>
                            <option value=""duedate"">By Due Date</option>
                        </select>
                    </div>
                </div>
            </div>
        </div>

        <!-- Approvals List -->
        @if (_loading) {{
            <div class=""text-center py-5"">
                <i class=""fa-solid fa-spinner fa-spin fa-2x""></i>
                <p class=""mt-2 text-muted"">Loading approvals...</p>
            </div>
        }} else if (_result.Records.Count == 0) {{
            <div class=""text-center py-5 text-muted"">
                <i class=""fa-solid fa-inbox fa-4x mb-3 opacity-50""></i>
                <h4>No approvals found</h4>
                <p>@GetEmptyMessage()</p>
            </div>
        }} else {{
            <div class=""mt-3"">
                @foreach (var item in _result.Records)
                {{
                    <div class=""card shadow-sm mb-3 @(item.RequiresMyAction ? ""border-start border-primary border-4"" : """")"">
                        <div class=""card-body"">
                            <div class=""row align-items-center"">
                                <div class=""col-md-6"">
                                    <div class=""d-flex align-items-start"">
                                        <div class=""me-3"">
                                            <div class=""rounded-circle d-flex align-items-center justify-content-center @GetTypeClass(item.RequestType)""
                                                 style=""width: 48px; height: 48px;"">
                                                <i class=""fa-solid @GetTypeIcon(item.RequestType) fa-lg""></i>
                                            </div>
                                        </div>
                                        <div>
                                            <h5 class=""mb-1"">
                                                <a href=""/approvals/@item.Id"" class=""text-decoration-none"">@item.Title</a>
                                            </h5>
                                            <div class=""text-muted small"">
                                                <span class=""me-3"">
                                                    <i class=""fa-solid fa-user me-1""></i>@item.SubmittedBy
                                                </span>
                                                <span class=""me-3"">
                                                    <i class=""fa-regular fa-clock me-1""></i>@item.SubmittedAt.ToString(""MMM dd, yyyy"")
                                                </span>
                                                @if (item.Amount.HasValue)
                                                {{
                                                    <span class=""text-primary fw-semibold"">
                                                        <i class=""fa-solid fa-dollar-sign me-1""></i>@item.Amount?.ToString(""N2"")
                                                    </span>
                                                }}
                                            </div>
                                        </div>
                                    </div>
                                </div>
                                <div class=""col-md-3"">
                                    <div class=""d-flex flex-column"">
                                        <div class=""mb-1"">
                                            <span class=""badge @GetStatusBadgeClass(item.Status)"">
                                                <i class=""fa-solid @GetStatusIcon(item.Status) me-1""></i>@item.Status
                                            </span>
                                            <span class=""badge @GetPriorityBadgeClass(item.Priority) ms-1"">@item.Priority</span>
                                        </div>
                                        <div class=""small text-muted"">
                                            Step @item.CurrentStep of @item.TotalSteps
                                            <div class=""progress mt-1"" style=""height: 4px;"">
                                                <div class=""progress-bar"" style=""width: @(item.CurrentStep * 100 / item.TotalSteps)%""></div>
                                            </div>
                                        </div>
                                    </div>
                                </div>
                                <div class=""col-md-3 text-end"">
                                    @if (item.RequiresMyAction)
                                    {{
                                        <span class=""badge bg-danger mb-2 d-block"">
                                            <i class=""fa-solid fa-exclamation-circle me-1""></i>Action Required
                                        </span>
                                    }}
                                    <a href=""/approvals/@item.Id"" class=""btn btn-outline-primary btn-sm"">
                                        <i class=""fa-solid fa-eye me-1""></i>View Details
                                    </a>
                                </div>
                            </div>
                        </div>
                    </div>
                }}
            </div>

            <!-- Pagination -->
            @if (_result.TotalPages > 1)
            {{
                <nav class=""mt-4"">
                    <ul class=""pagination justify-content-center"">
                        <li class=""page-item @(_filter.Page == 1 ? ""disabled"" : """")"">
                            <button class=""page-link"" @onclick=""@(() => GoToPage(_filter.Page - 1))"">
                                <i class=""fa-solid fa-chevron-left""></i>
                            </button>
                        </li>
                        @for (var i = 1; i <= _result.TotalPages; i++)
                        {{
                            var pageNum = i;
                            <li class=""page-item @(_filter.Page == pageNum ? ""active"" : """")"">
                                <button class=""page-link"" @onclick=""@(() => GoToPage(pageNum))"">@pageNum</button>
                            </li>
                        }}
                        <li class=""page-item @(_filter.Page == _result.TotalPages ? ""disabled"" : """")"">
                            <button class=""page-link"" @onclick=""@(() => GoToPage(_filter.Page + 1))"">
                                <i class=""fa-solid fa-chevron-right""></i>
                            </button>
                        </li>
                    </ul>
                </nav>
            }}
        }}
    </div>
}}

@code {{
    private string _pageName = ""approvals"";
    private bool _loading = true;
    private ApprovalFilter _filter = new();
    private ApprovalListResult _result = new();
    private int _pendingCount = 0;
    private int _myActionCount = 0;

    public void Dispose() {{ Model.OnChange -= StateHasChanged; }}

    protected override void OnInitialized()
    {{
        Model.View = _pageName;
        Model.OnChange += StateHasChanged;
    }}

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {{
        if (firstRender) await LoadData();
    }}

    private async Task LoadData()
    {{
        _loading = true;
        StateHasChanged();

        _result = await Helpers.GetOrPost<ApprovalListResult>(
            ""api/approvals/list"", _filter) ?? new();

        _pendingCount = _result.PendingCount;
        _myActionCount = _result.MyActionCount;

        _loading = false;
        StateHasChanged();
    }}

    private async Task SetStatusFilter(string status)
    {{
        _filter.Status = status;
        _filter.Page = 1;
        await LoadData();
    }}

    private async Task GoToPage(int page)
    {{
        if (page >= 1 && page <= _result.TotalPages)
        {{
            _filter.Page = page;
            await LoadData();
        }}
    }}

    private string GetEmptyMessage() => _filter.Status switch
    {{
        ""pending"" => ""No pending approvals at this time."",
        ""my-action"" => ""You have no approvals requiring your action."",
        ""approved"" => ""No approved requests found."",
        ""rejected"" => ""No rejected requests found."",
        _ => ""No approvals match your filter criteria.""
    }};

    private string GetStatusBadgeClass(string status) => status switch
    {{
        ""Pending"" => ""bg-warning text-dark"",
        ""Approved"" => ""bg-success"",
        ""Rejected"" => ""bg-danger"",
        ""Changes Requested"" => ""bg-info"",
        _ => ""bg-secondary""
    }};

    private string GetStatusIcon(string status) => status switch
    {{
        ""Pending"" => ""fa-clock"",
        ""Approved"" => ""fa-check-circle"",
        ""Rejected"" => ""fa-times-circle"",
        ""Changes Requested"" => ""fa-edit"",
        _ => ""fa-question-circle""
    }};

    private string GetPriorityBadgeClass(string priority) => priority switch
    {{
        ""High"" => ""bg-danger"",
        ""Medium"" => ""bg-warning text-dark"",
        ""Low"" => ""bg-info"",
        _ => ""bg-secondary""
    }};

    private string GetTypeClass(string type) => type switch
    {{
        ""Purchase"" => ""bg-primary-subtle text-primary"",
        ""Leave"" => ""bg-success-subtle text-success"",
        ""Expense"" => ""bg-warning-subtle text-warning"",
        ""Document"" => ""bg-info-subtle text-info"",
        _ => ""bg-secondary-subtle text-secondary""
    }};

    private string GetTypeIcon(string type) => type switch
    {{
        ""Purchase"" => ""fa-shopping-cart"",
        ""Leave"" => ""fa-calendar-days"",
        ""Expense"" => ""fa-receipt"",
        ""Document"" => ""fa-file-alt"",
        _ => ""fa-file""
    }};

    public class ApprovalFilter
    {{
        public string Status {{ get; set; }} = ""pending"";
        public string Search {{ get; set; }} = string.Empty;
        public string RequestType {{ get; set; }} = string.Empty;
        public string Priority {{ get; set; }} = string.Empty;
        public string SortBy {{ get; set; }} = ""newest"";
        public int Page {{ get; set; }} = 1;
        public int PageSize {{ get; set; }} = 10;
    }}

    public class ApprovalListResult
    {{
        public List<ApprovalListItem> Records {{ get; set; }} = new();
        public int TotalCount {{ get; set; }}
        public int TotalPages {{ get; set; }}
        public int PendingCount {{ get; set; }}
        public int MyActionCount {{ get; set; }}
    }}

    public class ApprovalListItem
    {{
        public Guid Id {{ get; set; }}
        public string Title {{ get; set; }} = string.Empty;
        public string RequestType {{ get; set; }} = string.Empty;
        public string Status {{ get; set; }} = ""Pending"";
        public string Priority {{ get; set; }} = ""Medium"";
        public string SubmittedBy {{ get; set; }} = string.Empty;
        public DateTime SubmittedAt {{ get; set; }}
        public decimal? Amount {{ get; set; }}
        public int CurrentStep {{ get; set; }}
        public int TotalSteps {{ get; set; }}
        public bool RequiresMyAction {{ get; set; }}
    }}
}}
";
}

#endregion
