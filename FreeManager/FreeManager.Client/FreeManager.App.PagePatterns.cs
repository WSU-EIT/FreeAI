namespace FreeManager.Client;

// ---- PagePatterns ----

#region PagePatterns - Coordinator
// ============================================================================
// PAGE PATTERN TEMPLATES - Router for page pattern code generation.
// ============================================================================

public static partial class PagePatterns
{
    public static List<(string FileName, string FileType, string Content)> GetPatternFiles(
        DataObjects.FMPagePattern pattern, string projectName, DataObjects.EntityDefinition? entity = null){
        return pattern switch
        {
            DataObjects.FMPagePattern.QuickStartWizard => GetQuickStartWizardFiles(projectName, entity),
            DataObjects.FMPagePattern.SplitPanelMasterDetail => GetSplitPanelMasterDetailFiles(projectName, entity),
            DataObjects.FMPagePattern.KanbanBoard => GetKanbanBoardFiles(projectName, entity),
            DataObjects.FMPagePattern.TimelineActivityFeed => GetTimelineActivityFeedFiles(projectName, entity),
            DataObjects.FMPagePattern.SettingsHub => GetSettingsHubFiles(projectName, entity),
            DataObjects.FMPagePattern.DataImportWizard => GetDataImportWizardFiles(projectName, entity),
            DataObjects.FMPagePattern.ComparisonView => GetComparisonViewFiles(projectName, entity),
            DataObjects.FMPagePattern.CustomizableDashboard => GetCustomizableDashboardFiles(projectName, entity),
            DataObjects.FMPagePattern.ApprovalWorkflow => GetApprovalWorkflowFiles(projectName, entity),
            DataObjects.FMPagePattern.ApiDocumentation => GetApiDocumentationFiles(projectName, entity),
            _ => new()
        };
    }
}
#endregion

// ---- PagePatterns.ApiDocumentation ----

#region PagePatterns - ApiDocumentation
// ============================================================================
// PAGE PATTERN: API_DOCUMENTATION
// Interactive API documentation with try-it-now functionality.
// Use Case: Developer portal, API reference, endpoint explorer
//
// Generated Files:
// - {name}ApiExplorer.App.razor (main container with split view)
// - {name}EndpointTree.App.razor (left panel with endpoint navigation)
// - {name}EndpointDetail.App.razor (right panel with endpoint info)
// - {name}TryItPanel.App.razor (interactive request testing)
// - {name}ResponseViewer.App.razor (JSON response display with formatting)
// ============================================================================

public static partial class PagePatterns
{
    /// <summary>
    /// Generate ApiDocumentation files for the given project.
    /// </summary>
    public static List<(string FileName, string FileType, string Content)> GetApiDocumentationFiles(
        string projectName,
        DataObjects.EntityDefinition? entity = null){
        List<(string, string, string)> files = new();

        files.Add(($"{projectName}.App.ApiExplorer.razor", "RazorComponent", GetApiDocumentation_ApiExplorer(projectName, entity)));
        files.Add(($"{projectName}.App.EndpointTree.razor", "RazorComponent", GetApiDocumentation_EndpointTree(projectName, entity)));
        files.Add(($"{projectName}.App.EndpointDetail.razor", "RazorComponent", GetApiDocumentation_EndpointDetail(projectName, entity)));
        files.Add(($"{projectName}.App.TryItPanel.razor", "RazorComponent", GetApiDocumentation_TryItPanel(projectName, entity)));
        files.Add(($"{projectName}.App.ResponseViewer.razor", "RazorComponent", GetApiDocumentation_ResponseViewer(projectName, entity)));

        return files;
    }

    // ============================================================
    // API EXPLORER - Main Container
    // ============================================================

    private static string GetApiDocumentation_ApiExplorer(string name, DataObjects.EntityDefinition? entity) => $@"@* {name} - ApiExplorer.razor *@
@* API Documentation Explorer - Main container with split view *@
@page ""/api-docs""
@page ""/{{TenantCode}}/api-docs""
@implements IDisposable
@inject BlazorDataModel Model
@inject HttpClient Http

@if (Model.Loaded && Model.LoggedIn) {{
    <div class=""container-fluid h-100"">
        <div class=""@Model.StickyMenuClass"">
            <div class=""d-flex justify-content-between align-items-center"">
                <h1 class=""page-title mb-0"">
                    <i class=""fa-solid fa-plug me-2""></i>API Documentation
                </h1>
                <div class=""d-flex gap-2"">
                    <div class=""input-group input-group-sm"" style=""width: 250px;"">
                        <span class=""input-group-text""><i class=""fa-solid fa-search""></i></span>
                        <input type=""text"" class=""form-control"" placeholder=""Search endpoints...""
                               @bind=""_searchTerm"" @bind:event=""oninput"" />
                    </div>
                    <button class=""btn btn-outline-secondary btn-sm"" @onclick=""ExpandAll"">
                        <i class=""fa-solid fa-expand me-1""></i>Expand All
                    </button>
                    <button class=""btn btn-outline-secondary btn-sm"" @onclick=""CollapseAll"">
                        <i class=""fa-solid fa-compress me-1""></i>Collapse All
                    </button>
                </div>
            </div>
        </div>

        <div class=""row mt-3"" style=""height: calc(100vh - 150px);"">
            @* Left Panel - Endpoint Tree *@
            <div class=""col-md-4 h-100"">
                <div class=""card shadow-sm h-100"">
                    <div class=""card-header bg-light py-2"">
                        <strong><i class=""fa-solid fa-sitemap me-2""></i>Endpoints</strong>
                        <span class=""badge bg-primary ms-2"">@_endpoints.Count</span>
                    </div>
                    <div class=""card-body p-0 overflow-auto"">
                        <{name}_App_EndpointTree
                            Endpoints=""@_filteredEndpoints""
                            SelectedEndpoint=""@_selectedEndpoint""
                            OnSelect=""SelectEndpoint""
                            ExpandedControllers=""@_expandedControllers"" />
                    </div>
                </div>
            </div>

            @* Right Panel - Endpoint Details *@
            <div class=""col-md-8 h-100"">
                @if (_selectedEndpoint != null) {{
                    <div class=""card shadow-sm h-100"">
                        <div class=""card-body p-0 overflow-auto"">
                            <{name}_App_EndpointDetail
                                Endpoint=""@_selectedEndpoint""
                                OnTryIt=""OpenTryIt"" />
                        </div>
                    </div>
                }} else {{
                    <div class=""card shadow-sm h-100"">
                        <div class=""card-body d-flex align-items-center justify-content-center text-muted"">
                            <div class=""text-center"">
                                <i class=""fa-solid fa-hand-pointer fa-4x mb-3 opacity-50""></i>
                                <h4>Select an endpoint</h4>
                                <p>Choose an endpoint from the tree to view its documentation</p>
                            </div>
                        </div>
                    </div>
                }}
            </div>
        </div>
    </div>

    @* Try It Modal *@
    @if (_showTryIt && _selectedEndpoint != null) {{
        <{name}_App_TryItPanel
            Endpoint=""@_selectedEndpoint""
            OnClose=""CloseTryIt""
            OnResponse=""HandleResponse"" />
    }}

    @* Response Viewer Modal *@
    @if (_showResponse) {{
        <{name}_App_ResponseViewer
            Response=""@_lastResponse""
            StatusCode=""@_lastStatusCode""
            ResponseTime=""@_lastResponseTime""
            OnClose=""CloseResponse"" />
    }}
}}

@code {{
    private string _searchTerm = string.Empty;
    private List<ApiEndpointInfo> _endpoints = new();
    private List<ApiEndpointInfo> _filteredEndpoints => FilterEndpoints();
    private ApiEndpointInfo? _selectedEndpoint;
    private HashSet<string> _expandedControllers = new();
    private bool _showTryIt = false;
    private bool _showResponse = false;
    private string _lastResponse = string.Empty;
    private int _lastStatusCode = 0;
    private long _lastResponseTime = 0;

    public void Dispose() {{ Model.OnChange -= StateHasChanged; }}

    protected override void OnInitialized()
    {{
        Model.View = ""api-docs"";
        Model.OnChange += StateHasChanged;
        LoadEndpoints();
    }}

    private void LoadEndpoints()
    {{
        // Sample endpoints for demonstration
        _endpoints = new List<ApiEndpointInfo>
        {{
            new() {{ Controller = ""Auth"", Method = ""POST"", Path = ""/api/auth/login"", Summary = ""Authenticate user and get token"", RequiresAuth = false }},
            new() {{ Controller = ""Auth"", Method = ""POST"", Path = ""/api/auth/refresh"", Summary = ""Refresh authentication token"", RequiresAuth = true }},
            new() {{ Controller = ""Auth"", Method = ""POST"", Path = ""/api/auth/logout"", Summary = ""Invalidate current session"", RequiresAuth = true }},
            new() {{ Controller = ""Users"", Method = ""GET"", Path = ""/api/users"", Summary = ""Get all users"", RequiresAuth = true }},
            new() {{ Controller = ""Users"", Method = ""GET"", Path = ""/api/users/{{id}}"", Summary = ""Get user by ID"", RequiresAuth = true }},
            new() {{ Controller = ""Users"", Method = ""POST"", Path = ""/api/users"", Summary = ""Create new user"", RequiresAuth = true }},
            new() {{ Controller = ""Users"", Method = ""PUT"", Path = ""/api/users/{{id}}"", Summary = ""Update user"", RequiresAuth = true }},
            new() {{ Controller = ""Users"", Method = ""DELETE"", Path = ""/api/users/{{id}}"", Summary = ""Delete user"", RequiresAuth = true }},
            new() {{ Controller = ""Data"", Method = ""GET"", Path = ""/api/data/items"", Summary = ""Get all items"", RequiresAuth = true }},
            new() {{ Controller = ""Data"", Method = ""POST"", Path = ""/api/data/items"", Summary = ""Create item"", RequiresAuth = true }}
        }};

        // Expand first controller by default
        if (_endpoints.Any())
        {{
            _expandedControllers.Add(_endpoints.First().Controller);
        }}
    }}

    private List<ApiEndpointInfo> FilterEndpoints()
    {{
        if (string.IsNullOrWhiteSpace(_searchTerm))
            return _endpoints;

        var term = _searchTerm.ToLower();
        return _endpoints.Where(e =>
            e.Path.ToLower().Contains(term) ||
            e.Summary.ToLower().Contains(term) ||
            e.Controller.ToLower().Contains(term) ||
            e.Method.ToLower().Contains(term)
        ).ToList();
    }}

    private void SelectEndpoint(ApiEndpointInfo endpoint)
    {{
        _selectedEndpoint = endpoint;
    }}

    private void ExpandAll()
    {{
        _expandedControllers = _endpoints.Select(e => e.Controller).Distinct().ToHashSet();
    }}

    private void CollapseAll()
    {{
        _expandedControllers.Clear();
    }}

    private void OpenTryIt()
    {{
        _showTryIt = true;
    }}

    private void CloseTryIt()
    {{
        _showTryIt = false;
    }}

    private void HandleResponse(string response, int statusCode, long responseTime)
    {{
        _lastResponse = response;
        _lastStatusCode = statusCode;
        _lastResponseTime = responseTime;
        _showTryIt = false;
        _showResponse = true;
    }}

    private void CloseResponse()
    {{
        _showResponse = false;
    }}

    // DTO for endpoint information
    public class ApiEndpointInfo
    {{
        public string Controller {{ get; set; }} = string.Empty;
        public string Method {{ get; set; }} = ""GET"";
        public string Path {{ get; set; }} = string.Empty;
        public string Summary {{ get; set; }} = string.Empty;
        public bool RequiresAuth {{ get; set; }} = true;
        public List<ApiParameterInfo> Parameters {{ get; set; }} = new();
        public string? RequestBodySchema {{ get; set; }}
        public string? ResponseSchema {{ get; set; }}
    }}

    public class ApiParameterInfo
    {{
        public string Name {{ get; set; }} = string.Empty;
        public string Type {{ get; set; }} = ""string"";
        public string Location {{ get; set; }} = ""query""; // query, path, header
        public bool Required {{ get; set; }} = false;
        public string? Description {{ get; set; }}
    }}
}}
";

    // ============================================================
    // ENDPOINT DETAIL - Right Detail Panel
    // ============================================================

    private static string GetApiDocumentation_EndpointTree(string name, DataObjects.EntityDefinition? entity) => $@"@* {name} - EndpointTree.razor *@
@* API Endpoint Tree - Grouped navigation by controller *@

@typeparam TEndpoint

<div class=""endpoint-tree"">
    @foreach (var group in GroupedEndpoints)
    {{
        <div class=""controller-group"">
            <div class=""controller-header px-3 py-2 bg-light border-bottom d-flex align-items-center cursor-pointer""
                 @onclick=""@(() => ToggleController(group.Key))"">
                <i class=""fa-solid @(IsExpanded(group.Key) ? ""fa-chevron-down"" : ""fa-chevron-right"") me-2 small""></i>
                <strong>@group.Key</strong>
                <span class=""badge bg-secondary ms-auto"">@group.Count()</span>
            </div>

            @if (IsExpanded(group.Key))
            {{
                <div class=""endpoint-list"">
                    @foreach (var endpoint in group)
                    {{
                        <div class=""endpoint-item px-3 py-2 border-bottom d-flex align-items-center @(IsSelected(endpoint) ? ""bg-primary-subtle"" : """")""
                             style=""cursor: pointer;""
                             @onclick=""@(() => SelectEndpoint(endpoint))"">
                            <span class=""badge @GetMethodBadgeClass(GetMethod(endpoint)) me-2"" style=""width: 60px;"">
                                @GetMethod(endpoint)
                            </span>
                            <span class=""text-truncate small"" title=""@GetPath(endpoint)"">@GetPath(endpoint)</span>
                            @if (GetRequiresAuth(endpoint))
                            {{
                                <i class=""fa-solid fa-lock ms-auto text-muted small"" title=""Requires authentication""></i>
                            }}
                        </div>
                    }}
                </div>
            }}
        </div>
    }}

    @if (!GroupedEndpoints.Any())
    {{
        <div class=""text-center text-muted py-4"">
            <i class=""fa-solid fa-search fa-2x mb-2 opacity-50""></i>
            <p class=""mb-0"">No endpoints found</p>
        </div>
    }}
</div>

<style>
    .endpoint-tree .controller-header:hover {{
        background-color: #e9ecef !important;
    }}
    .endpoint-tree .endpoint-item:hover {{
        background-color: #f8f9fa;
    }}
    .cursor-pointer {{
        cursor: pointer;
    }}
</style>

@code {{
    [Parameter] public List<TEndpoint> Endpoints {{ get; set; }} = new();
    [Parameter] public TEndpoint? SelectedEndpoint {{ get; set; }}
    [Parameter] public EventCallback<TEndpoint> OnSelect {{ get; set; }}
    [Parameter] public HashSet<string> ExpandedControllers {{ get; set; }} = new();

    private IEnumerable<IGrouping<string, TEndpoint>> GroupedEndpoints =>
        Endpoints.GroupBy(e => GetController(e)).OrderBy(g => g.Key);

    private bool IsExpanded(string controller) => ExpandedControllers.Contains(controller);

    private bool IsSelected(TEndpoint endpoint) =>
        SelectedEndpoint != null && GetPath(SelectedEndpoint).Equals(GetPath(endpoint));

    private void ToggleController(string controller)
    {{
        if (ExpandedControllers.Contains(controller))
            ExpandedControllers.Remove(controller);
        else
            ExpandedControllers.Add(controller);
    }}

    private async Task SelectEndpoint(TEndpoint endpoint)
    {{
        await OnSelect.InvokeAsync(endpoint);
    }}

    // Reflection helpers to access endpoint properties
    private string GetController(TEndpoint endpoint) =>
        endpoint?.GetType().GetProperty(""Controller"")?.GetValue(endpoint)?.ToString() ?? ""Unknown"";

    private string GetMethod(TEndpoint endpoint) =>
        endpoint?.GetType().GetProperty(""Method"")?.GetValue(endpoint)?.ToString() ?? ""GET"";

    private string GetPath(TEndpoint endpoint) =>
        endpoint?.GetType().GetProperty(""Path"")?.GetValue(endpoint)?.ToString() ?? """";

    private bool GetRequiresAuth(TEndpoint endpoint) =>
        (bool)(endpoint?.GetType().GetProperty(""RequiresAuth"")?.GetValue(endpoint) ?? true);

    private string GetMethodBadgeClass(string method) => method.ToUpper() switch
    {{
        ""GET"" => ""bg-success"",
        ""POST"" => ""bg-primary"",
        ""PUT"" => ""bg-warning text-dark"",
        ""PATCH"" => ""bg-info"",
        ""DELETE"" => ""bg-danger"",
        _ => ""bg-secondary""
    }};
}}
";

    // ============================================================
    // RESPONSE VIEWER - JSON Response Display
    // ============================================================
}

#endregion

// ---- PagePatterns.ApiDocumentation.EndpointDetail ----

#region PagePatterns - ApiDocumentation (EndpointDetail)
// Part of PagePatterns.ApiDocumentation: generator methods split out of the PagePatterns.ApiDocumentation section above, now merged into FreeManager.App.PagePatterns.cs (EndpointDetail, ResponseViewer).

public static partial class PagePatterns
{
    private static string GetApiDocumentation_EndpointDetail(string name, DataObjects.EntityDefinition? entity) => $@"@* {name} - EndpointDetail.razor *@
@* API Endpoint Detail - Shows full endpoint documentation *@

@typeparam TEndpoint

<div class=""endpoint-detail p-4"">
    @if (Endpoint != null)
    {{
        @* Header with method and path *@
        <div class=""d-flex align-items-center mb-4"">
            <span class=""badge @GetMethodBadgeClass(GetMethod()) fs-6 me-3"" style=""padding: 8px 16px;"">
                @GetMethod()
            </span>
            <code class=""fs-5 flex-grow-1"">@GetPath()</code>
            <button class=""btn btn-primary"" @onclick=""TryIt"">
                <i class=""fa-solid fa-play me-2""></i>Try It
            </button>
        </div>

        @* Description *@
        <div class=""mb-4"">
            <h5>Description</h5>
            <p class=""text-muted"">@GetSummary()</p>
        </div>

        @* Authentication *@
        <div class=""mb-4"">
            <h5>Authentication</h5>
            @if (GetRequiresAuth())
            {{
                <div class=""alert alert-info py-2"">
                    <i class=""fa-solid fa-lock me-2""></i>
                    This endpoint requires authentication. Include the <code>Authorization: Bearer {{token}}</code> header.
                </div>
            }}
            else
            {{
                <div class=""alert alert-success py-2"">
                    <i class=""fa-solid fa-unlock me-2""></i>
                    This endpoint is publicly accessible.
                </div>
            }}
        </div>

        @* Parameters *@
        <div class=""mb-4"">
            <h5>Parameters</h5>
            @if (GetParameters().Any())
            {{
                <div class=""table-responsive"">
                    <table class=""table table-sm"">
                        <thead class=""table-light"">
                            <tr>
                                <th>Name</th>
                                <th>Location</th>
                                <th>Type</th>
                                <th>Required</th>
                                <th>Description</th>
                            </tr>
                        </thead>
                        <tbody>
                            @foreach (var param in GetParameters())
                            {{
                                <tr>
                                    <td><code>@param.Name</code></td>
                                    <td><span class=""badge bg-secondary"">@param.Location</span></td>
                                    <td>@param.Type</td>
                                    <td>
                                        @if (param.Required)
                                        {{
                                            <i class=""fa-solid fa-check text-success""></i>
                                        }}
                                        else
                                        {{
                                            <i class=""fa-solid fa-minus text-muted""></i>
                                        }}
                                    </td>
                                    <td class=""text-muted"">@(param.Description ?? ""-"")</td>
                                </tr>
                            }}
                        </tbody>
                    </table>
                </div>
            }}
            else
            {{
                <p class=""text-muted"">No parameters required.</p>
            }}
        </div>

        @* Request Body *@
        @if (GetMethod() is ""POST"" or ""PUT"" or ""PATCH"")
        {{
            <div class=""mb-4"">
                <h5>Request Body</h5>
                <div class=""bg-dark text-light p-3 rounded"">
                    <pre class=""mb-0""><code>@(GetRequestBodySchema() ?? ""{{ }}"") </code></pre>
                </div>
            </div>
        }}

        @* Response *@
        <div class=""mb-4"">
            <h5>Response</h5>
            <div class=""bg-dark text-light p-3 rounded"">
                <pre class=""mb-0""><code>@(GetResponseSchema() ?? @""{{
  ""success"": true,
  ""data"": {{ }}
}}"")</code></pre>
            </div>
        </div>

        @* Code Examples *@
        <div class=""mb-4"">
            <h5>Code Examples</h5>
            <ul class=""nav nav-tabs"" role=""tablist"">
                <li class=""nav-item"">
                    <button class=""nav-link @(_activeTab == ""curl"" ? ""active"" : """")"" @onclick=""@(() => _activeTab = ""curl"")"">cURL</button>
                </li>
                <li class=""nav-item"">
                    <button class=""nav-link @(_activeTab == ""javascript"" ? ""active"" : """")"" @onclick=""@(() => _activeTab = ""javascript"")"">JavaScript</button>
                </li>
                <li class=""nav-item"">
                    <button class=""nav-link @(_activeTab == ""csharp"" ? ""active"" : """")"" @onclick=""@(() => _activeTab = ""csharp"")"">C#</button>
                </li>
            </ul>
            <div class=""bg-dark text-light p-3 rounded-bottom position-relative"">
                <button class=""btn btn-sm btn-outline-light position-absolute"" style=""top: 8px; right: 8px;"" @onclick=""CopyCode"">
                    <i class=""fa-solid fa-copy""></i>
                </button>
                <pre class=""mb-0""><code>@GetCodeExample()</code></pre>
            </div>
        </div>
    }}
</div>

@code {{
    [Parameter] public TEndpoint? Endpoint {{ get; set; }}
    [Parameter] public EventCallback OnTryIt {{ get; set; }}

    private string _activeTab = ""curl"";

    private async Task TryIt() => await OnTryIt.InvokeAsync();

    private async Task CopyCode()
    {{
        // Copy to clipboard functionality would go here
        await Task.CompletedTask;
    }}

    // Property accessors using reflection
    private string GetMethod() =>
        Endpoint?.GetType().GetProperty(""Method"")?.GetValue(Endpoint)?.ToString() ?? ""GET"";

    private string GetPath() =>
        Endpoint?.GetType().GetProperty(""Path"")?.GetValue(Endpoint)?.ToString() ?? """";

    private string GetSummary() =>
        Endpoint?.GetType().GetProperty(""Summary"")?.GetValue(Endpoint)?.ToString() ?? ""No description available."";

    private bool GetRequiresAuth() =>
        (bool)(Endpoint?.GetType().GetProperty(""RequiresAuth"")?.GetValue(Endpoint) ?? true);

    private string? GetRequestBodySchema() =>
        Endpoint?.GetType().GetProperty(""RequestBodySchema"")?.GetValue(Endpoint)?.ToString();

    private string? GetResponseSchema() =>
        Endpoint?.GetType().GetProperty(""ResponseSchema"")?.GetValue(Endpoint)?.ToString();

    private List<ParamInfo> GetParameters()
    {{
        var prop = Endpoint?.GetType().GetProperty(""Parameters"");
        if (prop?.GetValue(Endpoint) is System.Collections.IEnumerable list)
        {{
            var result = new List<ParamInfo>();
            foreach (var item in list)
            {{
                result.Add(new ParamInfo
                {{
                    Name = item.GetType().GetProperty(""Name"")?.GetValue(item)?.ToString() ?? """",
                    Type = item.GetType().GetProperty(""Type"")?.GetValue(item)?.ToString() ?? ""string"",
                    Location = item.GetType().GetProperty(""Location"")?.GetValue(item)?.ToString() ?? ""query"",
                    Required = (bool)(item.GetType().GetProperty(""Required"")?.GetValue(item) ?? false),
                    Description = item.GetType().GetProperty(""Description"")?.GetValue(item)?.ToString()
                }});
            }}
            return result;
        }}
        return new();
    }}

    private string GetMethodBadgeClass(string method) => method.ToUpper() switch
    {{
        ""GET"" => ""bg-success"",
        ""POST"" => ""bg-primary"",
        ""PUT"" => ""bg-warning text-dark"",
        ""PATCH"" => ""bg-info"",
        ""DELETE"" => ""bg-danger"",
        _ => ""bg-secondary""
    }};

    private string GetCodeExample() => _activeTab switch
    {{
        ""curl"" => GetCurlExample(),
        ""javascript"" => GetJsExample(),
        ""csharp"" => GetCSharpExample(),
        _ => """"
    }};

    private string GetCurlExample()
    {{
        var method = GetMethod();
        var path = GetPath();
        var auth = GetRequiresAuth() ? @""
  -H ""Authorization: Bearer YOUR_TOKEN"""" : """";

        return $@""curl -X {{method}} \
  ""https://api.example.com{{path}}"" \
  -H ""Content-Type: application/json""{{auth}}"";
    }}

    private string GetJsExample()
    {{
        var method = GetMethod();
        var path = GetPath();
        var auth = GetRequiresAuth() ? @"",
    ""Authorization"": ""Bearer YOUR_TOKEN"""" : """";

        return $@""fetch(""https://api.example.com{{path}}"", {{
  method: ""{{method}}"",
  headers: {{
    ""Content-Type"": ""application/json""{{auth}}
  }}
}})
.then(response => response.json())
.then(data => console.log(data));"";
    }}

    private string GetCSharpExample()
    {{
        var method = GetMethod();
        var path = GetPath();
        var httpMethod = method switch
        {{
            ""GET"" => ""GetAsync"",
            ""POST"" => ""PostAsync"",
            ""PUT"" => ""PutAsync"",
            ""DELETE"" => ""DeleteAsync"",
            _ => ""SendAsync""
        }};

        return $@""using var client = new HttpClient();
client.BaseAddress = new Uri(""https://api.example.com"");
client.DefaultRequestHeaders.Add(""Authorization"", ""Bearer YOUR_TOKEN"");

var response = await client.{{httpMethod}}(""{{path}}"");
var content = await response.Content.ReadAsStringAsync();"";
    }}

    private class ParamInfo
    {{
        public string Name {{ get; set; }} = """";
        public string Type {{ get; set; }} = ""string"";
        public string Location {{ get; set; }} = ""query"";
        public bool Required {{ get; set; }}
        public string? Description {{ get; set; }}
    }}
}}
";

    // ============================================================
    // ENDPOINT TREE - Left Navigation Panel
    // ============================================================

    private static string GetApiDocumentation_ResponseViewer(string name, DataObjects.EntityDefinition? entity) => $@"@* {name} - ResponseViewer.razor *@
@* API Response Viewer - Displays formatted JSON response *@

<div class=""modal fade show d-block"" tabindex=""-1"" style=""background: rgba(0,0,0,0.5);"">
    <div class=""modal-dialog modal-xl"">
        <div class=""modal-content"">
            <div class=""modal-header @GetStatusHeaderClass()"">
                <h5 class=""modal-title"">
                    <i class=""fa-solid @GetStatusIcon() me-2""></i>
                    Response: <span class=""fw-bold"">@StatusCode</span>
                    <span class=""ms-3 small"">@ResponseTime ms</span>
                </h5>
                <button type=""button"" class=""btn-close"" @onclick=""Close""></button>
            </div>
            <div class=""modal-body p-0"">
                @* Status Summary *@
                <div class=""p-3 border-bottom bg-light"">
                    <div class=""row text-center"">
                        <div class=""col"">
                            <div class=""small text-muted"">Status</div>
                            <div class=""fw-bold @GetStatusTextClass()"">@GetStatusText()</div>
                        </div>
                        <div class=""col"">
                            <div class=""small text-muted"">Time</div>
                            <div class=""fw-bold"">@ResponseTime ms</div>
                        </div>
                        <div class=""col"">
                            <div class=""small text-muted"">Size</div>
                            <div class=""fw-bold"">@GetResponseSize()</div>
                        </div>
                    </div>
                </div>

                @* Response Tabs *@
                <ul class=""nav nav-tabs px-3 pt-2"" role=""tablist"">
                    <li class=""nav-item"">
                        <button class=""nav-link @(_activeTab == ""pretty"" ? ""active"" : """")""
                                @onclick=""@(() => _activeTab = ""pretty"")"">
                            <i class=""fa-solid fa-code me-1""></i>Pretty
                        </button>
                    </li>
                    <li class=""nav-item"">
                        <button class=""nav-link @(_activeTab == ""raw"" ? ""active"" : """")""
                                @onclick=""@(() => _activeTab = ""raw"")"">
                            <i class=""fa-solid fa-file-lines me-1""></i>Raw
                        </button>
                    </li>
                    <li class=""nav-item"">
                        <button class=""nav-link @(_activeTab == ""headers"" ? ""active"" : """")""
                                @onclick=""@(() => _activeTab = ""headers"")"">
                            <i class=""fa-solid fa-list me-1""></i>Headers
                        </button>
                    </li>
                </ul>

                @* Tab Content *@
                <div class=""p-3"" style=""max-height: 60vh; overflow-y: auto;"">
                    @if (_activeTab == ""pretty"")
                    {{
                        <div class=""bg-dark text-light p-3 rounded position-relative"">
                            <button class=""btn btn-sm btn-outline-light position-absolute""
                                    style=""top: 8px; right: 8px;"" @onclick=""CopyResponse"">
                                <i class=""fa-solid fa-copy""></i>
                            </button>
                            <pre class=""mb-0"" style=""white-space: pre-wrap; word-break: break-word;""><code>@FormatJson()</code></pre>
                        </div>
                    }}
                    else if (_activeTab == ""raw"")
                    {{
                        <div class=""bg-light p-3 rounded border"">
                            <pre class=""mb-0"" style=""white-space: pre-wrap; word-break: break-word;"">@Response</pre>
                        </div>
                    }}
                    else if (_activeTab == ""headers"")
                    {{
                        <table class=""table table-sm"">
                            <thead class=""table-light"">
                                <tr>
                                    <th>Header</th>
                                    <th>Value</th>
                                </tr>
                            </thead>
                            <tbody>
                                <tr>
                                    <td><code>Content-Type</code></td>
                                    <td>application/json</td>
                                </tr>
                                <tr>
                                    <td><code>Content-Length</code></td>
                                    <td>@Response.Length</td>
                                </tr>
                            </tbody>
                        </table>
                    }}
                </div>
            </div>
            <div class=""modal-footer"">
                <button type=""button"" class=""btn btn-outline-secondary"" @onclick=""CopyResponse"">
                    <i class=""fa-solid fa-copy me-2""></i>Copy Response
                </button>
                <button type=""button"" class=""btn btn-primary"" @onclick=""Close"">Close</button>
            </div>
        </div>
    </div>
</div>

@code {{
    [Parameter] public string Response {{ get; set; }} = string.Empty;
    [Parameter] public int StatusCode {{ get; set; }}
    [Parameter] public long ResponseTime {{ get; set; }}
    [Parameter] public EventCallback OnClose {{ get; set; }}

    private string _activeTab = ""pretty"";

    private async Task Close() => await OnClose.InvokeAsync();

    private async Task CopyResponse()
    {{
        // Copy to clipboard functionality would go here
        await Task.CompletedTask;
    }}

    private string FormatJson()
    {{
        try
        {{
            var obj = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(Response);
            return System.Text.Json.JsonSerializer.Serialize(obj, new System.Text.Json.JsonSerializerOptions
            {{
                WriteIndented = true
            }});
        }}
        catch
        {{
            return Response;
        }}
    }}

    private string GetResponseSize()
    {{
        var bytes = System.Text.Encoding.UTF8.GetByteCount(Response);
        if (bytes < 1024) return $""{{bytes}} B"";
        if (bytes < 1024 * 1024) return $""{{bytes / 1024.0:F1}} KB"";
        return $""{{bytes / (1024.0 * 1024.0):F1}} MB"";
    }}

    private string GetStatusText() => StatusCode switch
    {{
        200 => ""OK"",
        201 => ""Created"",
        204 => ""No Content"",
        400 => ""Bad Request"",
        401 => ""Unauthorized"",
        403 => ""Forbidden"",
        404 => ""Not Found"",
        500 => ""Internal Server Error"",
        _ => $""HTTP {{StatusCode}}""
    }};

    private string GetStatusHeaderClass() => StatusCode switch
    {{
        >= 200 and < 300 => ""bg-success text-white"",
        >= 400 and < 500 => ""bg-warning"",
        >= 500 => ""bg-danger text-white"",
        _ => """"
    }};

    private string GetStatusTextClass() => StatusCode switch
    {{
        >= 200 and < 300 => ""text-success"",
        >= 400 and < 500 => ""text-warning"",
        >= 500 => ""text-danger"",
        _ => """"
    }};

    private string GetStatusIcon() => StatusCode switch
    {{
        >= 200 and < 300 => ""fa-check-circle"",
        >= 400 and < 500 => ""fa-exclamation-triangle"",
        >= 500 => ""fa-times-circle"",
        _ => ""fa-question-circle""
    }};
}}
";

    // ============================================================
    // TRY IT PANEL - Interactive Request Testing
    // ============================================================
}

#endregion

// ---- PagePatterns.ApiDocumentation.TryItPanel ----

#region PagePatterns - ApiDocumentation (TryItPanel)
// Part of PagePatterns.ApiDocumentation: generator methods split out of the PagePatterns.ApiDocumentation section above, now merged into FreeManager.App.PagePatterns.cs (TryItPanel).

public static partial class PagePatterns
{
    private static string GetApiDocumentation_TryItPanel(string name, DataObjects.EntityDefinition? entity) => $@"@* {name} - TryItPanel.razor *@
@* API Try It Panel - Interactive form for testing endpoints *@
@inject HttpClient Http

@typeparam TEndpoint

<div class=""modal fade show d-block"" tabindex=""-1"" style=""background: rgba(0,0,0,0.5);"">
    <div class=""modal-dialog modal-lg"">
        <div class=""modal-content"">
            <div class=""modal-header"">
                <h5 class=""modal-title"">
                    <i class=""fa-solid fa-play me-2""></i>Try It:
                    <span class=""badge @GetMethodBadgeClass(GetMethod())"">@GetMethod()</span>
                    <code class=""ms-2"">@GetPath()</code>
                </h5>
                <button type=""button"" class=""btn-close"" @onclick=""Close""></button>
            </div>
            <div class=""modal-body"">
                @* Base URL *@
                <div class=""mb-3"">
                    <label class=""form-label"">Base URL</label>
                    <input type=""text"" class=""form-control"" @bind=""_baseUrl"" />
                </div>

                @* Authentication *@
                @if (GetRequiresAuth())
                {{
                    <div class=""mb-3"">
                        <label class=""form-label"">Authorization Token</label>
                        <div class=""input-group"">
                            <span class=""input-group-text"">Bearer</span>
                            <input type=""text"" class=""form-control"" @bind=""_authToken"" placeholder=""Enter your token..."" />
                        </div>
                    </div>
                }}

                @* Path Parameters *@
                @if (GetPathParams().Any())
                {{
                    <div class=""mb-3"">
                        <label class=""form-label"">Path Parameters</label>
                        @foreach (var param in GetPathParams())
                        {{
                            <div class=""input-group mb-2"">
                                <span class=""input-group-text"" style=""width: 120px;"">@param</span>
                                <input type=""text"" class=""form-control""
                                       @bind=""_pathParams[param]""
                                       placeholder=""Enter @param..."" />
                            </div>
                        }}
                    </div>
                }}

                @* Query Parameters *@
                <div class=""mb-3"">
                    <label class=""form-label d-flex justify-content-between"">
                        Query Parameters
                        <button class=""btn btn-sm btn-outline-primary"" @onclick=""AddQueryParam"">
                            <i class=""fa-solid fa-plus""></i> Add
                        </button>
                    </label>
                    @foreach (var (key, index) in _queryParams.Select((k, i) => (k, i)))
                    {{
                        <div class=""input-group mb-2"">
                            <input type=""text"" class=""form-control"" style=""max-width: 150px;""
                                   @bind=""_queryParams[index].Key"" placeholder=""Key"" />
                            <span class=""input-group-text"">=</span>
                            <input type=""text"" class=""form-control""
                                   @bind=""_queryParams[index].Value"" placeholder=""Value"" />
                            <button class=""btn btn-outline-danger"" @onclick=""@(() => RemoveQueryParam(index))"">
                                <i class=""fa-solid fa-times""></i>
                            </button>
                        </div>
                    }}
                </div>

                @* Request Headers *@
                <div class=""mb-3"">
                    <label class=""form-label d-flex justify-content-between"">
                        Headers
                        <button class=""btn btn-sm btn-outline-primary"" @onclick=""AddHeader"">
                            <i class=""fa-solid fa-plus""></i> Add
                        </button>
                    </label>
                    @foreach (var (header, index) in _headers.Select((h, i) => (h, i)))
                    {{
                        <div class=""input-group mb-2"">
                            <input type=""text"" class=""form-control"" style=""max-width: 150px;""
                                   @bind=""_headers[index].Key"" placeholder=""Header name"" />
                            <span class=""input-group-text"">:</span>
                            <input type=""text"" class=""form-control""
                                   @bind=""_headers[index].Value"" placeholder=""Header value"" />
                            <button class=""btn btn-outline-danger"" @onclick=""@(() => RemoveHeader(index))"">
                                <i class=""fa-solid fa-times""></i>
                            </button>
                        </div>
                    }}
                </div>

                @* Request Body *@
                @if (GetMethod() is ""POST"" or ""PUT"" or ""PATCH"")
                {{
                    <div class=""mb-3"">
                        <label class=""form-label"">Request Body (JSON)</label>
                        <textarea class=""form-control font-monospace"" rows=""8""
                                  @bind=""_requestBody"" placeholder=""{{ }}""></textarea>
                    </div>
                }}

                @* Error Message *@
                @if (!string.IsNullOrEmpty(_errorMessage))
                {{
                    <div class=""alert alert-danger"">
                        <i class=""fa-solid fa-exclamation-triangle me-2""></i>@_errorMessage
                    </div>
                }}
            </div>
            <div class=""modal-footer"">
                <button type=""button"" class=""btn btn-secondary"" @onclick=""Close"">Cancel</button>
                <button type=""button"" class=""btn btn-primary"" @onclick=""SendRequest"" disabled=""@_sending"">
                    @if (_sending)
                    {{
                        <span class=""spinner-border spinner-border-sm me-2""></span>
                    }}
                    else
                    {{
                        <i class=""fa-solid fa-paper-plane me-2""></i>
                    }}
                    Send Request
                </button>
            </div>
        </div>
    </div>
</div>

@code {{
    [Parameter] public TEndpoint? Endpoint {{ get; set; }}
    [Parameter] public EventCallback OnClose {{ get; set; }}
    [Parameter] public EventCallback<(string Response, int StatusCode, long ResponseTime)> OnResponse {{ get; set; }}

    private string _baseUrl = ""https://api.example.com"";
    private string _authToken = string.Empty;
    private string _requestBody = ""{{ }}"";
    private string _errorMessage = string.Empty;
    private bool _sending = false;
    private Dictionary<string, string> _pathParams = new();
    private List<KeyValuePair> _queryParams = new();
    private List<KeyValuePair> _headers = new() {{ new() {{ Key = ""Content-Type"", Value = ""application/json"" }} }};

    protected override void OnInitialized()
    {{
        // Initialize path parameters
        foreach (var param in GetPathParams())
        {{
            _pathParams[param] = string.Empty;
        }}
    }}

    private async Task Close() => await OnClose.InvokeAsync();

    private async Task SendRequest()
    {{
        _sending = true;
        _errorMessage = string.Empty;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {{
            // Build URL with path and query params
            var path = GetPath();
            foreach (var (param, value) in _pathParams)
            {{
                path = path.Replace($""{{{{{{param}}}}}}"", Uri.EscapeDataString(value));
            }}

            var queryString = string.Join(""&"", _queryParams
                .Where(q => !string.IsNullOrEmpty(q.Key))
                .Select(q => $""{{Uri.EscapeDataString(q.Key)}}={{Uri.EscapeDataString(q.Value)}}""));

            var fullUrl = $""{{_baseUrl}}{{path}}"";
            if (!string.IsNullOrEmpty(queryString))
                fullUrl += $""?{{queryString}}"";

            using var request = new HttpRequestMessage(new HttpMethod(GetMethod()), fullUrl);

            // Add headers
            foreach (var header in _headers.Where(h => !string.IsNullOrEmpty(h.Key)))
            {{
                request.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }}

            if (GetRequiresAuth() && !string.IsNullOrEmpty(_authToken))
            {{
                request.Headers.TryAddWithoutValidation(""Authorization"", $""Bearer {{_authToken}}"");
            }}

            // Add body for POST/PUT/PATCH
            if (GetMethod() is ""POST"" or ""PUT"" or ""PATCH"" && !string.IsNullOrEmpty(_requestBody))
            {{
                request.Content = new StringContent(_requestBody, System.Text.Encoding.UTF8, ""application/json"");
            }}

            var response = await Http.SendAsync(request);
            stopwatch.Stop();

            var content = await response.Content.ReadAsStringAsync();
            await OnResponse.InvokeAsync((content, (int)response.StatusCode, stopwatch.ElapsedMilliseconds));
        }}
        catch (Exception ex)
        {{
            stopwatch.Stop();
            _errorMessage = $""Request failed: {{ex.Message}}"";
        }}
        finally
        {{
            _sending = false;
        }}
    }}

    private void AddQueryParam() => _queryParams.Add(new KeyValuePair());
    private void RemoveQueryParam(int index) => _queryParams.RemoveAt(index);
    private void AddHeader() => _headers.Add(new KeyValuePair());
    private void RemoveHeader(int index) => _headers.RemoveAt(index);

    private List<string> GetPathParams()
    {{
        var path = GetPath();
        var matches = System.Text.RegularExpressions.Regex.Matches(path, @""\{{(\w+)\}}"");
        return matches.Select(m => m.Groups[1].Value).ToList();
    }}

    // Property accessors
    private string GetMethod() =>
        Endpoint?.GetType().GetProperty(""Method"")?.GetValue(Endpoint)?.ToString() ?? ""GET"";

    private string GetPath() =>
        Endpoint?.GetType().GetProperty(""Path"")?.GetValue(Endpoint)?.ToString() ?? """";

    private bool GetRequiresAuth() =>
        (bool)(Endpoint?.GetType().GetProperty(""RequiresAuth"")?.GetValue(Endpoint) ?? true);

    private string GetMethodBadgeClass(string method) => method.ToUpper() switch
    {{
        ""GET"" => ""bg-success"",
        ""POST"" => ""bg-primary"",
        ""PUT"" => ""bg-warning text-dark"",
        ""DELETE"" => ""bg-danger"",
        _ => ""bg-secondary""
    }};

    private class KeyValuePair
    {{
        public string Key {{ get; set; }} = string.Empty;
        public string Value {{ get; set; }} = string.Empty;
    }}
}}
";
}

#endregion

// ---- PagePatterns.ApprovalWorkflow ----

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
        DataObjects.EntityDefinition? entity = null){
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
}

#endregion

// ---- PagePatterns.ApprovalWorkflow.Detail ----

#region PagePatterns - ApprovalWorkflow (Detail)
// Part of PagePatterns.ApprovalWorkflow: generator methods split out of the PagePatterns.ApprovalWorkflow section above, now merged into FreeManager.App.PagePatterns.cs (ApprovalDetail).

public static partial class PagePatterns
{
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
}

#endregion

// ---- PagePatterns.ApprovalWorkflow.List ----

#region PagePatterns - ApprovalWorkflow (List)
// Part of PagePatterns.ApprovalWorkflow: generator methods split out of the PagePatterns.ApprovalWorkflow section above, now merged into FreeManager.App.PagePatterns.cs (ApprovalList).

public static partial class PagePatterns
{
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

// ---- PagePatterns.ComparisonView ----

#region PagePatterns - ComparisonView
// ============================================================================
// PAGE PATTERN: COMPARISON VIEW
// Side-by-side comparison for versions or diffs
//
// Generated Files:
// CompareView.razor, VersionSelector.razor, SideBySide.razor, FieldDiff.razor
// ============================================================================

public static partial class PagePatterns
{
    /// <summary>
    /// Generate ComparisonView files for the given entity/project.
    /// </summary>
    public static List<(string FileName, string FileType, string Content)> GetComparisonViewFiles(
        string projectName,
        DataObjects.EntityDefinition? entity = null){
        List<(string, string, string)> files = new();
        string name = entity?.Name ?? projectName;

        files.Add(($"{projectName}.App.{name}CompareView.razor", "RazorComponent", GetComparisonView_CompareView(name, entity)));
        files.Add(($"{projectName}.App.{name}VersionSelector.razor", "RazorComponent", GetComparisonView_VersionSelector(name, entity)));
        files.Add(($"{projectName}.App.{name}SideBySide.razor", "RazorComponent", GetComparisonView_SideBySide(name, entity)));
        files.Add(($"{projectName}.App.{name}FieldDiff.razor", "RazorComponent", GetComparisonView_FieldDiff(name, entity)));

        return files;
    }

    // ============================================================
    // CompareView.razor - Main comparison container
    // ============================================================
    private static string GetComparisonView_CompareView(string name, DataObjects.EntityDefinition? entity) => $@"@* {name}CompareView.App.razor *@
@* Main comparison container for version history and diffs *@
@page ""/{name}/compare""
@page ""/{{TenantCode}}/{name}/compare""
@page ""/{name}/compare/{{ItemId:guid}}""
@page ""/{{TenantCode}}/{name}/compare/{{ItemId:guid}}""
@implements IDisposable
@inject BlazorDataModel Model
@inject HttpClient Http
@inject NavigationManager Nav

@if (Model.Loaded && Model.LoggedIn) {{
    <div class=""container-fluid"">
        <div class=""@Model.StickyMenuClass"">
            <div class=""d-flex justify-content-between align-items-center"">
                <h1 class=""page-title mb-0"">
                    <i class=""fa-solid fa-code-compare me-2""></i>{name} Version Comparison
                </h1>
                <div class=""d-flex gap-2"">
                    <button class=""btn btn-outline-secondary"" @onclick=""GoBack"">
                        <i class=""fa-solid fa-arrow-left me-1""></i>Back
                    </button>
                    @if (_canRestore && _selectedRightVersion != null) {{
                        <button class=""btn btn-warning"" @onclick=""RestoreVersion"">
                            <i class=""fa-solid fa-clock-rotate-left me-1""></i>Restore This Version
                        </button>
                    }}
                </div>
            </div>
        </div>

        @if (_loading) {{
            <div class=""text-center py-5"">
                <i class=""fa-solid fa-spinner fa-spin fa-2x""></i>
                <p class=""mt-2 text-muted"">Loading version history...</p>
            </div>
        }} else if (_versions.Count == 0) {{
            <div class=""text-center py-5 text-muted"">
                <i class=""fa-solid fa-clock-rotate-left fa-4x mb-3 opacity-50""></i>
                <h4>No version history available</h4>
                <p>Changes to this item will be tracked here.</p>
            </div>
        }} else {{
            <div class=""card shadow-sm mb-3"">
                <div class=""card-body"">
                    <{name}_App_VersionSelector
                        Versions=""_versions""
                        SelectedLeftVersion=""_selectedLeftVersion""
                        SelectedRightVersion=""_selectedRightVersion""
                        OnLeftVersionChanged=""OnLeftVersionChanged""
                        OnRightVersionChanged=""OnRightVersionChanged"" />
                </div>
            </div>

            @if (_selectedLeftVersion != null && _selectedRightVersion != null) {{
                <{name}_App_SideBySide
                    LeftVersion=""_selectedLeftVersion""
                    RightVersion=""_selectedRightVersion""
                    Differences=""_differences"" />
            }} else {{
                <div class=""alert alert-info"">
                    <i class=""fa-solid fa-info-circle me-2""></i>
                    Select two versions above to compare them.
                </div>
            }}
        }}
    </div>

    @if (_showRestoreConfirm) {{
        <div class=""modal fade show d-block"" tabindex=""-1"" style=""background: rgba(0,0,0,0.5);"">
            <div class=""modal-dialog"">
                <div class=""modal-content"">
                    <div class=""modal-header"">
                        <h5 class=""modal-title"">
                            <i class=""fa-solid fa-clock-rotate-left me-2""></i>Restore Version
                        </h5>
                        <button type=""button"" class=""btn-close"" @onclick=""CancelRestore""></button>
                    </div>
                    <div class=""modal-body"">
                        <p>Are you sure you want to restore this version?</p>
                        <p class=""text-muted small"">
                            <strong>Version:</strong> @_selectedRightVersion?.VersionNumber<br />
                            <strong>Created:</strong> @_selectedRightVersion?.CreatedAt.ToString(""MMM dd, yyyy HH:mm"")<br />
                            <strong>By:</strong> @_selectedRightVersion?.CreatedBy
                        </p>
                        <div class=""alert alert-warning mb-0"">
                            <i class=""fa-solid fa-exclamation-triangle me-2""></i>
                            This will create a new version with the restored data.
                        </div>
                    </div>
                    <div class=""modal-footer"">
                        <button type=""button"" class=""btn btn-secondary"" @onclick=""CancelRestore"">Cancel</button>
                        <button type=""button"" class=""btn btn-warning"" @onclick=""ConfirmRestore"">
                            <i class=""fa-solid fa-clock-rotate-left me-1""></i>Restore
                        </button>
                    </div>
                </div>
            </div>
        </div>
    }}
}}

@code {{
    [Parameter] public Guid? ItemId {{ get; set; }}

    private bool _loading = true;
    private bool _showRestoreConfirm = false;
    private bool _canRestore = true;
    private List<{name}Version> _versions = new();
    private {name}Version? _selectedLeftVersion;
    private {name}Version? _selectedRightVersion;
    private List<FieldDifference> _differences = new();

    public void Dispose() {{ Model.OnChange -= StateHasChanged; }}

    protected override void OnInitialized()
    {{
        Model.OnChange += StateHasChanged;
    }}

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {{
        if (firstRender) await LoadVersions();
    }}

    private async Task LoadVersions()
    {{
        _loading = true;
        StateHasChanged();

        if (ItemId.HasValue)
        {{
            // Load version history for the item
            // _versions = await Helpers.GetOrPost<List<{name}Version>>(
            //     DataObjects.Endpoints.{name}.GetVersions, ItemId.Value) ?? new();

            // Mock data for template demonstration
            _versions = new List<{name}Version>
            {{
                new() {{ VersionNumber = 3, CreatedAt = DateTime.UtcNow.AddHours(-1), CreatedBy = ""Current User"", IsCurrent = true }},
                new() {{ VersionNumber = 2, CreatedAt = DateTime.UtcNow.AddDays(-1), CreatedBy = ""Other User"" }},
                new() {{ VersionNumber = 1, CreatedAt = DateTime.UtcNow.AddDays(-7), CreatedBy = ""Current User"" }}
            }};

            if (_versions.Count >= 2)
            {{
                _selectedLeftVersion = _versions[1];
                _selectedRightVersion = _versions[0];
                await ComputeDifferences();
            }}
        }}

        _loading = false;
        StateHasChanged();
    }}

    private async Task OnLeftVersionChanged({name}Version version)
    {{
        _selectedLeftVersion = version;
        await ComputeDifferences();
    }}

    private async Task OnRightVersionChanged({name}Version version)
    {{
        _selectedRightVersion = version;
        _canRestore = !version.IsCurrent;
        await ComputeDifferences();
    }}

    private Task ComputeDifferences()
    {{
        _differences.Clear();

        if (_selectedLeftVersion != null && _selectedRightVersion != null)
        {{
            // Compare the two versions and compute field differences
            // This would typically call an API or compare locally
            // Mock data for template demonstration
            _differences = new List<FieldDifference>
            {{
                new() {{ FieldName = ""Name"", OldValue = ""Old Name"", NewValue = ""Updated Name"", ChangeType = DiffChangeType.Modified }},
                new() {{ FieldName = ""Description"", OldValue = ""Previous description"", NewValue = ""New description with more details"", ChangeType = DiffChangeType.Modified }},
                new() {{ FieldName = ""Status"", OldValue = ""Draft"", NewValue = ""Published"", ChangeType = DiffChangeType.Modified }},
                new() {{ FieldName = ""Tags"", OldValue = null, NewValue = ""important, featured"", ChangeType = DiffChangeType.Added }},
                new() {{ FieldName = ""LegacyField"", OldValue = ""old value"", NewValue = null, ChangeType = DiffChangeType.Removed }}
            }};
        }}

        StateHasChanged();
        return Task.CompletedTask;
    }}

    private void GoBack() => Nav.NavigateTo($""/{name}"");

    private void RestoreVersion() => _showRestoreConfirm = true;

    private void CancelRestore() => _showRestoreConfirm = false;

    private async Task ConfirmRestore()
    {{
        if (_selectedRightVersion != null)
        {{
            // await Helpers.GetOrPost<bool>(
            //     DataObjects.Endpoints.{name}.RestoreVersion,
            //     new {{ ItemId = ItemId, VersionNumber = _selectedRightVersion.VersionNumber }});

            _showRestoreConfirm = false;
            await LoadVersions();
        }}
    }}

    // Version model
    public class {name}Version
    {{
        public int VersionNumber {{ get; set; }}
        public DateTime CreatedAt {{ get; set; }}
        public string CreatedBy {{ get; set; }} = string.Empty;
        public bool IsCurrent {{ get; set; }}
        public Dictionary<string, object?> Data {{ get; set; }} = new();
    }}

    // Field difference model
    public class FieldDifference
    {{
        public string FieldName {{ get; set; }} = string.Empty;
        public object? OldValue {{ get; set; }}
        public object? NewValue {{ get; set; }}
        public DiffChangeType ChangeType {{ get; set; }}
    }}

    public enum DiffChangeType {{ Added, Removed, Modified, Unchanged }}
}}
";

    // ============================================================
    // FieldDiff.razor - Field-by-field difference display
    // ============================================================
    private static string GetComparisonView_FieldDiff(string name, DataObjects.EntityDefinition? entity) => $@"@* {name}FieldDiff.App.razor *@
@* Individual field difference row with change highlighting *@

<tr class=""@GetRowClass()"">
    <td>
        <strong>@Difference.FieldName</strong>
    </td>
    <td class=""text-center"">
        @switch (Difference.ChangeType)
        {{
            case {name}_App_CompareView.DiffChangeType.Added:
                <span class=""badge bg-success"" title=""Added"">
                    <i class=""fa-solid fa-plus""></i>
                </span>
                break;
            case {name}_App_CompareView.DiffChangeType.Removed:
                <span class=""badge bg-danger"" title=""Removed"">
                    <i class=""fa-solid fa-minus""></i>
                </span>
                break;
            case {name}_App_CompareView.DiffChangeType.Modified:
                <span class=""badge bg-warning text-dark"" title=""Modified"">
                    <i class=""fa-solid fa-pen""></i>
                </span>
                break;
            default:
                <span class=""badge bg-secondary"" title=""Unchanged"">
                    <i class=""fa-solid fa-equals""></i>
                </span>
                break;
        }}
    </td>
    <td>
        @if (Difference.OldValue == null)
        {{
            <span class=""diff-value-null"">(empty)</span>
        }}
        else
        {{
            <span class=""@(Difference.ChangeType == {name}_App_CompareView.DiffChangeType.Removed || Difference.ChangeType == {name}_App_CompareView.DiffChangeType.Modified ? ""diff-value-old"" : """")"">
                @FormatValue(Difference.OldValue)
            </span>
        }}
    </td>
    <td>
        @if (Difference.NewValue == null)
        {{
            <span class=""diff-value-null"">(empty)</span>
        }}
        else
        {{
            <span class=""@(Difference.ChangeType == {name}_App_CompareView.DiffChangeType.Added || Difference.ChangeType == {name}_App_CompareView.DiffChangeType.Modified ? ""diff-value-new"" : """")"">
                @FormatValue(Difference.NewValue)
            </span>
        }}
    </td>
</tr>

@code {{
    [Parameter] public {name}_App_CompareView.FieldDifference Difference {{ get; set; }} = new();

    private string GetRowClass()
    {{
        return Difference.ChangeType switch
        {{
            {name}_App_CompareView.DiffChangeType.Added => ""diff-added"",
            {name}_App_CompareView.DiffChangeType.Removed => ""diff-removed"",
            {name}_App_CompareView.DiffChangeType.Modified => ""diff-modified"",
            _ => """"
        }};
    }}

    private string FormatValue(object? value)
    {{
        if (value == null) return ""(empty)"";

        return value switch
        {{
            DateTime dt => dt.ToString(""MMM dd, yyyy HH:mm""),
            bool b => b ? ""Yes"" : ""No"",
            IEnumerable<string> list => string.Join("", "", list),
            _ => value.ToString() ?? """"
        }};
    }}
}}
";

    // ============================================================
    // SideBySide.razor - Two-column layout for old vs new
    // ============================================================
    private static string GetComparisonView_SideBySide(string name, DataObjects.EntityDefinition? entity) => $@"@* {name}SideBySide.App.razor *@
@* Side-by-side comparison layout for version differences *@

<div class=""card shadow-sm"">
    <div class=""card-header"">
        <div class=""row"">
            <div class=""col-6"">
                <h6 class=""mb-0"">
                    <i class=""fa-solid fa-arrow-left me-1 text-danger""></i>
                    Version @LeftVersion?.VersionNumber
                    <span class=""text-muted small"">(@LeftVersion?.CreatedAt.ToString(""MMM dd, yyyy""))</span>
                </h6>
            </div>
            <div class=""col-6"">
                <h6 class=""mb-0"">
                    <i class=""fa-solid fa-arrow-right me-1 text-success""></i>
                    Version @RightVersion?.VersionNumber
                    @if (RightVersion?.IsCurrent == true) {{ <span class=""badge bg-primary ms-1"">Current</span> }}
                    <span class=""text-muted small"">(@RightVersion?.CreatedAt.ToString(""MMM dd, yyyy""))</span>
                </h6>
            </div>
        </div>
    </div>
    <div class=""card-body p-0"">
        @if (Differences.Count == 0)
        {{
            <div class=""text-center py-4 text-muted"">
                <i class=""fa-solid fa-equals fa-2x mb-2""></i>
                <p>No differences found between these versions.</p>
            </div>
        }}
        else
        {{
            <div class=""comparison-summary p-3 bg-light border-bottom"">
                <span class=""badge bg-success me-2"">
                    <i class=""fa-solid fa-plus me-1""></i>@Differences.Count(d => d.ChangeType == {name}_App_CompareView.DiffChangeType.Added) Added
                </span>
                <span class=""badge bg-danger me-2"">
                    <i class=""fa-solid fa-minus me-1""></i>@Differences.Count(d => d.ChangeType == {name}_App_CompareView.DiffChangeType.Removed) Removed
                </span>
                <span class=""badge bg-warning text-dark"">
                    <i class=""fa-solid fa-pen me-1""></i>@Differences.Count(d => d.ChangeType == {name}_App_CompareView.DiffChangeType.Modified) Modified
                </span>
            </div>

            <div class=""table-responsive"">
                <table class=""table table-hover align-middle mb-0"">
                    <thead class=""table-light"">
                        <tr>
                            <th style=""width: 15%;"">Field</th>
                            <th style=""width: 5%;"">Change</th>
                            <th style=""width: 40%;"">Old Value</th>
                            <th style=""width: 40%;"">New Value</th>
                        </tr>
                    </thead>
                    <tbody>
                        @foreach (var diff in Differences)
                        {{
                            <{name}_App_FieldDiff Difference=""diff"" />
                        }}
                    </tbody>
                </table>
            </div>
        }}
    </div>
</div>

<style>
    .diff-added {{
        background-color: rgba(25, 135, 84, 0.1);
    }}
    .diff-removed {{
        background-color: rgba(220, 53, 69, 0.1);
    }}
    .diff-modified {{
        background-color: rgba(255, 193, 7, 0.1);
    }}
    .diff-value-old {{
        text-decoration: line-through;
        color: #dc3545;
    }}
    .diff-value-new {{
        color: #198754;
        font-weight: 500;
    }}
    .diff-value-null {{
        font-style: italic;
        color: #6c757d;
    }}
</style>

@code {{
    [Parameter] public {name}_App_CompareView.{name}Version? LeftVersion {{ get; set; }}
    [Parameter] public {name}_App_CompareView.{name}Version? RightVersion {{ get; set; }}
    [Parameter] public List<{name}_App_CompareView.FieldDifference> Differences {{ get; set; }} = new();
}}
";

    // ============================================================
    // VersionSelector.razor - Dropdown to select versions to compare
    // ============================================================
    private static string GetComparisonView_VersionSelector(string name, DataObjects.EntityDefinition? entity) => $@"@* {name}VersionSelector.App.razor *@
@* Dropdown selectors for choosing versions to compare *@

<div class=""row g-3 align-items-end"">
    <div class=""col-md-5"">
        <label class=""form-label fw-bold"">
            <i class=""fa-solid fa-arrow-left me-1 text-danger""></i>Left (Older)
        </label>
        <select class=""form-select"" value=""@SelectedLeftVersion?.VersionNumber"" @onchange=""OnLeftChanged"">
            <option value="""">Select a version...</option>
            @foreach (var version in Versions.Where(v => v != SelectedRightVersion))
            {{
                <option value=""@version.VersionNumber"">
                    Version @version.VersionNumber - @version.CreatedAt.ToString(""MMM dd, yyyy HH:mm"")
                    @if (version.IsCurrent) {{ <text> (Current)</text> }}
                </option>
            }}
        </select>
    </div>

    <div class=""col-md-2 text-center"">
        <button class=""btn btn-outline-secondary"" @onclick=""SwapVersions"" title=""Swap versions"">
            <i class=""fa-solid fa-arrows-left-right""></i>
        </button>
    </div>

    <div class=""col-md-5"">
        <label class=""form-label fw-bold"">
            <i class=""fa-solid fa-arrow-right me-1 text-success""></i>Right (Newer)
        </label>
        <select class=""form-select"" value=""@SelectedRightVersion?.VersionNumber"" @onchange=""OnRightChanged"">
            <option value="""">Select a version...</option>
            @foreach (var version in Versions.Where(v => v != SelectedLeftVersion))
            {{
                <option value=""@version.VersionNumber"">
                    Version @version.VersionNumber - @version.CreatedAt.ToString(""MMM dd, yyyy HH:mm"")
                    @if (version.IsCurrent) {{ <text> (Current)</text> }}
                </option>
            }}
        </select>
    </div>
</div>

@if (SelectedLeftVersion != null && SelectedRightVersion != null)
{{
    <div class=""row mt-3"">
        <div class=""col-md-5"">
            <div class=""card bg-light"">
                <div class=""card-body py-2"">
                    <small class=""text-muted"">
                        <i class=""fa-solid fa-user me-1""></i>@SelectedLeftVersion.CreatedBy
                        <br />
                        <i class=""fa-solid fa-clock me-1""></i>@SelectedLeftVersion.CreatedAt.ToString(""MMM dd, yyyy HH:mm:ss"")
                    </small>
                </div>
            </div>
        </div>
        <div class=""col-md-2""></div>
        <div class=""col-md-5"">
            <div class=""card bg-light"">
                <div class=""card-body py-2"">
                    <small class=""text-muted"">
                        <i class=""fa-solid fa-user me-1""></i>@SelectedRightVersion.CreatedBy
                        <br />
                        <i class=""fa-solid fa-clock me-1""></i>@SelectedRightVersion.CreatedAt.ToString(""MMM dd, yyyy HH:mm:ss"")
                    </small>
                </div>
            </div>
        </div>
    </div>
}}

@code {{
    [Parameter] public List<{name}_App_CompareView.{name}Version> Versions {{ get; set; }} = new();
    [Parameter] public {name}_App_CompareView.{name}Version? SelectedLeftVersion {{ get; set; }}
    [Parameter] public {name}_App_CompareView.{name}Version? SelectedRightVersion {{ get; set; }}
    [Parameter] public EventCallback<{name}_App_CompareView.{name}Version> OnLeftVersionChanged {{ get; set; }}
    [Parameter] public EventCallback<{name}_App_CompareView.{name}Version> OnRightVersionChanged {{ get; set; }}

    private async Task OnLeftChanged(ChangeEventArgs e)
    {{
        if (int.TryParse(e.Value?.ToString(), out var versionNumber))
        {{
            var version = Versions.FirstOrDefault(v => v.VersionNumber == versionNumber);
            if (version != null)
            {{
                await OnLeftVersionChanged.InvokeAsync(version);
            }}
        }}
    }}

    private async Task OnRightChanged(ChangeEventArgs e)
    {{
        if (int.TryParse(e.Value?.ToString(), out var versionNumber))
        {{
            var version = Versions.FirstOrDefault(v => v.VersionNumber == versionNumber);
            if (version != null)
            {{
                await OnRightVersionChanged.InvokeAsync(version);
            }}
        }}
    }}

    private async Task SwapVersions()
    {{
        var temp = SelectedLeftVersion;
        if (SelectedRightVersion != null)
            await OnLeftVersionChanged.InvokeAsync(SelectedRightVersion);
        if (temp != null)
            await OnRightVersionChanged.InvokeAsync(temp);
    }}
}}
";
}

#endregion

// ---- PagePatterns.CustomizableDashboard ----

#region PagePatterns - CustomizableDashboard
// ============================================================================
// PAGE PATTERN: CUSTOMIZABLE DASHBOARD
// User-configurable dashboard with drag-drop widgets.
//
// Generated Files:
// - Dashboard.razor - Main dashboard page with layout management
// - DashboardGrid.razor - Grid-based widget layout system
// - WidgetContainer.razor - Widget wrapper with drag/resize handles
// - WidgetPicker.razor - Modal for selecting widgets to add
// - StatsWidget.razor - Statistics/metrics display widget
// - ChartWidget.razor - Chart visualization widget
// ============================================================================

public static partial class PagePatterns
{
    /// <summary>
    /// Generate CustomizableDashboard files for the given project.
    /// </summary>
    public static List<(string FileName, string FileType, string Content)> GetCustomizableDashboardFiles(
        string projectName,
        DataObjects.EntityDefinition? entity = null){
        List<(string, string, string)> files = new();

        files.Add(($"{projectName}.App.Dashboard.razor", "RazorComponent", GetCustomizableDashboard_Dashboard(projectName, entity)));
        files.Add(($"{projectName}.App.DashboardGrid.razor", "RazorComponent", GetCustomizableDashboard_DashboardGrid(projectName, entity)));
        files.Add(($"{projectName}.App.WidgetContainer.razor", "RazorComponent", GetCustomizableDashboard_WidgetContainer(projectName, entity)));
        files.Add(($"{projectName}.App.WidgetPicker.razor", "RazorComponent", GetCustomizableDashboard_WidgetPicker(projectName, entity)));
        files.Add(($"{projectName}.App.StatsWidget.razor", "RazorComponent", GetCustomizableDashboard_StatsWidget(projectName, entity)));
        files.Add(($"{projectName}.App.ChartWidget.razor", "RazorComponent", GetCustomizableDashboard_ChartWidget(projectName, entity)));

        return files;
    }

    // ============================================================
    // Dashboard.razor - Main dashboard page
    // ============================================================
    private static string GetCustomizableDashboard_Dashboard(string name, DataObjects.EntityDefinition? entity) => $@"@* {name} - Dashboard.razor - Customizable Dashboard *@
@* Generated by Page Pattern Template *@
@page ""/dashboard""
@page ""/{{TenantCode}}/dashboard""
@implements IDisposable
@inject BlazorDataModel Model
@inject IJSRuntime JS

@if (Model.Loaded && Model.LoggedIn) {{
    <div class=""dashboard-container"">
        <div class=""dashboard-header d-flex justify-content-between align-items-center mb-3"">
            <h1 class=""page-title mb-0"">
                <i class=""fa-solid fa-grip me-2""></i>Dashboard
            </h1>
            <div class=""dashboard-actions"">
                @if (_editMode) {{
                    <button class=""btn btn-success me-2"" @onclick=""SaveLayout"">
                        <i class=""fa-solid fa-save me-1""></i>Save Layout
                    </button>
                    <button class=""btn btn-secondary me-2"" @onclick=""CancelEdit"">
                        <i class=""fa-solid fa-times me-1""></i>Cancel
                    </button>
                }}
                <button class=""btn @(_editMode ? ""btn-warning"" : ""btn-outline-primary"")"" @onclick=""ToggleEditMode"">
                    <i class=""fa-solid @(_editMode ? ""fa-lock"" : ""fa-edit"") me-1""></i>
                    @(_editMode ? ""Lock Dashboard"" : ""Customize"")
                </button>
                @if (_editMode) {{
                    <button class=""btn btn-primary ms-2"" @onclick=""ShowWidgetPicker"">
                        <i class=""fa-solid fa-plus me-1""></i>Add Widget
                    </button>
                }}
            </div>
        </div>

        <{name}_App_DashboardGrid @ref=""_grid""
                         Widgets=""_widgets""
                         EditMode=""_editMode""
                         OnWidgetMoved=""HandleWidgetMoved""
                         OnWidgetResized=""HandleWidgetResized""
                         OnWidgetRemoved=""HandleWidgetRemoved"" />

        @if (_showWidgetPicker) {{
            <{name}_App_WidgetPicker OnSelect=""AddWidget"" OnClose=""HideWidgetPicker"" />
        }}
    </div>
}}

<style>
    .dashboard-container {{
        padding: 1rem;
    }}

    .dashboard-header {{
        position: sticky;
        top: 0;
        background: var(--bs-body-bg);
        z-index: 100;
        padding: 1rem 0;
        border-bottom: 1px solid var(--bs-border-color);
    }}
</style>

@code {{
    private DashboardGrid_{name}? _grid;
    private List<DashboardWidget> _widgets = new();
    private List<DashboardWidget> _originalWidgets = new();
    private bool _editMode = false;
    private bool _showWidgetPicker = false;

    public void Dispose() {{ Model.OnChange -= StateHasChanged; }}

    protected override void OnInitialized()
    {{
        Model.View = ""dashboard"";
        Model.OnChange += StateHasChanged;
        LoadLayout();
    }}

    private void LoadLayout()
    {{
        // Load saved layout from user preferences or use defaults
        _widgets = GetDefaultWidgets();
        _originalWidgets = _widgets.Select(w => w.Clone()).ToList();
    }}

    private List<DashboardWidget> GetDefaultWidgets() => new()
    {{
        new DashboardWidget {{ Id = Guid.NewGuid(), Type = ""stats"", Title = ""Total Users"", Column = 0, Row = 0, Width = 3, Height = 2, Config = new() {{ [""icon""] = ""fa-users"", [""value""] = ""1,234"", [""trend""] = ""+12%"", [""color""] = ""primary"" }} }},
        new DashboardWidget {{ Id = Guid.NewGuid(), Type = ""stats"", Title = ""Revenue"", Column = 3, Row = 0, Width = 3, Height = 2, Config = new() {{ [""icon""] = ""fa-dollar-sign"", [""value""] = ""$45,678"", [""trend""] = ""+8%"", [""color""] = ""success"" }} }},
        new DashboardWidget {{ Id = Guid.NewGuid(), Type = ""stats"", Title = ""Orders"", Column = 6, Row = 0, Width = 3, Height = 2, Config = new() {{ [""icon""] = ""fa-shopping-cart"", [""value""] = ""892"", [""trend""] = ""+23%"", [""color""] = ""info"" }} }},
        new DashboardWidget {{ Id = Guid.NewGuid(), Type = ""stats"", Title = ""Pending Tasks"", Column = 9, Row = 0, Width = 3, Height = 2, Config = new() {{ [""icon""] = ""fa-tasks"", [""value""] = ""47"", [""trend""] = ""-5%"", [""color""] = ""warning"" }} }},
        new DashboardWidget {{ Id = Guid.NewGuid(), Type = ""chart"", Title = ""Sales Overview"", Column = 0, Row = 2, Width = 6, Height = 4, Config = new() {{ [""chartType""] = ""line"" }} }},
        new DashboardWidget {{ Id = Guid.NewGuid(), Type = ""chart"", Title = ""Revenue by Category"", Column = 6, Row = 2, Width = 6, Height = 4, Config = new() {{ [""chartType""] = ""bar"" }} }}
    }};

    private void ToggleEditMode()
    {{
        _editMode = !_editMode;
        if (_editMode)
        {{
            _originalWidgets = _widgets.Select(w => w.Clone()).ToList();
        }}
    }}

    private async Task SaveLayout()
    {{
        // Persist layout to user preferences
        _editMode = false;
        _originalWidgets = _widgets.Select(w => w.Clone()).ToList();
        StateHasChanged();
    }}

    private void CancelEdit()
    {{
        _widgets = _originalWidgets.Select(w => w.Clone()).ToList();
        _editMode = false;
        StateHasChanged();
    }}

    private void ShowWidgetPicker() => _showWidgetPicker = true;
    private void HideWidgetPicker() => _showWidgetPicker = false;

    private void AddWidget(string widgetType)
    {{
        var newWidget = new DashboardWidget
        {{
            Id = Guid.NewGuid(),
            Type = widgetType,
            Title = GetWidgetTitle(widgetType),
            Column = 0,
            Row = GetNextAvailableRow(),
            Width = widgetType == ""chart"" ? 6 : 3,
            Height = widgetType == ""chart"" ? 4 : 2,
            Config = GetDefaultConfig(widgetType)
        }};
        _widgets.Add(newWidget);
        _showWidgetPicker = false;
        StateHasChanged();
    }}

    private string GetWidgetTitle(string type) => type switch
    {{
        ""stats"" => ""New Statistic"",
        ""chart"" => ""New Chart"",
        _ => ""New Widget""
    }};

    private int GetNextAvailableRow()
    {{
        if (!_widgets.Any()) return 0;
        return _widgets.Max(w => w.Row + w.Height);
    }}

    private Dictionary<string, string> GetDefaultConfig(string type) => type switch
    {{
        ""stats"" => new() {{ [""icon""] = ""fa-chart-simple"", [""value""] = ""0"", [""trend""] = ""0%"", [""color""] = ""secondary"" }},
        ""chart"" => new() {{ [""chartType""] = ""line"" }},
        _ => new()
    }};

    private void HandleWidgetMoved((Guid id, int col, int row) args)
    {{
        var widget = _widgets.FirstOrDefault(w => w.Id == args.id);
        if (widget != null)
        {{
            widget.Column = args.col;
            widget.Row = args.row;
        }}
    }}

    private void HandleWidgetResized((Guid id, int width, int height) args)
    {{
        var widget = _widgets.FirstOrDefault(w => w.Id == args.id);
        if (widget != null)
        {{
            widget.Width = Math.Max(2, Math.Min(12, args.width));
            widget.Height = Math.Max(2, Math.Min(8, args.height));
        }}
    }}

    private void HandleWidgetRemoved(Guid widgetId)
    {{
        _widgets.RemoveAll(w => w.Id == widgetId);
        StateHasChanged();
    }}

    public class DashboardWidget
    {{
        public Guid Id {{ get; set; }}
        public string Type {{ get; set; }} = ""stats"";
        public string Title {{ get; set; }} = """";
        public int Column {{ get; set; }}
        public int Row {{ get; set; }}
        public int Width {{ get; set; }} = 3;
        public int Height {{ get; set; }} = 2;
        public Dictionary<string, string> Config {{ get; set; }} = new();

        public DashboardWidget Clone() => new()
        {{
            Id = Id,
            Type = Type,
            Title = Title,
            Column = Column,
            Row = Row,
            Width = Width,
            Height = Height,
            Config = new Dictionary<string, string>(Config)
        }};
    }}
}}
";

    // ============================================================
    // DashboardGrid.razor - Grid layout for widgets
    // ============================================================
    private static string GetCustomizableDashboard_DashboardGrid(string name, DataObjects.EntityDefinition? entity) => $@"@* {name} - DashboardGrid.razor - Grid-based widget layout *@
@* Generated by Page Pattern Template *@

<div class=""dashboard-grid @(_editMode ? ""edit-mode"" : """")"">
    @foreach (var widget in Widgets)
    {{
        <{name}_App_WidgetContainer Widget=""widget""
                           EditMode=""_editMode""
                           OnMoved=""HandleMoved""
                           OnResized=""HandleResized""
                           OnRemoved=""HandleRemoved"" />
    }}
</div>

<style>
    .dashboard-grid {{
        display: grid;
        grid-template-columns: repeat(12, 1fr);
        grid-auto-rows: minmax(60px, auto);
        gap: 1rem;
        min-height: 400px;
    }}

    .dashboard-grid.edit-mode {{
        background-image:
            linear-gradient(to right, rgba(0,0,0,0.05) 1px, transparent 1px),
            linear-gradient(to bottom, rgba(0,0,0,0.05) 1px, transparent 1px);
        background-size: calc(100% / 12) 60px;
        border: 2px dashed var(--bs-primary);
        border-radius: 0.5rem;
        padding: 0.5rem;
    }}
</style>

@code {{
    [Parameter] public List<Dashboard_{name}.DashboardWidget> Widgets {{ get; set; }} = new();
    [Parameter] public bool EditMode {{ get; set; }}
    [Parameter] public EventCallback<(Guid id, int col, int row)> OnWidgetMoved {{ get; set; }}
    [Parameter] public EventCallback<(Guid id, int width, int height)> OnWidgetResized {{ get; set; }}
    [Parameter] public EventCallback<Guid> OnWidgetRemoved {{ get; set; }}

    private bool _editMode => EditMode;

    private async Task HandleMoved((Guid id, int col, int row) args)
    {{
        await OnWidgetMoved.InvokeAsync(args);
    }}

    private async Task HandleResized((Guid id, int width, int height) args)
    {{
        await OnWidgetResized.InvokeAsync(args);
    }}

    private async Task HandleRemoved(Guid widgetId)
    {{
        await OnWidgetRemoved.InvokeAsync(widgetId);
    }}
}}
";

    // ============================================================
    // WidgetPicker.razor - Modal for adding widgets
    // ============================================================
    private static string GetCustomizableDashboard_WidgetPicker(string name, DataObjects.EntityDefinition? entity) => $@"@* {name} - WidgetPicker.razor - Widget selection modal *@
@* Generated by Page Pattern Template *@

<div class=""modal fade show d-block"" tabindex=""-1"" style=""background: rgba(0,0,0,0.5);"">
    <div class=""modal-dialog modal-lg"">
        <div class=""modal-content"">
            <div class=""modal-header"">
                <h5 class=""modal-title"">
                    <i class=""fa-solid fa-plus me-2""></i>Add Widget
                </h5>
                <button type=""button"" class=""btn-close"" @onclick=""Close""></button>
            </div>
            <div class=""modal-body"">
                <p class=""text-muted mb-4"">Select a widget type to add to your dashboard:</p>

                <div class=""row g-3"">
                    @foreach (var widget in _availableWidgets)
                    {{
                        <div class=""col-md-4"">
                            <div class=""card widget-option h-100"" @onclick=""() => SelectWidget(widget.Type)"">
                                <div class=""card-body text-center"">
                                    <div class=""widget-icon mb-3"">
                                        <i class=""fa-solid @widget.Icon fa-3x text-@widget.Color""></i>
                                    </div>
                                    <h5 class=""card-title"">@widget.Name</h5>
                                    <p class=""card-text text-muted small"">@widget.Description</p>
                                </div>
                            </div>
                        </div>
                    }}
                </div>
            </div>
            <div class=""modal-footer"">
                <button type=""button"" class=""btn btn-secondary"" @onclick=""Close"">Cancel</button>
            </div>
        </div>
    </div>
</div>

<style>
    .widget-option {{
        cursor: pointer;
        transition: transform 0.2s, box-shadow 0.2s;
        border: 2px solid transparent;
    }}

    .widget-option:hover {{
        transform: translateY(-4px);
        box-shadow: 0 8px 16px rgba(0,0,0,0.1);
        border-color: var(--bs-primary);
    }}

    .widget-icon {{
        width: 80px;
        height: 80px;
        margin: 0 auto;
        display: flex;
        align-items: center;
        justify-content: center;
        background: var(--bs-light);
        border-radius: 50%;
    }}
</style>

@code {{
    [Parameter] public EventCallback<string> OnSelect {{ get; set; }}
    [Parameter] public EventCallback OnClose {{ get; set; }}

    private List<WidgetOption> _availableWidgets = new()
    {{
        new WidgetOption {{ Type = ""stats"", Name = ""Statistics Card"", Description = ""Display a key metric with trend indicator"", Icon = ""fa-chart-simple"", Color = ""primary"" }},
        new WidgetOption {{ Type = ""chart"", Name = ""Chart Widget"", Description = ""Visualize data with line, bar, or pie charts"", Icon = ""fa-chart-line"", Color = ""success"" }}
    }};

    private async Task SelectWidget(string widgetType)
    {{
        await OnSelect.InvokeAsync(widgetType);
    }}

    private async Task Close()
    {{
        await OnClose.InvokeAsync();
    }}

    private class WidgetOption
    {{
        public string Type {{ get; set; }} = """";
        public string Name {{ get; set; }} = """";
        public string Description {{ get; set; }} = """";
        public string Icon {{ get; set; }} = """";
        public string Color {{ get; set; }} = ""primary"";
    }}
}}
";
}

#endregion

// ---- PagePatterns.CustomizableDashboard.Widgets ----

#region PagePatterns - CustomizableDashboard (Widgets)
// Part of PagePatterns.CustomizableDashboard: generator methods split out of the PagePatterns.CustomizableDashboard section above, now merged into FreeManager.App.PagePatterns.cs (ChartWidget, StatsWidget, WidgetContainer).

public static partial class PagePatterns
{
    // ============================================================
    // ChartWidget.razor - Chart visualization widget
    // ============================================================
    private static string GetCustomizableDashboard_ChartWidget(string name, DataObjects.EntityDefinition? entity) => $@"@* {name} - ChartWidget.razor - Chart visualization widget *@
@* Generated by Page Pattern Template *@

<div class=""chart-widget h-100 d-flex flex-column"">
    <div class=""chart-header d-flex justify-content-between align-items-center p-3 border-bottom"">
        <h6 class=""mb-0"">
            <i class=""fa-solid @_chartIcon me-2 text-primary""></i>@Title
        </h6>
        <div class=""btn-group btn-group-sm"">
            <button class=""btn @(_chartType == ""line"" ? ""btn-primary"" : ""btn-outline-primary"")"" @onclick=""@(() => SetChartType(""line""))"">
                <i class=""fa-solid fa-chart-line""></i>
            </button>
            <button class=""btn @(_chartType == ""bar"" ? ""btn-primary"" : ""btn-outline-primary"")"" @onclick=""@(() => SetChartType(""bar""))"">
                <i class=""fa-solid fa-chart-bar""></i>
            </button>
            <button class=""btn @(_chartType == ""pie"" ? ""btn-primary"" : ""btn-outline-primary"")"" @onclick=""@(() => SetChartType(""pie""))"">
                <i class=""fa-solid fa-chart-pie""></i>
            </button>
        </div>
    </div>
    <div class=""chart-body flex-grow-1 p-3"">
        <div class=""chart-placeholder d-flex flex-column align-items-center justify-content-center h-100 text-muted"">
            @if (_chartType == ""line"")
            {{
                <svg viewBox=""0 0 200 100"" class=""chart-svg"">
                    <polyline points=""10,80 40,60 70,70 100,40 130,50 160,20 190,30""
                              fill=""none"" stroke=""currentColor"" stroke-width=""2"" class=""text-primary"" />
                    <circle cx=""10"" cy=""80"" r=""4"" fill=""currentColor"" class=""text-primary"" />
                    <circle cx=""40"" cy=""60"" r=""4"" fill=""currentColor"" class=""text-primary"" />
                    <circle cx=""70"" cy=""70"" r=""4"" fill=""currentColor"" class=""text-primary"" />
                    <circle cx=""100"" cy=""40"" r=""4"" fill=""currentColor"" class=""text-primary"" />
                    <circle cx=""130"" cy=""50"" r=""4"" fill=""currentColor"" class=""text-primary"" />
                    <circle cx=""160"" cy=""20"" r=""4"" fill=""currentColor"" class=""text-primary"" />
                    <circle cx=""190"" cy=""30"" r=""4"" fill=""currentColor"" class=""text-primary"" />
                </svg>
            }}
            else if (_chartType == ""bar"")
            {{
                <svg viewBox=""0 0 200 100"" class=""chart-svg"">
                    <rect x=""10"" y=""40"" width=""25"" height=""55"" fill=""currentColor"" class=""text-primary"" opacity=""0.8"" />
                    <rect x=""45"" y=""20"" width=""25"" height=""75"" fill=""currentColor"" class=""text-success"" opacity=""0.8"" />
                    <rect x=""80"" y=""50"" width=""25"" height=""45"" fill=""currentColor"" class=""text-info"" opacity=""0.8"" />
                    <rect x=""115"" y=""10"" width=""25"" height=""85"" fill=""currentColor"" class=""text-warning"" opacity=""0.8"" />
                    <rect x=""150"" y=""30"" width=""25"" height=""65"" fill=""currentColor"" class=""text-danger"" opacity=""0.8"" />
                </svg>
            }}
            else
            {{
                <svg viewBox=""0 0 100 100"" class=""chart-svg pie"">
                    <circle cx=""50"" cy=""50"" r=""40"" fill=""transparent"" stroke=""currentColor"" stroke-width=""20""
                            stroke-dasharray=""62.8 188.4"" transform=""rotate(-90 50 50)"" class=""text-primary"" />
                    <circle cx=""50"" cy=""50"" r=""40"" fill=""transparent"" stroke=""currentColor"" stroke-width=""20""
                            stroke-dasharray=""50.24 188.4"" stroke-dashoffset=""-62.8"" transform=""rotate(-90 50 50)"" class=""text-success"" />
                    <circle cx=""50"" cy=""50"" r=""40"" fill=""transparent"" stroke=""currentColor"" stroke-width=""20""
                            stroke-dasharray=""37.68 188.4"" stroke-dashoffset=""-113.04"" transform=""rotate(-90 50 50)"" class=""text-warning"" />
                    <circle cx=""50"" cy=""50"" r=""40"" fill=""transparent"" stroke=""currentColor"" stroke-width=""20""
                            stroke-dasharray=""37.68 188.4"" stroke-dashoffset=""-150.72"" transform=""rotate(-90 50 50)"" class=""text-info"" />
                </svg>
            }}
            <p class=""mt-3 small"">Sample @_chartType chart - Connect to data source</p>
        </div>
    </div>
    <div class=""chart-footer p-2 border-top d-flex justify-content-between align-items-center text-muted small"">
        <span><i class=""fa-solid fa-database me-1""></i>Sample Data</span>
        <button class=""btn btn-sm btn-link p-0"">
            <i class=""fa-solid fa-expand""></i> Expand
        </button>
    </div>
</div>

<style>
    .chart-widget {{
        background: var(--bs-body-bg);
    }}

    .chart-svg {{
        width: 100%;
        max-width: 300px;
        height: auto;
    }}

    .chart-svg.pie {{
        max-width: 150px;
    }}

    .chart-placeholder {{
        min-height: 150px;
    }}
</style>

@code {{
    [Parameter] public string Title {{ get; set; }} = ""Chart"";
    [Parameter] public Dictionary<string, string> Config {{ get; set; }} = new();

    private string _chartType => Config.GetValueOrDefault(""chartType"", ""line"");

    private string _chartIcon => _chartType switch
    {{
        ""line"" => ""fa-chart-line"",
        ""bar"" => ""fa-chart-bar"",
        ""pie"" => ""fa-chart-pie"",
        _ => ""fa-chart-simple""
    }};

    private void SetChartType(string type)
    {{
        Config[""chartType""] = type;
        StateHasChanged();
    }}
}}
";

    // ============================================================
    // StatsWidget.razor - Statistics/metrics widget
    // ============================================================
    private static string GetCustomizableDashboard_StatsWidget(string name, DataObjects.EntityDefinition? entity) => $@"@* {name} - StatsWidget.razor - Statistics display widget *@
@* Generated by Page Pattern Template *@

<div class=""stats-widget h-100"">
    <div class=""stats-card h-100 d-flex flex-column"">
        <div class=""stats-header d-flex justify-content-between align-items-start"">
            <div class=""stats-icon bg-@_color bg-opacity-10 text-@_color"">
                <i class=""fa-solid @_icon""></i>
            </div>
            <span class=""badge @(_trendPositive ? ""bg-success"" : ""bg-danger"")"">
                <i class=""fa-solid @(_trendPositive ? ""fa-arrow-up"" : ""fa-arrow-down"") me-1""></i>
                @_trend
            </span>
        </div>
        <div class=""stats-body flex-grow-1 d-flex flex-column justify-content-center"">
            <h2 class=""stats-value mb-1"">@_value</h2>
            <p class=""stats-title text-muted mb-0"">@Title</p>
        </div>
        <div class=""stats-footer text-muted small"">
            <i class=""fa-solid fa-clock me-1""></i>Updated just now
        </div>
    </div>
</div>

<style>
    .stats-widget {{
        padding: 1rem;
    }}

    .stats-card {{
        padding: 1rem;
    }}

    .stats-icon {{
        width: 48px;
        height: 48px;
        border-radius: 0.5rem;
        display: flex;
        align-items: center;
        justify-content: center;
        font-size: 1.25rem;
    }}

    .stats-value {{
        font-size: 2rem;
        font-weight: 700;
        color: var(--bs-dark);
    }}

    .stats-title {{
        font-size: 0.875rem;
        font-weight: 500;
    }}

    .stats-footer {{
        padding-top: 0.75rem;
        border-top: 1px solid var(--bs-border-color);
    }}
</style>

@code {{
    [Parameter] public string Title {{ get; set; }} = ""Statistic"";
    [Parameter] public Dictionary<string, string> Config {{ get; set; }} = new();

    private string _icon => Config.GetValueOrDefault(""icon"", ""fa-chart-simple"");
    private string _value => Config.GetValueOrDefault(""value"", ""0"");
    private string _trend => Config.GetValueOrDefault(""trend"", ""0%"");
    private string _color => Config.GetValueOrDefault(""color"", ""primary"");
    private bool _trendPositive => !_trend.StartsWith(""-"");
}}
";

    // ============================================================
    // WidgetContainer.razor - Widget wrapper with controls
    // ============================================================
    private static string GetCustomizableDashboard_WidgetContainer(string name, DataObjects.EntityDefinition? entity) => $@"@* {name} - WidgetContainer.razor - Widget container with drag/resize *@
@* Generated by Page Pattern Template *@

<div class=""widget-container @(_editMode ? ""editable"" : """")""
     style=""grid-column: span @Widget.Width; grid-row: span @Widget.Height; grid-column-start: @(Widget.Column + 1); grid-row-start: @(Widget.Row + 1);""
     draggable=""@_editMode""
     @ondragstart=""HandleDragStart""
     @ondragend=""HandleDragEnd"">

    @if (_editMode)
    {{
        <div class=""widget-controls"">
            <button class=""btn btn-sm btn-link text-muted"" @onclick=""() => MoveWidget(-1, 0)"" title=""Move Left"">
                <i class=""fa-solid fa-arrow-left""></i>
            </button>
            <button class=""btn btn-sm btn-link text-muted"" @onclick=""() => MoveWidget(1, 0)"" title=""Move Right"">
                <i class=""fa-solid fa-arrow-right""></i>
            </button>
            <button class=""btn btn-sm btn-link text-muted"" @onclick=""() => MoveWidget(0, -1)"" title=""Move Up"">
                <i class=""fa-solid fa-arrow-up""></i>
            </button>
            <button class=""btn btn-sm btn-link text-muted"" @onclick=""() => MoveWidget(0, 1)"" title=""Move Down"">
                <i class=""fa-solid fa-arrow-down""></i>
            </button>
            <span class=""mx-1"">|</span>
            <button class=""btn btn-sm btn-link text-muted"" @onclick=""() => ResizeWidget(1, 0)"" title=""Wider"">
                <i class=""fa-solid fa-arrows-left-right""></i>+
            </button>
            <button class=""btn btn-sm btn-link text-muted"" @onclick=""() => ResizeWidget(-1, 0)"" title=""Narrower"">
                <i class=""fa-solid fa-arrows-left-right""></i>-
            </button>
            <button class=""btn btn-sm btn-link text-muted"" @onclick=""() => ResizeWidget(0, 1)"" title=""Taller"">
                <i class=""fa-solid fa-arrows-up-down""></i>+
            </button>
            <button class=""btn btn-sm btn-link text-muted"" @onclick=""() => ResizeWidget(0, -1)"" title=""Shorter"">
                <i class=""fa-solid fa-arrows-up-down""></i>-
            </button>
            <span class=""mx-1"">|</span>
            <button class=""btn btn-sm btn-link text-danger"" @onclick=""RemoveWidget"" title=""Remove"">
                <i class=""fa-solid fa-trash""></i>
            </button>
        </div>
    }}

    <div class=""widget-content"">
        @switch (Widget.Type)
        {{
            case ""stats"":
                <{name}_App_StatsWidget Title=""@Widget.Title"" Config=""@Widget.Config"" />
                break;
            case ""chart"":
                <{name}_App_ChartWidget Title=""@Widget.Title"" Config=""@Widget.Config"" />
                break;
            default:
                <div class=""p-3 text-muted"">Unknown widget type: @Widget.Type</div>
                break;
        }}
    </div>

    @if (_editMode)
    {{
        <div class=""widget-resize-handle"" @onmousedown=""StartResize"">
            <i class=""fa-solid fa-grip""></i>
        </div>
    }}
</div>

<style>
    .widget-container {{
        background: var(--bs-body-bg);
        border: 1px solid var(--bs-border-color);
        border-radius: 0.5rem;
        box-shadow: 0 2px 4px rgba(0,0,0,0.05);
        overflow: hidden;
        display: flex;
        flex-direction: column;
        transition: box-shadow 0.2s, transform 0.2s;
    }}

    .widget-container.editable {{
        cursor: move;
        border: 2px dashed var(--bs-primary);
    }}

    .widget-container.editable:hover {{
        box-shadow: 0 4px 12px rgba(0,0,0,0.15);
        transform: translateY(-2px);
    }}

    .widget-controls {{
        display: flex;
        align-items: center;
        justify-content: center;
        background: var(--bs-light);
        padding: 0.25rem;
        border-bottom: 1px solid var(--bs-border-color);
        flex-wrap: wrap;
        gap: 0.25rem;
    }}

    .widget-controls .btn {{
        padding: 0.125rem 0.375rem;
        font-size: 0.75rem;
    }}

    .widget-content {{
        flex: 1;
        overflow: auto;
    }}

    .widget-resize-handle {{
        position: absolute;
        bottom: 0;
        right: 0;
        width: 24px;
        height: 24px;
        background: var(--bs-light);
        border-top-left-radius: 0.5rem;
        display: flex;
        align-items: center;
        justify-content: center;
        cursor: nwse-resize;
        color: var(--bs-secondary);
    }}

    .widget-resize-handle:hover {{
        background: var(--bs-primary);
        color: white;
    }}
</style>

@code {{
    [Parameter] public Dashboard_{name}.DashboardWidget Widget {{ get; set; }} = new();
    [Parameter] public bool EditMode {{ get; set; }}
    [Parameter] public EventCallback<(Guid id, int col, int row)> OnMoved {{ get; set; }}
    [Parameter] public EventCallback<(Guid id, int width, int height)> OnResized {{ get; set; }}
    [Parameter] public EventCallback<Guid> OnRemoved {{ get; set; }}

    private bool _editMode => EditMode;
    private bool _isDragging = false;

    private void HandleDragStart()
    {{
        _isDragging = true;
    }}

    private void HandleDragEnd()
    {{
        _isDragging = false;
    }}

    private async Task MoveWidget(int colDelta, int rowDelta)
    {{
        var newCol = Math.Max(0, Math.Min(12 - Widget.Width, Widget.Column + colDelta));
        var newRow = Math.Max(0, Widget.Row + rowDelta);
        await OnMoved.InvokeAsync((Widget.Id, newCol, newRow));
    }}

    private async Task ResizeWidget(int widthDelta, int heightDelta)
    {{
        var newWidth = Math.Max(2, Math.Min(12, Widget.Width + widthDelta));
        var newHeight = Math.Max(2, Math.Min(8, Widget.Height + heightDelta));
        await OnResized.InvokeAsync((Widget.Id, newWidth, newHeight));
    }}

    private async Task RemoveWidget()
    {{
        await OnRemoved.InvokeAsync(Widget.Id);
    }}

    private void StartResize()
    {{
        // Resize functionality handled by CSS and button controls
    }}
}}
";
}

#endregion

// ---- PagePatterns.DataImportWizard ----

#region PagePatterns - DataImportWizard
// ============================================================================
// PAGE PATTERN: DATA IMPORT WIZARD
// Multi-step file upload with column mapping and validation.
//
// Generated Files:
// - ImportWizard.App.razor (main wizard container)
// - UploadStep.App.razor (drag-drop file upload)
// - MappingStep.App.razor (column mapping interface)
// - ValidateStep.App.razor (validation preview)
// - ProgressStep.App.razor (import progress tracking)
// ============================================================================

public static partial class PagePatterns
{
    /// <summary>
    /// Generate DataImportWizard files for the given entity/project.
    /// </summary>
    public static List<(string FileName, string FileType, string Content)> GetDataImportWizardFiles(
        string projectName,
        DataObjects.EntityDefinition? entity = null){
        List<(string, string, string)> files = new();
        string entityName = entity?.Name ?? "Item";
        string pluralName = entity?.PluralName ?? "Items";

        files.Add(($"{projectName}.App.ImportWizard.razor", "RazorComponent", GetDataImportWizard_ImportWizard(projectName, entityName, pluralName)));
        files.Add(($"{projectName}.App.UploadStep.razor", "RazorComponent", GetDataImportWizard_UploadStep(projectName, entityName)));
        files.Add(($"{projectName}.App.MappingStep.razor", "RazorComponent", GetDataImportWizard_MappingStep(projectName, entityName)));
        files.Add(($"{projectName}.App.ValidateStep.razor", "RazorComponent", GetDataImportWizard_ValidateStep(projectName, entityName)));
        files.Add(($"{projectName}.App.ProgressStep.razor", "RazorComponent", GetDataImportWizard_ProgressStep(projectName, entityName)));

        return files;
    }

    // ============================================================
    // IMPORT WIZARD - Main Container
    // ============================================================

    private static string GetDataImportWizard_ImportWizard(string name, string entityName, string pluralName) => $@"@* {name} - ImportWizard.razor *@
@* Data Import Wizard for {entityName} - Generated by Page Pattern *@
@page ""/import/{pluralName.ToLower()}""
@page ""/{{TenantCode}}/import/{pluralName.ToLower()}""
@implements IDisposable
@inject BlazorDataModel Model
@inject HttpClient Http
@inject IJSRuntime JS

@if (Model.Loaded && Model.LoggedIn) {{
    <div class=""container-fluid py-4"">
        <div class=""row justify-content-center"">
            <div class=""col-lg-10"">
                <div class=""card shadow"">
                    <div class=""card-header bg-primary text-white"">
                        <h4 class=""mb-0"">
                            <i class=""fa-solid fa-file-import me-2""></i>Import {pluralName}
                        </h4>
                    </div>
                    <div class=""card-body"">
                        @* Step Progress Indicator *@
                        <div class=""mb-4"">
                            <div class=""d-flex justify-content-between align-items-center position-relative"">
                                <div class=""position-absolute top-50 start-0 end-0"" style=""height: 2px; background: #dee2e6; z-index: 0;""></div>
                                @for (int i = 1; i <= 4; i++) {{
                                    var stepNum = i;
                                    var isActive = stepNum == _currentStep;
                                    var isCompleted = stepNum < _currentStep;
                                    <div class=""position-relative bg-white px-2 z-1"">
                                        <div class=""rounded-circle d-flex align-items-center justify-content-center @(isCompleted ? ""bg-success text-white"" : isActive ? ""bg-primary text-white"" : ""bg-light text-muted"")""
                                             style=""width: 40px; height: 40px;"">
                                            @if (isCompleted) {{
                                                <i class=""fa-solid fa-check""></i>
                                            }} else {{
                                                @stepNum
                                            }}
                                        </div>
                                        <div class=""text-center mt-1 small @(isActive ? ""fw-bold"" : ""text-muted"")"">@GetStepName(stepNum)</div>
                                    </div>
                                }}
                            </div>
                        </div>

                        @* Step Content *@
                        @switch (_currentStep) {{
                            case 1:
                                <{name}_App_UploadStep
                                    OnFileUploaded=""HandleFileUploaded""
                                    OnError=""HandleError"" />
                                break;
                            case 2:
                                <{name}_App_MappingStep
                                    CsvColumns=""_csvColumns""
                                    EntityProperties=""_entityProperties""
                                    ColumnMappings=""_columnMappings""
                                    OnMappingsChanged=""HandleMappingsChanged""
                                    OnBack=""GoBack"" />
                                break;
                            case 3:
                                <{name}_App_ValidateStep
                                    PreviewData=""_previewData""
                                    ValidationErrors=""_validationErrors""
                                    OnBack=""GoBack""
                                    OnConfirm=""StartImport"" />
                                break;
                            case 4:
                                <{name}_App_ProgressStep
                                    Progress=""_importProgress""
                                    TotalRows=""_totalRows""
                                    ProcessedRows=""_processedRows""
                                    FailedRows=""_failedRows""
                                    IsComplete=""_importComplete""
                                    OnRollback=""HandleRollback""
                                    OnClose=""ResetWizard"" />
                                break;
                        }}

                        @* Error Display *@
                        @if (!string.IsNullOrEmpty(_errorMessage)) {{
                            <div class=""alert alert-danger mt-3"">
                                <i class=""fa-solid fa-exclamation-triangle me-2""></i>@_errorMessage
                                <button type=""button"" class=""btn-close float-end"" @onclick=""() => _errorMessage = string.Empty""></button>
                            </div>
                        }}
                    </div>
                </div>
            </div>
        </div>
    </div>
}}

@code {{
    private int _currentStep = 1;
    private string _errorMessage = string.Empty;

    // Upload state
    private byte[]? _fileContent;
    private string _fileName = string.Empty;

    // Mapping state
    private List<string> _csvColumns = new();
    private List<string> _entityProperties = new() {{ ""Name"", ""Description"", ""Email"", ""Phone"", ""Address"", ""Status"", ""CreatedAt"" }};
    private Dictionary<string, string> _columnMappings = new();

    // Validation state
    private List<Dictionary<string, object?>> _previewData = new();
    private List<ImportValidationError> _validationErrors = new();

    // Progress state
    private int _importProgress = 0;
    private int _totalRows = 0;
    private int _processedRows = 0;
    private List<ImportFailedRow> _failedRows = new();
    private bool _importComplete = false;
    private List<Guid> _importedIds = new();

    public void Dispose() {{ Model.OnChange -= StateHasChanged; }}

    protected override void OnInitialized()
    {{
        Model.OnChange += StateHasChanged;
    }}

    private string GetStepName(int step) => step switch
    {{
        1 => ""Upload"",
        2 => ""Mapping"",
        3 => ""Validate"",
        4 => ""Import"",
        _ => """"
    }};

    private void HandleFileUploaded((byte[] content, string fileName, List<string> columns, List<Dictionary<string, object?>> preview) data)
    {{
        _fileContent = data.content;
        _fileName = data.fileName;
        _csvColumns = data.columns;
        _previewData = data.preview;

        // Auto-map matching column names
        foreach (var col in _csvColumns)
        {{
            var match = _entityProperties.FirstOrDefault(p =>
                p.Equals(col, StringComparison.OrdinalIgnoreCase) ||
                p.Replace("" "", """").Equals(col.Replace("" "", """"), StringComparison.OrdinalIgnoreCase));
            if (match != null)
            {{
                _columnMappings[col] = match;
            }}
        }}

        _currentStep = 2;
        StateHasChanged();
    }}

    private void HandleMappingsChanged(Dictionary<string, string> mappings)
    {{
        _columnMappings = mappings;
        ValidateData();
        _currentStep = 3;
        StateHasChanged();
    }}

    private void ValidateData()
    {{
        _validationErrors.Clear();
        int rowNum = 0;

        foreach (var row in _previewData)
        {{
            rowNum++;
            foreach (var mapping in _columnMappings)
            {{
                if (row.TryGetValue(mapping.Key, out var value))
                {{
                    // Basic validation checks
                    if (mapping.Value.Contains(""Email"") && value != null)
                    {{
                        var email = value.ToString();
                        if (!string.IsNullOrEmpty(email) && !email!.Contains(""@""))
                        {{
                            _validationErrors.Add(new ImportValidationError
                            {{
                                RowNumber = rowNum,
                                ColumnName = mapping.Key,
                                Value = email,
                                Message = ""Invalid email format""
                            }});
                        }}
                    }}
                }}
            }}
        }}
    }}

    private async Task StartImport()
    {{
        _currentStep = 4;
        _totalRows = _previewData.Count;
        _processedRows = 0;
        _failedRows.Clear();
        _importedIds.Clear();
        _importComplete = false;
        StateHasChanged();

        // Simulate import process
        foreach (var row in _previewData)
        {{
            await Task.Delay(100); // Simulate API call

            try
            {{
                // TODO: Call actual import API
                _importedIds.Add(Guid.NewGuid());
                _processedRows++;
            }}
            catch (Exception ex)
            {{
                _failedRows.Add(new ImportFailedRow
                {{
                    RowNumber = _processedRows + 1,
                    Data = row,
                    ErrorMessage = ex.Message
                }});
            }}

            _importProgress = (int)((_processedRows / (double)_totalRows) * 100);
            StateHasChanged();
        }}

        _importComplete = true;
        StateHasChanged();
    }}

    private async Task HandleRollback()
    {{
        if (!await Helpers.Confirm(""Are you sure you want to rollback all imported records?""))
            return;

        // TODO: Call rollback API with _importedIds
        foreach (var id in _importedIds)
        {{
            // await Helpers.GetOrPost<bool>(DataObjects.Endpoints.{name}.Delete{entityName}, id);
        }}

        ResetWizard();
    }}

    private void GoBack()
    {{
        if (_currentStep > 1)
        {{
            _currentStep--;
            StateHasChanged();
        }}
    }}

    private void ResetWizard()
    {{
        _currentStep = 1;
        _fileContent = null;
        _fileName = string.Empty;
        _csvColumns.Clear();
        _columnMappings.Clear();
        _previewData.Clear();
        _validationErrors.Clear();
        _importProgress = 0;
        _totalRows = 0;
        _processedRows = 0;
        _failedRows.Clear();
        _importedIds.Clear();
        _importComplete = false;
        _errorMessage = string.Empty;
        StateHasChanged();
    }}

    private void HandleError(string message)
    {{
        _errorMessage = message;
        StateHasChanged();
    }}

    public class ImportValidationError
    {{
        public int RowNumber {{ get; set; }}
        public string ColumnName {{ get; set; }} = string.Empty;
        public string? Value {{ get; set; }}
        public string Message {{ get; set; }} = string.Empty;
    }}

    public class ImportFailedRow
    {{
        public int RowNumber {{ get; set; }}
        public Dictionary<string, object?> Data {{ get; set; }} = new();
        public string ErrorMessage {{ get; set; }} = string.Empty;
    }}
}}
";

    // ============================================================
    // MAPPING STEP - Column Mapping Interface
    // ============================================================
}

#endregion

// ---- PagePatterns.DataImportWizard.Steps.Input ----

#region PagePatterns - DataImportWizard (Steps.Input)
// Part of PagePatterns.DataImportWizard: generator methods split out of the PagePatterns.DataImportWizard section above, now merged into FreeManager.App.PagePatterns.cs (UploadStep, MappingStep).

public static partial class PagePatterns
{
    private static string GetDataImportWizard_MappingStep(string name, string entityName) => $@"@* {name} - MappingStep.razor *@
@* Column Mapping Interface *@

<div class=""py-3"">
    <div class=""d-flex justify-content-between align-items-center mb-4"">
        <h5 class=""mb-0"">
            <i class=""fa-solid fa-arrows-left-right me-2""></i>Map CSV Columns to {entityName} Fields
        </h5>
        <div>
            <button class=""btn btn-outline-secondary me-2"" @onclick=""AutoMap"">
                <i class=""fa-solid fa-magic me-1""></i>Auto-Map
            </button>
            <button class=""btn btn-outline-secondary"" @onclick=""ClearMappings"">
                <i class=""fa-solid fa-eraser me-1""></i>Clear All
            </button>
        </div>
    </div>

    <div class=""card"">
        <div class=""table-responsive"">
            <table class=""table table-hover mb-0"">
                <thead class=""table-light"">
                    <tr>
                        <th style=""width: 40%;"">CSV Column</th>
                        <th style=""width: 20%;"" class=""text-center"">
                            <i class=""fa-solid fa-arrow-right""></i>
                        </th>
                        <th style=""width: 40%;"">{entityName} Field</th>
                    </tr>
                </thead>
                <tbody>
                    @foreach (var column in CsvColumns) {{
                        <tr>
                            <td>
                                <div class=""d-flex align-items-center"">
                                    <i class=""fa-solid fa-table-columns text-muted me-2""></i>
                                    <span class=""fw-medium"">@column</span>
                                </div>
                            </td>
                            <td class=""text-center"">
                                @if (_localMappings.ContainsKey(column) && !string.IsNullOrEmpty(_localMappings[column])) {{
                                    <i class=""fa-solid fa-link text-success""></i>
                                }} else {{
                                    <i class=""fa-solid fa-minus text-muted""></i>
                                }}
                            </td>
                            <td>
                                <select class=""form-select form-select-sm""
                                        value=""@(_localMappings.GetValueOrDefault(column) ?? """")""
                                        @onchange=""e => UpdateMapping(column, e.Value?.ToString())"">
                                    <option value="""">(Skip this column)</option>
                                    @foreach (var prop in EntityProperties) {{
                                        <option value=""@prop"" disabled=""@IsPropertyMapped(prop, column)"">
                                            @prop @(IsPropertyMapped(prop, column) ? ""(already mapped)"" : """")
                                        </option>
                                    }}
                                </select>
                            </td>
                        </tr>
                    }}
                </tbody>
            </table>
        </div>
    </div>

    <div class=""mt-3"">
        <div class=""row"">
            <div class=""col-md-6"">
                <div class=""card bg-light"">
                    <div class=""card-body py-2"">
                        <div class=""d-flex justify-content-between"">
                            <span class=""text-muted"">Mapped columns:</span>
                            <strong class=""text-success"">@MappedCount / @CsvColumns.Count</strong>
                        </div>
                    </div>
                </div>
            </div>
            <div class=""col-md-6"">
                <div class=""card bg-light"">
                    <div class=""card-body py-2"">
                        <div class=""d-flex justify-content-between"">
                            <span class=""text-muted"">Skipped columns:</span>
                            <strong class=""text-warning"">@(CsvColumns.Count - MappedCount)</strong>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>

    <div class=""d-flex justify-content-between mt-4"">
        <button class=""btn btn-outline-secondary"" @onclick=""OnBack"">
            <i class=""fa-solid fa-arrow-left me-1""></i>Back
        </button>
        <button class=""btn btn-primary"" @onclick=""ProceedToValidation"" disabled=""@(MappedCount == 0)"">
            Continue to Validation<i class=""fa-solid fa-arrow-right ms-1""></i>
        </button>
    </div>
</div>

@code {{
    [Parameter] public List<string> CsvColumns {{ get; set; }} = new();
    [Parameter] public List<string> EntityProperties {{ get; set; }} = new();
    [Parameter] public Dictionary<string, string> ColumnMappings {{ get; set; }} = new();
    [Parameter] public EventCallback<Dictionary<string, string>> OnMappingsChanged {{ get; set; }}
    [Parameter] public EventCallback OnBack {{ get; set; }}

    private Dictionary<string, string> _localMappings = new();

    private int MappedCount => _localMappings.Count(m => !string.IsNullOrEmpty(m.Value));

    protected override void OnParametersSet()
    {{
        _localMappings = new Dictionary<string, string>(ColumnMappings);
        foreach (var col in CsvColumns)
        {{
            if (!_localMappings.ContainsKey(col))
            {{
                _localMappings[col] = string.Empty;
            }}
        }}
    }}

    private void UpdateMapping(string column, string? property)
    {{
        _localMappings[column] = property ?? string.Empty;
    }}

    private bool IsPropertyMapped(string property, string currentColumn)
    {{
        return _localMappings.Any(m => m.Value == property && m.Key != currentColumn);
    }}

    private void AutoMap()
    {{
        foreach (var col in CsvColumns)
        {{
            var match = EntityProperties.FirstOrDefault(p =>
                p.Equals(col, StringComparison.OrdinalIgnoreCase) ||
                p.Replace("" "", """").Equals(col.Replace("" "", """"), StringComparison.OrdinalIgnoreCase) ||
                p.Replace(""_"", """").Equals(col.Replace(""_"", """"), StringComparison.OrdinalIgnoreCase));

            if (match != null && !IsPropertyMapped(match, col))
            {{
                _localMappings[col] = match;
            }}
        }}
    }}

    private void ClearMappings()
    {{
        foreach (var col in CsvColumns)
        {{
            _localMappings[col] = string.Empty;
        }}
    }}

    private async Task ProceedToValidation()
    {{
        var mappings = _localMappings.Where(m => !string.IsNullOrEmpty(m.Value))
                                      .ToDictionary(m => m.Key, m => m.Value);
        await OnMappingsChanged.InvokeAsync(mappings);
    }}
}}
";

    // ============================================================
    // PROGRESS STEP - Import Progress Tracking
    // ============================================================

    private static string GetDataImportWizard_UploadStep(string name, string entityName) => $@"@* {name} - UploadStep.razor *@
@* File Upload Step with Drag-Drop Support *@

<div class=""text-center py-4"">
    <div class=""upload-zone border border-2 border-dashed rounded-3 p-5 @(_isDragging ? ""border-primary bg-light"" : ""border-secondary"")""
         @ondragenter=""HandleDragEnter""
         @ondragover=""HandleDragOver""
         @ondragover:preventDefault
         @ondragleave=""HandleDragLeave""
         @ondrop=""HandleDrop""
         @ondrop:preventDefault>

        @if (_isProcessing) {{
            <div class=""mb-3"">
                <i class=""fa-solid fa-spinner fa-spin fa-3x text-primary""></i>
            </div>
            <h5>Processing file...</h5>
        }} else if (!string.IsNullOrEmpty(_uploadedFileName)) {{
            <div class=""mb-3"">
                <i class=""fa-solid fa-file-csv fa-3x text-success""></i>
            </div>
            <h5 class=""text-success"">@_uploadedFileName</h5>
            <p class=""text-muted mb-3"">@_rowCount rows detected</p>
            <button class=""btn btn-primary btn-lg"" @onclick=""ProceedToMapping"">
                <i class=""fa-solid fa-arrow-right me-2""></i>Continue to Mapping
            </button>
            <button class=""btn btn-outline-secondary ms-2"" @onclick=""ClearFile"">
                <i class=""fa-solid fa-times me-1""></i>Clear
            </button>
        }} else {{
            <div class=""mb-3"">
                <i class=""fa-solid fa-cloud-arrow-up fa-3x text-muted""></i>
            </div>
            <h5>Drag & drop your CSV file here</h5>
            <p class=""text-muted mb-3"">or</p>
            <label class=""btn btn-primary btn-lg"">
                <i class=""fa-solid fa-folder-open me-2""></i>Browse Files
                <InputFile OnChange=""HandleFileSelected"" accept="".csv,.txt"" class=""d-none"" />
            </label>
            <p class=""text-muted mt-3 small"">Supported formats: CSV, TXT (max 10MB)</p>
        }}
    </div>

    <div class=""mt-4"">
        <div class=""card bg-light"">
            <div class=""card-body"">
                <h6 class=""card-title""><i class=""fa-solid fa-lightbulb text-warning me-2""></i>Import Tips</h6>
                <ul class=""text-start mb-0 small"">
                    <li>Ensure your CSV file has headers in the first row</li>
                    <li>Use UTF-8 encoding for special characters</li>
                    <li>Maximum 10,000 rows per import</li>
                    <li>You can map columns in the next step</li>
                </ul>
            </div>
        </div>
    </div>
</div>

@code {{
    [Parameter] public EventCallback<(byte[] content, string fileName, List<string> columns, List<Dictionary<string, object?>> preview)> OnFileUploaded {{ get; set; }}
    [Parameter] public EventCallback<string> OnError {{ get; set; }}

    private bool _isDragging = false;
    private bool _isProcessing = false;
    private string _uploadedFileName = string.Empty;
    private int _rowCount = 0;
    private byte[]? _fileContent;
    private List<string> _columns = new();
    private List<Dictionary<string, object?>> _previewData = new();

    private void HandleDragEnter() => _isDragging = true;
    private void HandleDragOver() => _isDragging = true;
    private void HandleDragLeave() => _isDragging = false;

    private async Task HandleDrop(DragEventArgs e)
    {{
        _isDragging = false;
        // Note: Browser file drop requires JS interop for full implementation
        // This is a placeholder - actual implementation would use JS to get dropped files
    }}

    private async Task HandleFileSelected(InputFileChangeEventArgs e)
    {{
        var file = e.File;

        if (file.Size > 10 * 1024 * 1024)
        {{
            await OnError.InvokeAsync(""File size exceeds 10MB limit"");
            return;
        }}

        _isProcessing = true;
        StateHasChanged();

        try
        {{
            using var stream = file.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024);
            using var memoryStream = new System.IO.MemoryStream();
            await stream.CopyToAsync(memoryStream);
            _fileContent = memoryStream.ToArray();

            // Parse CSV
            var content = System.Text.Encoding.UTF8.GetString(_fileContent);
            var lines = content.Split(new[] {{ ""\r\n"", ""\n"" }}, StringSplitOptions.RemoveEmptyEntries);

            if (lines.Length < 2)
            {{
                await OnError.InvokeAsync(""File must contain at least a header row and one data row"");
                _isProcessing = false;
                return;
            }}

            // Parse headers
            _columns = ParseCsvLine(lines[0]);

            // Parse preview data (first 100 rows)
            _previewData.Clear();
            for (int i = 1; i < Math.Min(lines.Length, 101); i++)
            {{
                var values = ParseCsvLine(lines[i]);
                var row = new Dictionary<string, object?>();
                for (int j = 0; j < Math.Min(_columns.Count, values.Count); j++)
                {{
                    row[_columns[j]] = values[j];
                }}
                _previewData.Add(row);
            }}

            _uploadedFileName = file.Name;
            _rowCount = lines.Length - 1;
        }}
        catch (Exception ex)
        {{
            await OnError.InvokeAsync($""Error processing file: {{ex.Message}}"");
        }}

        _isProcessing = false;
        StateHasChanged();
    }}

    private List<string> ParseCsvLine(string line)
    {{
        var result = new List<string>();
        var inQuotes = false;
        var field = new System.Text.StringBuilder();

        for (int i = 0; i < line.Length; i++)
        {{
            var c = line[i];

            if (c == '""')
            {{
                inQuotes = !inQuotes;
            }}
            else if (c == ',' && !inQuotes)
            {{
                result.Add(field.ToString().Trim());
                field.Clear();
            }}
            else
            {{
                field.Append(c);
            }}
        }}

        result.Add(field.ToString().Trim());
        return result;
    }}

    private async Task ProceedToMapping()
    {{
        if (_fileContent != null && _columns.Count > 0)
        {{
            await OnFileUploaded.InvokeAsync((_fileContent, _uploadedFileName, _columns, _previewData));
        }}
    }}

    private void ClearFile()
    {{
        _fileContent = null;
        _uploadedFileName = string.Empty;
        _rowCount = 0;
        _columns.Clear();
        _previewData.Clear();
    }}
}}
";

    // ============================================================
    // VALIDATE STEP - Validation Preview
    // ============================================================
}

#endregion

// ---- PagePatterns.DataImportWizard.Steps.Results ----

#region PagePatterns - DataImportWizard (Steps.Results)
// Part of PagePatterns.DataImportWizard: generator methods split out of the PagePatterns.DataImportWizard section above, now merged into FreeManager.App.PagePatterns.cs (ValidateStep, ProgressStep).

public static partial class PagePatterns
{
    private static string GetDataImportWizard_ProgressStep(string name, string entityName) => $@"@* {name} - ProgressStep.razor *@
@* Import Progress Tracking Step *@

<div class=""py-3"">
    <div class=""text-center mb-4"">
        @if (IsComplete) {{
            @if (FailedRows.Count == 0) {{
                <div class=""mb-3"">
                    <i class=""fa-solid fa-check-circle fa-4x text-success""></i>
                </div>
                <h4 class=""text-success"">Import Complete!</h4>
                <p class=""text-muted"">Successfully imported @ProcessedRows {entityName.ToLower()} records.</p>
            }} else {{
                <div class=""mb-3"">
                    <i class=""fa-solid fa-exclamation-circle fa-4x text-warning""></i>
                </div>
                <h4 class=""text-warning"">Import Completed with Errors</h4>
                <p class=""text-muted"">Imported @(ProcessedRows - FailedRows.Count) of @TotalRows records. @FailedRows.Count row(s) failed.</p>
            }}
        }} else {{
            <div class=""mb-3"">
                <i class=""fa-solid fa-cog fa-spin fa-4x text-primary""></i>
            </div>
            <h4>Importing Data...</h4>
            <p class=""text-muted"">Please wait while your data is being imported.</p>
        }}
    </div>

    <div class=""card mb-4"">
        <div class=""card-body"">
            <div class=""d-flex justify-content-between mb-2"">
                <span>Progress</span>
                <span class=""fw-bold"">@ProcessedRows / @TotalRows rows</span>
            </div>
            <div class=""progress"" style=""height: 25px;"">
                <div class=""progress-bar @(IsComplete && FailedRows.Count == 0 ? ""bg-success"" : IsComplete ? ""bg-warning"" : """")""
                     role=""progressbar""
                     style=""width: @Progress%;""
                     aria-valuenow=""@Progress""
                     aria-valuemin=""0""
                     aria-valuemax=""100"">
                    @Progress%
                </div>
            </div>
        </div>
    </div>

    @if (IsComplete) {{
        <div class=""row mb-4"">
            <div class=""col-md-4"">
                <div class=""card text-center border-success"">
                    <div class=""card-body"">
                        <i class=""fa-solid fa-check text-success fa-2x mb-2""></i>
                        <h3 class=""text-success mb-0"">@(ProcessedRows - FailedRows.Count)</h3>
                        <small class=""text-muted"">Successful</small>
                    </div>
                </div>
            </div>
            <div class=""col-md-4"">
                <div class=""card text-center border-danger"">
                    <div class=""card-body"">
                        <i class=""fa-solid fa-times text-danger fa-2x mb-2""></i>
                        <h3 class=""text-danger mb-0"">@FailedRows.Count</h3>
                        <small class=""text-muted"">Failed</small>
                    </div>
                </div>
            </div>
            <div class=""col-md-4"">
                <div class=""card text-center"">
                    <div class=""card-body"">
                        <i class=""fa-solid fa-clock text-muted fa-2x mb-2""></i>
                        <h3 class=""mb-0"">@TotalRows</h3>
                        <small class=""text-muted"">Total Processed</small>
                    </div>
                </div>
            </div>
        </div>
    }}

    @if (FailedRows.Count > 0) {{
        <div class=""card border-danger mb-4"">
            <div class=""card-header bg-danger text-white"">
                <i class=""fa-solid fa-exclamation-triangle me-2""></i>Failed Rows
            </div>
            <div class=""table-responsive"" style=""max-height: 300px; overflow-y: auto;"">
                <table class=""table table-sm mb-0"">
                    <thead class=""table-light sticky-top"">
                        <tr>
                            <th>Row</th>
                            <th>Error</th>
                            <th>Data Preview</th>
                        </tr>
                    </thead>
                    <tbody>
                        @foreach (var failed in FailedRows) {{
                            <tr>
                                <td>@failed.RowNumber</td>
                                <td class=""text-danger"">@failed.ErrorMessage</td>
                                <td><code>@string.Join("", "", failed.Data.Take(3).Select(d => $""{{d.Key}}: {{d.Value}}""))</code></td>
                            </tr>
                        }}
                    </tbody>
                </table>
            </div>
        </div>
    }}

    @if (IsComplete) {{
        <div class=""d-flex justify-content-between"">
            <button class=""btn btn-outline-danger"" @onclick=""OnRollback"" disabled=""@(ProcessedRows == FailedRows.Count)"">
                <i class=""fa-solid fa-rotate-left me-1""></i>Rollback Import
            </button>
            <div>
                @if (FailedRows.Count > 0) {{
                    <button class=""btn btn-outline-secondary me-2"" @onclick=""DownloadFailedRows"">
                        <i class=""fa-solid fa-download me-1""></i>Download Failed Rows
                    </button>
                }}
                <button class=""btn btn-primary"" @onclick=""OnClose"">
                    <i class=""fa-solid fa-check me-1""></i>Done
                </button>
            </div>
        </div>
    }}
</div>

@code {{
    [Parameter] public int Progress {{ get; set; }}
    [Parameter] public int TotalRows {{ get; set; }}
    [Parameter] public int ProcessedRows {{ get; set; }}
    [Parameter] public List<ImportFailedRow> FailedRows {{ get; set; }} = new();
    [Parameter] public bool IsComplete {{ get; set; }}
    [Parameter] public EventCallback OnRollback {{ get; set; }}
    [Parameter] public EventCallback OnClose {{ get; set; }}

    private void DownloadFailedRows()
    {{
        // TODO: Implement CSV export of failed rows
        // This would create a CSV with the failed row data for user review
    }}

    public class ImportFailedRow
    {{
        public int RowNumber {{ get; set; }}
        public Dictionary<string, object?> Data {{ get; set; }} = new();
        public string ErrorMessage {{ get; set; }} = string.Empty;
    }}
}}
";

    // ============================================================
    // UPLOAD STEP - Drag & Drop File Upload
    // ============================================================

    private static string GetDataImportWizard_ValidateStep(string name, string entityName) => $@"@* {name} - ValidateStep.razor *@
@* Validation Preview Step *@

<div class=""py-3"">
    <div class=""d-flex justify-content-between align-items-center mb-4"">
        <h5 class=""mb-0"">
            <i class=""fa-solid fa-clipboard-check me-2""></i>Review & Validate Data
        </h5>
        <div>
            @if (ValidationErrors.Count == 0) {{
                <span class=""badge bg-success fs-6"">
                    <i class=""fa-solid fa-check me-1""></i>No validation errors
                </span>
            }} else {{
                <span class=""badge bg-warning text-dark fs-6"">
                    <i class=""fa-solid fa-exclamation-triangle me-1""></i>@ValidationErrors.Count validation issue(s)
                </span>
            }}
        </div>
    </div>

    @if (ValidationErrors.Count > 0) {{
        <div class=""alert alert-warning mb-4"">
            <h6 class=""alert-heading"">
                <i class=""fa-solid fa-exclamation-triangle me-2""></i>Validation Issues Found
            </h6>
            <p class=""mb-2"">The following issues were found in your data. You can still proceed, but these rows may fail to import:</p>
            <div class=""table-responsive"" style=""max-height: 200px; overflow-y: auto;"">
                <table class=""table table-sm table-bordered mb-0 bg-white"">
                    <thead>
                        <tr>
                            <th>Row</th>
                            <th>Column</th>
                            <th>Value</th>
                            <th>Issue</th>
                        </tr>
                    </thead>
                    <tbody>
                        @foreach (var error in ValidationErrors.Take(20)) {{
                            <tr>
                                <td>@error.RowNumber</td>
                                <td>@error.ColumnName</td>
                                <td><code>@(error.Value ?? ""(empty)"")</code></td>
                                <td class=""text-danger"">@error.Message</td>
                            </tr>
                        }}
                        @if (ValidationErrors.Count > 20) {{
                            <tr>
                                <td colspan=""4"" class=""text-center text-muted"">
                                    ... and @(ValidationErrors.Count - 20) more issues
                                </td>
                            </tr>
                        }}
                    </tbody>
                </table>
            </div>
        </div>
    }}

    <div class=""card mb-4"">
        <div class=""card-header d-flex justify-content-between align-items-center"">
            <span><i class=""fa-solid fa-table me-2""></i>Data Preview (first 10 rows)</span>
            <span class=""badge bg-secondary"">@PreviewData.Count total rows</span>
        </div>
        <div class=""table-responsive"" style=""max-height: 400px; overflow: auto;"">
            <table class=""table table-sm table-striped mb-0"">
                <thead class=""table-light sticky-top"">
                    <tr>
                        <th>#</th>
                        @if (PreviewData.Count > 0) {{
                            @foreach (var col in PreviewData[0].Keys) {{
                                <th>@col</th>
                            }}
                        }}
                    </tr>
                </thead>
                <tbody>
                    @{{ int rowNum = 0; }}
                    @foreach (var row in PreviewData.Take(10)) {{
                        rowNum++;
                        var hasError = ValidationErrors.Any(e => e.RowNumber == rowNum);
                        <tr class=""@(hasError ? ""table-warning"" : """")"">
                            <td>
                                @rowNum
                                @if (hasError) {{
                                    <i class=""fa-solid fa-exclamation-circle text-warning ms-1""></i>
                                }}
                            </td>
                            @foreach (var val in row.Values) {{
                                <td>@(val?.ToString() ?? """")</td>
                            }}
                        </tr>
                    }}
                </tbody>
            </table>
        </div>
    </div>

    <div class=""card bg-light mb-4"">
        <div class=""card-body"">
            <h6 class=""card-title""><i class=""fa-solid fa-info-circle me-2""></i>Import Summary</h6>
            <div class=""row"">
                <div class=""col-md-4"">
                    <div class=""d-flex justify-content-between"">
                        <span>Total rows to import:</span>
                        <strong>@PreviewData.Count</strong>
                    </div>
                </div>
                <div class=""col-md-4"">
                    <div class=""d-flex justify-content-between"">
                        <span>Rows with warnings:</span>
                        <strong class=""text-warning"">@ValidationErrors.Select(e => e.RowNumber).Distinct().Count()</strong>
                    </div>
                </div>
                <div class=""col-md-4"">
                    <div class=""d-flex justify-content-between"">
                        <span>Expected success:</span>
                        <strong class=""text-success"">@(PreviewData.Count - ValidationErrors.Select(e => e.RowNumber).Distinct().Count())</strong>
                    </div>
                </div>
            </div>
        </div>
    </div>

    <div class=""d-flex justify-content-between"">
        <button class=""btn btn-outline-secondary"" @onclick=""OnBack"">
            <i class=""fa-solid fa-arrow-left me-1""></i>Back to Mapping
        </button>
        <button class=""btn btn-success btn-lg"" @onclick=""OnConfirm"">
            <i class=""fa-solid fa-play me-1""></i>Start Import
        </button>
    </div>
</div>

@code {{
    [Parameter] public List<Dictionary<string, object?>> PreviewData {{ get; set; }} = new();
    [Parameter] public List<ImportValidationError> ValidationErrors {{ get; set; }} = new();
    [Parameter] public EventCallback OnBack {{ get; set; }}
    [Parameter] public EventCallback OnConfirm {{ get; set; }}

    public class ImportValidationError
    {{
        public int RowNumber {{ get; set; }}
        public string ColumnName {{ get; set; }} = string.Empty;
        public string? Value {{ get; set; }}
        public string Message {{ get; set; }} = string.Empty;
    }}
}}
";
}

#endregion

// ---- PagePatterns.KanbanBoard ----

#region PagePatterns - KanbanBoard
// ============================================================================
// PAGE PATTERN: KANBAN BOARD
// Drag-and-drop columns for visual workflow management
//
// Generated Files:
// - KanbanBoard.razor: Main board container with columns
// - KanbanColumn.razor: Individual column component with WIP limits
// - KanbanCard.razor: Draggable card component
// - CardEditor.razor: Quick edit modal for cards
// ============================================================================

public static partial class PagePatterns
{
    /// <summary>
    /// Generate KanbanBoard files for the given entity/project.
    /// </summary>
    public static List<(string FileName, string FileType, string Content)> GetKanbanBoardFiles(
        string projectName,
        DataObjects.EntityDefinition? entity = null){
        List<(string, string, string)> files = new();

        files.Add(($"{projectName}.App.KanbanBoard.razor", "RazorComponent", GetKanbanBoard_KanbanBoard(projectName, entity)));
        files.Add(($"{projectName}.App.KanbanColumn.razor", "RazorComponent", GetKanbanBoard_KanbanColumn(projectName, entity)));
        files.Add(($"{projectName}.App.KanbanCard.razor", "RazorComponent", GetKanbanBoard_KanbanCard(projectName, entity)));
        files.Add(($"{projectName}.App.CardEditor.razor", "RazorComponent", GetKanbanBoard_CardEditor(projectName, entity)));

        return files;
    }

    // ============================================================
    // CARD EDITOR - Quick Edit Modal
    // ============================================================

    private static string GetKanbanBoard_KanbanBoard(string name, DataObjects.EntityDefinition? entity) => $@"@* {name} - KanbanBoard.razor *@
@* Generated by Page Pattern: KanbanBoard *@
@page ""/kanban""
@page ""/{{TenantCode}}/kanban""
@implements IDisposable
@inject BlazorDataModel Model
@inject HttpClient Http

@if (Model.Loaded && Model.LoggedIn && Model.View == _pageName) {{
    <div class=""container-fluid"">
        <div class=""@Model.StickyMenuClass"">
            <div class=""d-flex justify-content-between align-items-center"">
                <h1 class=""page-title mb-0"">
                    <i class=""fa-solid fa-grip-vertical me-2""></i>Kanban Board
                </h1>
                <div class=""d-flex gap-2"">
                    <button class=""btn btn-outline-secondary"" @onclick=""ToggleSwimlanes"">
                        <i class=""fa-solid @(_showSwimlanes ? ""fa-compress"" : ""fa-layer-group"") me-1""></i>
                        @(_showSwimlanes ? ""Hide"" : ""Show"") Swimlanes
                    </button>
                    <button class=""btn btn-primary"" @onclick=""AddCard"">
                        <i class=""fa-solid fa-plus me-1""></i>New Card
                    </button>
                </div>
            </div>
        </div>

        @* Swimlane header (optional grouping) *@
        @if (_showSwimlanes) {{
            <div class=""swimlane-container"">
                @foreach (var swimlane in _swimlanes) {{
                    <div class=""swimlane mb-4"">
                        <div class=""swimlane-header d-flex align-items-center mb-2"">
                            <i class=""fa-solid fa-stream me-2""></i>
                            <strong>@swimlane</strong>
                            <span class=""badge bg-secondary ms-2"">@GetSwimlaneCardCount(swimlane)</span>
                        </div>
                        <div class=""kanban-board d-flex gap-3 overflow-auto pb-3"">
                            @foreach (var column in _columns) {{
                                <{name}_App_KanbanColumn
                                    Column=""column""
                                    Cards=""GetCardsForColumn(column.Id, swimlane)""
                                    OnCardDrop=""@((args) => HandleCardDrop(args.cardId, column.Id, swimlane))""
                                    OnCardClick=""EditCard""
                                    OnAddCard=""@(() => AddCardToColumn(column.Id, swimlane))"" />
                            }}
                        </div>
                    </div>
                }}
            </div>
        }} else {{
            <div class=""kanban-board d-flex gap-3 overflow-auto pb-3"" style=""min-height: calc(100vh - 200px);"">
                @foreach (var column in _columns) {{
                    <{name}_App_KanbanColumn
                        Column=""column""
                        Cards=""GetCardsForColumn(column.Id, null)""
                        OnCardDrop=""@((args) => HandleCardDrop(args.cardId, column.Id, null))""
                        OnCardClick=""EditCard""
                        OnAddCard=""@(() => AddCardToColumn(column.Id, null))"" />
                }}
            </div>
        }}
    </div>

    @if (_showEditor) {{
        <{name}_App_CardEditor
            Card=""_editingCard""
            Columns=""_columns""
            Swimlanes=""_swimlanes""
            OnSave=""SaveCard""
            OnDelete=""DeleteCard""
            OnCancel=""CloseEditor"" />
    }}
}}

<style>
    .kanban-board {{
        min-height: 500px;
    }}
    .swimlane {{
        background: var(--bs-gray-100);
        border-radius: 8px;
        padding: 1rem;
    }}
    .swimlane-header {{
        color: var(--bs-gray-700);
        font-size: 0.9rem;
        text-transform: uppercase;
        letter-spacing: 0.05em;
    }}
</style>

@code {{
    private string _pageName = ""kanban"";
    private bool _loading = true;
    private bool _showSwimlanes = false;
    private bool _showEditor = false;

    private List<KanbanColumnModel> _columns = new();
    private List<KanbanCardModel> _cards = new();
    private List<string> _swimlanes = new() {{ ""Default"" }};
    private KanbanCardModel _editingCard = new();

    public void Dispose() {{ Model.OnChange -= StateHasChanged; }}

    protected override void OnInitialized() {{
        Model.View = _pageName;
        Model.OnChange += StateHasChanged;
        InitializeBoard();
    }}

    private void InitializeBoard() {{
        // Default columns matching typical workflow statuses
        _columns = new List<KanbanColumnModel> {{
            new() {{ Id = ""todo"", Title = ""To Do"", Color = ""#6c757d"", WipLimit = 0, Order = 0 }},
            new() {{ Id = ""in-progress"", Title = ""In Progress"", Color = ""#0d6efd"", WipLimit = 5, Order = 1 }},
            new() {{ Id = ""review"", Title = ""In Review"", Color = ""#ffc107"", WipLimit = 3, Order = 2 }},
            new() {{ Id = ""done"", Title = ""Done"", Color = ""#198754"", WipLimit = 0, Order = 3 }}
        }};

        // Sample cards for demonstration
        _cards = new List<KanbanCardModel> {{
            new() {{ Id = Guid.NewGuid(), Title = ""Sample task 1"", ColumnId = ""todo"", Order = 0 }},
            new() {{ Id = Guid.NewGuid(), Title = ""Sample task 2"", ColumnId = ""in-progress"", Order = 0 }}
        }};

        _loading = false;
    }}

    private List<KanbanCardModel> GetCardsForColumn(string columnId, string? swimlane) {{
        return _cards
            .Where(c => c.ColumnId == columnId && (swimlane == null || c.Swimlane == swimlane || (swimlane == ""Default"" && string.IsNullOrEmpty(c.Swimlane))))
            .OrderBy(c => c.Order)
            .ToList();
    }}

    private int GetSwimlaneCardCount(string swimlane) {{
        return _cards.Count(c => c.Swimlane == swimlane || (swimlane == ""Default"" && string.IsNullOrEmpty(c.Swimlane)));
    }}

    private void ToggleSwimlanes() {{
        _showSwimlanes = !_showSwimlanes;
    }}

    private void AddCard() {{
        _editingCard = new KanbanCardModel {{
            Id = Guid.NewGuid(),
            ColumnId = _columns.FirstOrDefault()?.Id ?? ""todo"",
            CreatedAt = DateTime.UtcNow
        }};
        _showEditor = true;
    }}

    private void AddCardToColumn(string columnId, string? swimlane) {{
        _editingCard = new KanbanCardModel {{
            Id = Guid.NewGuid(),
            ColumnId = columnId,
            Swimlane = swimlane,
            CreatedAt = DateTime.UtcNow
        }};
        _showEditor = true;
    }}

    private void EditCard(Guid cardId) {{
        var card = _cards.FirstOrDefault(c => c.Id == cardId);
        if (card != null) {{
            _editingCard = card;
            _showEditor = true;
        }}
    }}

    private void HandleCardDrop(Guid cardId, string targetColumnId, string? targetSwimlane) {{
        var card = _cards.FirstOrDefault(c => c.Id == cardId);
        var targetColumn = _columns.FirstOrDefault(c => c.Id == targetColumnId);

        if (card == null || targetColumn == null) return;

        // Check WIP limit
        if (targetColumn.WipLimit > 0) {{
            var currentCount = GetCardsForColumn(targetColumnId, targetSwimlane).Count;
            if (currentCount >= targetColumn.WipLimit) {{
                // Could show a toast/alert here
                return;
            }}
        }}

        card.ColumnId = targetColumnId;
        if (targetSwimlane != null) card.Swimlane = targetSwimlane;
        card.UpdatedAt = DateTime.UtcNow;

        // Reorder cards in target column
        var cardsInColumn = GetCardsForColumn(targetColumnId, targetSwimlane);
        for (int i = 0; i < cardsInColumn.Count; i++) {{
            cardsInColumn[i].Order = i;
        }}

        StateHasChanged();
    }}

    private void SaveCard(KanbanCardModel card) {{
        var existing = _cards.FirstOrDefault(c => c.Id == card.Id);
        if (existing != null) {{
            existing.Title = card.Title;
            existing.Description = card.Description;
            existing.ColumnId = card.ColumnId;
            existing.Swimlane = card.Swimlane;
            existing.Assignee = card.Assignee;
            existing.DueDate = card.DueDate;
            existing.Priority = card.Priority;
            existing.Labels = card.Labels;
            existing.UpdatedAt = DateTime.UtcNow;
        }} else {{
            card.CreatedAt = DateTime.UtcNow;
            card.Order = _cards.Count(c => c.ColumnId == card.ColumnId);
            _cards.Add(card);
        }}
        _showEditor = false;
        StateHasChanged();
    }}

    private void DeleteCard(Guid cardId) {{
        _cards.RemoveAll(c => c.Id == cardId);
        _showEditor = false;
        StateHasChanged();
    }}

    private void CloseEditor() {{
        _showEditor = false;
    }}

    // ============================================================
    // View Models
    // ============================================================

    public class KanbanColumnModel {{
        public string Id {{ get; set; }} = string.Empty;
        public string Title {{ get; set; }} = string.Empty;
        public string Color {{ get; set; }} = ""#6c757d"";
        public int WipLimit {{ get; set; }} = 0;
        public int Order {{ get; set; }}
    }}

    public class KanbanCardModel {{
        public Guid Id {{ get; set; }}
        public string Title {{ get; set; }} = string.Empty;
        public string Description {{ get; set; }} = string.Empty;
        public string ColumnId {{ get; set; }} = string.Empty;
        public string? Swimlane {{ get; set; }}
        public string? Assignee {{ get; set; }}
        public DateTime? DueDate {{ get; set; }}
        public string Priority {{ get; set; }} = ""Medium"";
        public List<string> Labels {{ get; set; }} = new();
        public int Order {{ get; set; }}
        public DateTime CreatedAt {{ get; set; }}
        public DateTime? UpdatedAt {{ get; set; }}
    }}
}}
";

    // ============================================================
    // KANBAN CARD - Draggable Card Component
    // ============================================================

    private static string GetKanbanBoard_KanbanColumn(string name, DataObjects.EntityDefinition? entity) => $@"@* {name} - KanbanColumn.razor *@
@* Generated by Page Pattern: KanbanBoard *@

<div class=""kanban-column card shadow-sm""
     style=""min-width: 300px; max-width: 300px; background: var(--bs-gray-100);""
     @ondragover=""HandleDragOver""
     @ondragover:preventDefault=""true""
     @ondrop=""HandleDrop"">

    <div class=""card-header d-flex justify-content-between align-items-center""
         style=""border-left: 4px solid @Column.Color;"">
        <div class=""d-flex align-items-center"">
            <strong>@Column.Title</strong>
            <span class=""badge bg-secondary ms-2"">@Cards.Count</span>
            @if (Column.WipLimit > 0) {{
                <span class=""badge @(IsAtWipLimit ? ""bg-danger"" : ""bg-outline-secondary"") ms-1""
                      title=""WIP Limit"">
                    <i class=""fa-solid fa-gauge-high me-1""></i>@Cards.Count/@Column.WipLimit
                </span>
            }}
        </div>
        <button class=""btn btn-sm btn-outline-secondary"" @onclick=""@(() => OnAddCard.InvokeAsync())""
                disabled=""@IsAtWipLimit"">
            <i class=""fa-solid fa-plus""></i>
        </button>
    </div>

    <div class=""card-body p-2"" style=""min-height: 400px; max-height: calc(100vh - 280px); overflow-y: auto;"">
        @if (Cards.Count == 0) {{
            <div class=""text-center text-muted py-4"">
                <i class=""fa-regular fa-square-dashed fa-2x mb-2 opacity-50""></i>
                <p class=""small mb-0"">Drop cards here</p>
            </div>
        }} else {{
            @foreach (var card in Cards) {{
                <{name}_App_KanbanCard
                    Card=""card""
                    OnClick=""@(() => OnCardClick.InvokeAsync(card.Id))"" />
            }}
        }}
    </div>
</div>

<style>
    .kanban-column {{
        transition: box-shadow 0.2s ease;
    }}
    .kanban-column.drag-over {{
        box-shadow: 0 0 0 2px var(--bs-primary) !important;
    }}
</style>

@code {{
    [Parameter] public {name}_App_KanbanBoard.KanbanColumnModel Column {{ get; set; }} = new();
    [Parameter] public List<{name}_App_KanbanBoard.KanbanCardModel> Cards {{ get; set; }} = new();
    [Parameter] public EventCallback<(Guid cardId, string columnId)> OnCardDrop {{ get; set; }}
    [Parameter] public EventCallback<Guid> OnCardClick {{ get; set; }}
    [Parameter] public EventCallback OnAddCard {{ get; set; }}

    private bool _isDragOver = false;

    private bool IsAtWipLimit => Column.WipLimit > 0 && Cards.Count >= Column.WipLimit;

    private void HandleDragOver(DragEventArgs e) {{
        _isDragOver = true;
    }}

    private async Task HandleDrop(DragEventArgs e) {{
        _isDragOver = false;
        // The card ID is passed via the drag event data
        // In a real implementation, you'd use JS interop or a state service
        await OnCardDrop.InvokeAsync((Guid.Empty, Column.Id));
    }}
}}
";
}

#endregion

// ---- PagePatterns.KanbanBoard.Card ----

#region PagePatterns - KanbanBoard (Card)
// Part of PagePatterns.KanbanBoard: generator methods split out of the PagePatterns.KanbanBoard section above, now merged into FreeManager.App.PagePatterns.cs (CardEditor, KanbanCard).

public static partial class PagePatterns
{
    private static string GetKanbanBoard_CardEditor(string name, DataObjects.EntityDefinition? entity) => $@"@* {name} - CardEditor.razor *@
@* Generated by Page Pattern: KanbanBoard *@

<div class=""modal fade show d-block"" tabindex=""-1"" style=""background: rgba(0,0,0,0.5);"">
    <div class=""modal-dialog modal-lg"">
        <div class=""modal-content"">
            <div class=""modal-header"">
                <h5 class=""modal-title"">
                    <i class=""fa-solid @(IsNew ? ""fa-plus"" : ""fa-pen"") me-2""></i>
                    @(IsNew ? ""New Card"" : ""Edit Card"")
                </h5>
                <button type=""button"" class=""btn-close"" @onclick=""Cancel""></button>
            </div>

            <div class=""modal-body"">
                <div class=""row"">
                    <div class=""col-md-8"">
                        @* Title *@
                        <div class=""mb-3"">
                            <label class=""form-label"">Title <span class=""text-danger"">*</span></label>
                            <input type=""text"" class=""form-control"" @bind=""Card.Title"" placeholder=""Enter card title..."" />
                        </div>

                        @* Description *@
                        <div class=""mb-3"">
                            <label class=""form-label"">Description</label>
                            <textarea class=""form-control"" rows=""4"" @bind=""Card.Description"" placeholder=""Add a detailed description...""></textarea>
                        </div>

                        @* Labels *@
                        <div class=""mb-3"">
                            <label class=""form-label"">Labels</label>
                            <div class=""d-flex flex-wrap gap-1 mb-2"">
                                @foreach (var label in Card.Labels) {{
                                    <span class=""badge bg-primary"">
                                        @label
                                        <button type=""button"" class=""btn-close btn-close-white ms-1""
                                                style=""font-size: 0.6rem;""
                                                @onclick=""@(() => RemoveLabel(label))""></button>
                                    </span>
                                }}
                            </div>
                            <div class=""input-group input-group-sm"">
                                <input type=""text"" class=""form-control"" @bind=""_newLabel"" placeholder=""Add label...""
                                       @onkeypress=""HandleLabelKeyPress"" />
                                <button class=""btn btn-outline-secondary"" type=""button"" @onclick=""AddLabel"">
                                    <i class=""fa-solid fa-plus""></i>
                                </button>
                            </div>
                        </div>
                    </div>

                    <div class=""col-md-4"">
                        @* Column *@
                        <div class=""mb-3"">
                            <label class=""form-label"">Column</label>
                            <select class=""form-select"" @bind=""Card.ColumnId"">
                                @foreach (var col in Columns) {{
                                    <option value=""@col.Id"">@col.Title</option>
                                }}
                            </select>
                        </div>

                        @* Swimlane *@
                        @if (Swimlanes.Any()) {{
                            <div class=""mb-3"">
                                <label class=""form-label"">Swimlane</label>
                                <select class=""form-select"" @bind=""Card.Swimlane"">
                                    <option value="""">(None)</option>
                                    @foreach (var lane in Swimlanes) {{
                                        <option value=""@lane"">@lane</option>
                                    }}
                                </select>
                            </div>
                        }}

                        @* Assignee *@
                        <div class=""mb-3"">
                            <label class=""form-label"">Assignee</label>
                            <input type=""text"" class=""form-control"" @bind=""Card.Assignee"" placeholder=""Enter name..."" />
                        </div>

                        @* Due Date *@
                        <div class=""mb-3"">
                            <label class=""form-label"">Due Date</label>
                            <input type=""date"" class=""form-control"" @bind=""Card.DueDate"" />
                        </div>

                        @* Priority *@
                        <div class=""mb-3"">
                            <label class=""form-label"">Priority</label>
                            <select class=""form-select"" @bind=""Card.Priority"">
                                <option value=""Low"">Low</option>
                                <option value=""Medium"">Medium</option>
                                <option value=""High"">High</option>
                            </select>
                        </div>
                    </div>
                </div>
            </div>

            <div class=""modal-footer d-flex justify-content-between"">
                <div>
                    @if (!IsNew) {{
                        <button type=""button"" class=""btn btn-outline-danger"" @onclick=""Delete"">
                            <i class=""fa-solid fa-trash me-1""></i>Delete
                        </button>
                    }}
                </div>
                <div class=""d-flex gap-2"">
                    <button type=""button"" class=""btn btn-secondary"" @onclick=""Cancel"">Cancel</button>
                    <button type=""button"" class=""btn btn-primary"" @onclick=""Save"" disabled=""@(!IsValid)"">
                        <i class=""fa-solid fa-check me-1""></i>Save
                    </button>
                </div>
            </div>
        </div>
    </div>
</div>

@code {{
    [Parameter] public {name}_App_KanbanBoard.KanbanCardModel Card {{ get; set; }} = new();
    [Parameter] public List<{name}_App_KanbanBoard.KanbanColumnModel> Columns {{ get; set; }} = new();
    [Parameter] public List<string> Swimlanes {{ get; set; }} = new();
    [Parameter] public EventCallback<{name}_App_KanbanBoard.KanbanCardModel> OnSave {{ get; set; }}
    [Parameter] public EventCallback<Guid> OnDelete {{ get; set; }}
    [Parameter] public EventCallback OnCancel {{ get; set; }}

    private string _newLabel = string.Empty;

    private bool IsNew => Card.CreatedAt == default;
    private bool IsValid => !string.IsNullOrWhiteSpace(Card.Title);

    private async Task Save() {{
        if (IsValid) {{
            await OnSave.InvokeAsync(Card);
        }}
    }}

    private async Task Delete() {{
        await OnDelete.InvokeAsync(Card.Id);
    }}

    private async Task Cancel() {{
        await OnCancel.InvokeAsync();
    }}

    private void AddLabel() {{
        if (!string.IsNullOrWhiteSpace(_newLabel) && !Card.Labels.Contains(_newLabel)) {{
            Card.Labels.Add(_newLabel);
            _newLabel = string.Empty;
        }}
    }}

    private void RemoveLabel(string label) {{
        Card.Labels.Remove(label);
    }}

    private void HandleLabelKeyPress(KeyboardEventArgs e) {{
        if (e.Key == ""Enter"") {{
            AddLabel();
        }}
    }}
}}
";

    // ============================================================
    // KANBAN BOARD - Main Container
    // ============================================================

    private static string GetKanbanBoard_KanbanCard(string name, DataObjects.EntityDefinition? entity) => $@"@* {name} - KanbanCard.razor *@
@* Generated by Page Pattern: KanbanBoard *@

<div class=""kanban-card card mb-2 @(_isDragging ? ""dragging"" : """")""
     draggable=""true""
     @ondragstart=""HandleDragStart""
     @ondragend=""HandleDragEnd""
     @onclick=""@(() => OnClick.InvokeAsync())"">

    <div class=""card-body p-2"">
        @* Labels *@
        @if (Card.Labels.Any()) {{
            <div class=""d-flex flex-wrap gap-1 mb-2"">
                @foreach (var label in Card.Labels) {{
                    <span class=""badge"" style=""@GetLabelStyle(label)"">@label</span>
                }}
            </div>
        }}

        @* Title *@
        <h6 class=""card-title mb-2"" style=""font-size: 0.9rem;"">@Card.Title</h6>

        @* Description preview *@
        @if (!string.IsNullOrEmpty(Card.Description)) {{
            <p class=""card-text text-muted small mb-2"" style=""display: -webkit-box; -webkit-line-clamp: 2; -webkit-box-orient: vertical; overflow: hidden;"">
                @Card.Description
            </p>
        }}

        @* Footer with metadata *@
        <div class=""d-flex justify-content-between align-items-center"">
            <div class=""d-flex gap-2 align-items-center"">
                @* Priority indicator *@
                <span class=""badge @GetPriorityClass()"" title=""@Card.Priority priority"">
                    <i class=""fa-solid @GetPriorityIcon()""></i>
                </span>

                @* Due date *@
                @if (Card.DueDate.HasValue) {{
                    <span class=""small @(IsOverdue ? ""text-danger"" : ""text-muted"")"" title=""Due date"">
                        <i class=""fa-regular fa-calendar me-1""></i>
                        @Card.DueDate.Value.ToString(""MMM dd"")
                    </span>
                }}
            </div>

            @* Assignee avatar *@
            @if (!string.IsNullOrEmpty(Card.Assignee)) {{
                <span class=""avatar-sm rounded-circle bg-primary text-white d-flex align-items-center justify-content-center""
                      style=""width: 24px; height: 24px; font-size: 0.7rem;""
                      title=""@Card.Assignee"">
                    @Card.Assignee.Substring(0, 1).ToUpper()
                </span>
            }}
        </div>
    </div>
</div>

<style>
    .kanban-card {{
        cursor: grab;
        transition: transform 0.2s ease, box-shadow 0.2s ease;
        border: 1px solid var(--bs-gray-200);
    }}
    .kanban-card:hover {{
        box-shadow: 0 2px 8px rgba(0,0,0,0.12);
        transform: translateY(-1px);
    }}
    .kanban-card.dragging {{
        opacity: 0.5;
        cursor: grabbing;
        transform: rotate(2deg);
    }}
    .kanban-card:active {{
        cursor: grabbing;
    }}
</style>

@code {{
    [Parameter] public {name}_App_KanbanBoard.KanbanCardModel Card {{ get; set; }} = new();
    [Parameter] public EventCallback OnClick {{ get; set; }}

    private bool _isDragging = false;

    private bool IsOverdue => Card.DueDate.HasValue && Card.DueDate.Value < DateTime.Today;

    private void HandleDragStart(DragEventArgs e) {{
        _isDragging = true;
        // Store card ID for drop handling
        // In production, use JS interop or a state service to pass the ID
    }}

    private void HandleDragEnd(DragEventArgs e) {{
        _isDragging = false;
    }}

    private string GetPriorityClass() => Card.Priority switch {{
        ""High"" => ""bg-danger"",
        ""Medium"" => ""bg-warning text-dark"",
        ""Low"" => ""bg-success"",
        _ => ""bg-secondary""
    }};

    private string GetPriorityIcon() => Card.Priority switch {{
        ""High"" => ""fa-arrow-up"",
        ""Medium"" => ""fa-minus"",
        ""Low"" => ""fa-arrow-down"",
        _ => ""fa-minus""
    }};

    private string GetLabelStyle(string label) {{
        // Simple hash-based color generation for labels
        int hash = label.GetHashCode();
        string[] colors = new[] {{ ""#e91e63"", ""#9c27b0"", ""#673ab7"", ""#3f51b5"", ""#2196f3"", ""#00bcd4"", ""#009688"", ""#4caf50"", ""#ff9800"", ""#ff5722"" }};
        string color = colors[Math.Abs(hash) % colors.Length];
        return $""background-color: {{color}}; color: white;"";
    }}
}}
";

    // ============================================================
    // KANBAN COLUMN - Column Component with WIP Limits
    // ============================================================
}

#endregion

// ---- PagePatterns.QuickStartWizard ----

#region PagePatterns - QuickStartWizard
// ============================================================================
// PAGE PATTERN: QUICK START WIZARD
// Multi-step wizard for guided setup or onboarding.
//
// Generated Files:
// - {ProjectName}Wizard.App.razor (main wizard container with navigation)
// - {ProjectName}WizardStep1.App.razor (first step - welcome/intro)
// - {ProjectName}WizardStep2.App.razor (second step - configuration)
// - {ProjectName}WizardStep3.App.razor (third step - preferences)
// - {ProjectName}WizardComplete.App.razor (completion summary)
// ============================================================================

public static partial class PagePatterns
{
    /// <summary>
    /// Generate QuickStartWizard files for the given project.
    /// </summary>
    public static List<(string FileName, string FileType, string Content)> GetQuickStartWizardFiles(
        string projectName,
        DataObjects.EntityDefinition? entity = null){
        List<(string, string, string)> files = new();

        files.Add(($"{projectName}.App.Wizard.razor", "RazorComponent", GetQuickStartWizard_Wizard(projectName, entity)));
        files.Add(($"{projectName}.App.WizardStep1.razor", "RazorComponent", GetQuickStartWizard_Step1(projectName, entity)));
        files.Add(($"{projectName}.App.WizardStep2.razor", "RazorComponent", GetQuickStartWizard_Step2(projectName, entity)));
        files.Add(($"{projectName}.App.WizardStep3.razor", "RazorComponent", GetQuickStartWizard_Step3(projectName, entity)));
        files.Add(($"{projectName}.App.WizardComplete.razor", "RazorComponent", GetQuickStartWizard_Complete(projectName, entity)));

        return files;
    }

    // ============================================================
    // COMPLETE - Completion Summary
    // ============================================================

    private static string GetQuickStartWizard_Complete(string name, DataObjects.EntityDefinition? entity) => $@"@* {name}WizardComplete.App.razor - Wizard Complete *@
@* Generated by Page Pattern: QuickStartWizard *@

<div class=""text-center py-4"">
    <div class=""mb-4"">
        <div class=""rounded-circle bg-success d-inline-flex align-items-center justify-content-center""
             style=""width: 80px; height: 80px;"">
            <i class=""fa-solid fa-check fa-3x text-white""></i>
        </div>
    </div>

    <h2 class=""text-success mb-3"">Setup Complete!</h2>
    <p class=""lead text-muted mb-4"">
        Great job, <strong>@State.UserName</strong>! You're all set up and ready to go.
    </p>

    <!-- Summary Card -->
    <div class=""card text-start mx-auto"" style=""max-width: 500px;"">
        <div class=""card-header bg-light"">
            <i class=""fa-solid fa-clipboard-list me-2""></i>Your Settings Summary
        </div>
        <div class=""card-body"">
            <div class=""row g-3"">
                <div class=""col-6"">
                    <small class=""text-muted"">Name</small>
                    <div class=""fw-semibold"">@State.UserName</div>
                </div>
                @if (!string.IsNullOrEmpty(State.OrganizationName)) {{
                    <div class=""col-6"">
                        <small class=""text-muted"">Organization</small>
                        <div class=""fw-semibold"">@State.OrganizationName</div>
                    </div>
                }}
                <div class=""col-6"">
                    <small class=""text-muted"">Time Zone</small>
                    <div class=""fw-semibold"">@State.TimeZone</div>
                </div>
                <div class=""col-6"">
                    <small class=""text-muted"">Date Format</small>
                    <div class=""fw-semibold"">@State.DateFormat</div>
                </div>
                <div class=""col-6"">
                    <small class=""text-muted"">Language</small>
                    <div class=""fw-semibold"">@GetLanguageDisplay(State.Language)</div>
                </div>
                <div class=""col-6"">
                    <small class=""text-muted"">Theme</small>
                    <div class=""fw-semibold text-capitalize"">@State.Theme</div>
                </div>
            </div>

            <hr />

            <div class=""d-flex gap-3"">
                <div class=""d-flex align-items-center"">
                    @if (State.EnableNotifications) {{
                        <i class=""fa-solid fa-check-circle text-success me-1""></i>
                    }} else {{
                        <i class=""fa-solid fa-times-circle text-muted me-1""></i>
                    }}
                    <small>Notifications</small>
                </div>
                <div class=""d-flex align-items-center"">
                    @if (State.EnableEmailAlerts) {{
                        <i class=""fa-solid fa-check-circle text-success me-1""></i>
                    }} else {{
                        <i class=""fa-solid fa-times-circle text-muted me-1""></i>
                    }}
                    <small>Email Alerts</small>
                </div>
            </div>
        </div>
    </div>

    <!-- Next Steps -->
    <div class=""mt-4"">
        <h5 class=""text-muted mb-3"">What's Next?</h5>
        <div class=""row g-3 justify-content-center"">
            <div class=""col-md-4"">
                <div class=""card h-100 border-0 bg-light"">
                    <div class=""card-body text-center"">
                        <i class=""fa-solid fa-book fa-2x text-primary mb-2""></i>
                        <h6>Documentation</h6>
                        <small class=""text-muted"">Learn more about features</small>
                    </div>
                </div>
            </div>
            <div class=""col-md-4"">
                <div class=""card h-100 border-0 bg-light"">
                    <div class=""card-body text-center"">
                        <i class=""fa-solid fa-play-circle fa-2x text-primary mb-2""></i>
                        <h6>Quick Tour</h6>
                        <small class=""text-muted"">Take an interactive tour</small>
                    </div>
                </div>
            </div>
            <div class=""col-md-4"">
                <div class=""card h-100 border-0 bg-light"">
                    <div class=""card-body text-center"">
                        <i class=""fa-solid fa-headset fa-2x text-primary mb-2""></i>
                        <h6>Support</h6>
                        <small class=""text-muted"">Get help when needed</small>
                    </div>
                </div>
            </div>
        </div>
    </div>
</div>

@code {{
    [Parameter] public {name}WizardState State {{ get; set; }} = new();

    private string GetLanguageDisplay(string code) => code switch
    {{
        ""en"" => ""English"",
        ""es"" => ""Spanish"",
        ""fr"" => ""French"",
        ""de"" => ""German"",
        ""pt"" => ""Portuguese"",
        ""ja"" => ""Japanese"",
        ""zh"" => ""Chinese"",
        _ => code
    }};
}}
";

    // ============================================================
    // STEP 1 - Welcome / Introduction
    // ============================================================

    private static string GetQuickStartWizard_Wizard(string name, DataObjects.EntityDefinition? entity) => $@"@* {name}Wizard.App.razor - QuickStart Wizard *@
@* Generated by Page Pattern: QuickStartWizard *@
@page ""/{name}Wizard""
@page ""/{{TenantCode}}/{name}Wizard""
@implements IDisposable
@inject BlazorDataModel Model
@inject NavigationManager Nav

@if (Model.Loaded && Model.LoggedIn) {{
    <div class=""container py-4"">
        <div class=""row justify-content-center"">
            <div class=""col-lg-8"">
                <!-- Header -->
                <div class=""text-center mb-4"">
                    <h1 class=""display-6"">
                        <i class=""fa-solid fa-wand-magic-sparkles me-2 text-primary""></i>{name} Setup Wizard
                    </h1>
                    <p class=""text-muted"">Complete these steps to get started</p>
                </div>

                <!-- Progress Stepper -->
                <div class=""card shadow-sm mb-4"">
                    <div class=""card-body py-4"">
                        <div class=""d-flex justify-content-between position-relative"">
                            <!-- Progress Line -->
                            <div class=""position-absolute top-50 start-0 end-0"" style=""height: 2px; z-index: 0;"">
                                <div class=""bg-light w-100 h-100""></div>
                                <div class=""bg-primary h-100 position-absolute top-0 start-0""
                                     style=""width: @((_currentStep - 1) * 33.33)%; transition: width 0.3s ease;""></div>
                            </div>

                            @for (int i = 1; i <= 4; i++) {{
                                var step = i;
                                var isCompleted = step < _currentStep;
                                var isCurrent = step == _currentStep;
                                var stepClass = isCompleted ? ""bg-primary text-white"" :
                                               isCurrent ? ""bg-primary text-white"" :
                                               ""bg-light text-muted"";
                                <div class=""text-center position-relative"" style=""z-index: 1;"">
                                    <div class=""rounded-circle d-inline-flex align-items-center justify-content-center @stepClass""
                                         style=""width: 40px; height: 40px; transition: all 0.3s ease;"">
                                        @if (isCompleted) {{
                                            <i class=""fa-solid fa-check""></i>
                                        }} else {{
                                            <span>@step</span>
                                        }}
                                    </div>
                                    <div class=""small mt-1 @(isCurrent || isCompleted ? ""text-primary fw-semibold"" : ""text-muted"")"">
                                        @GetStepLabel(step)
                                    </div>
                                </div>
                            }}
                        </div>
                    </div>
                </div>

                <!-- Step Content -->
                <div class=""card shadow-sm"">
                    <div class=""card-body p-4"">
                        @switch (_currentStep) {{
                            case 1:
                                <{name}_App_WizardStep1 State=""_wizardState"" OnValidated=""OnStepValidated"" />
                                break;
                            case 2:
                                <{name}_App_WizardStep2 State=""_wizardState"" OnValidated=""OnStepValidated"" />
                                break;
                            case 3:
                                <{name}_App_WizardStep3 State=""_wizardState"" OnValidated=""OnStepValidated"" />
                                break;
                            case 4:
                                <{name}_App_WizardComplete State=""_wizardState"" />
                                break;
                        }}
                    </div>

                    <!-- Navigation Footer -->
                    <div class=""card-footer bg-light"">
                        <div class=""d-flex justify-content-between align-items-center"">
                            <div>
                                @if (_currentStep > 1 && _currentStep < 4) {{
                                    <button class=""btn btn-outline-secondary"" @onclick=""PreviousStep"">
                                        <i class=""fa-solid fa-arrow-left me-1""></i>Back
                                    </button>
                                }}
                            </div>
                            <div class=""d-flex gap-2"">
                                @if (_currentStep < 3) {{
                                    <button class=""btn btn-link text-muted"" @onclick=""SkipStep"">
                                        Skip this step
                                    </button>
                                }}
                                @if (_currentStep < 4) {{
                                    <button class=""btn btn-primary"" @onclick=""NextStep"" disabled=""@(!_stepValid)"">
                                        @if (_currentStep == 3) {{
                                            <span>Complete Setup<i class=""fa-solid fa-check ms-1""></i></span>
                                        }} else {{
                                            <span>Next<i class=""fa-solid fa-arrow-right ms-1""></i></span>
                                        }}
                                    </button>
                                }} else {{
                                    <button class=""btn btn-success"" @onclick=""FinishWizard"">
                                        <i class=""fa-solid fa-rocket me-1""></i>Get Started
                                    </button>
                                }}
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>
}}

@code {{
    private int _currentStep = 1;
    private bool _stepValid = false;
    private {name}WizardState _wizardState = new();

    public void Dispose() {{ Model.OnChange -= StateHasChanged; }}

    protected override void OnInitialized()
    {{
        Model.View = ""{name.ToLower()}wizard"";
        Model.OnChange += StateHasChanged;
        LoadState();
    }}

    private string GetStepLabel(int step) => step switch
    {{
        1 => ""Welcome"",
        2 => ""Configure"",
        3 => ""Preferences"",
        4 => ""Complete"",
        _ => """"
    }};

    private void OnStepValidated(bool isValid)
    {{
        _stepValid = isValid;
        StateHasChanged();
    }}

    private void NextStep()
    {{
        if (_stepValid && _currentStep < 4)
        {{
            _currentStep++;
            _stepValid = _currentStep == 4;
            SaveState();
            StateHasChanged();
        }}
    }}

    private void PreviousStep()
    {{
        if (_currentStep > 1)
        {{
            _currentStep--;
            _stepValid = true;
            StateHasChanged();
        }}
    }}

    private void SkipStep()
    {{
        if (_currentStep < 3)
        {{
            _currentStep++;
            _stepValid = false;
            SaveState();
            StateHasChanged();
        }}
    }}

    private void FinishWizard()
    {{
        ClearState();
        Nav.NavigateTo(""/"");
    }}

    private void LoadState()
    {{
        // Load persisted wizard state from local storage or session
        // This enables resuming the wizard if the user navigates away
        var savedState = Model.GetStoredValue(""{name}WizardState"");
        if (!string.IsNullOrEmpty(savedState))
        {{
            try
            {{
                var state = System.Text.Json.JsonSerializer.Deserialize<{name}WizardState>(savedState);
                if (state != null)
                {{
                    _wizardState = state;
                    _currentStep = state.LastCompletedStep + 1;
                    if (_currentStep > 4) _currentStep = 4;
                }}
            }}
            catch {{ /* Ignore deserialization errors */ }}
        }}
    }}

    private void SaveState()
    {{
        _wizardState.LastCompletedStep = _currentStep - 1;
        var json = System.Text.Json.JsonSerializer.Serialize(_wizardState);
        Model.SetStoredValue(""{name}WizardState"", json);
    }}

    private void ClearState()
    {{
        Model.RemoveStoredValue(""{name}WizardState"");
    }}
}}

@* Wizard State Class *@
@code {{
    public class {name}WizardState
    {{
        public int LastCompletedStep {{ get; set; }} = 0;

        // Step 1: Welcome
        public string UserName {{ get; set; }} = string.Empty;
        public string OrganizationName {{ get; set; }} = string.Empty;

        // Step 2: Configuration
        public string TimeZone {{ get; set; }} = ""UTC"";
        public string DateFormat {{ get; set; }} = ""MM/dd/yyyy"";
        public string Language {{ get; set; }} = ""en"";

        // Step 3: Preferences
        public bool EnableNotifications {{ get; set; }} = true;
        public bool EnableEmailAlerts {{ get; set; }} = true;
        public string Theme {{ get; set; }} = ""light"";
    }}
}}
";
}

#endregion

// ---- PagePatterns.QuickStartWizard.Steps ----

#region PagePatterns - QuickStartWizard (Steps)
// Part of PagePatterns.QuickStartWizard: generator methods split out of the PagePatterns.QuickStartWizard section above, now merged into FreeManager.App.PagePatterns.cs (Step1, Step2, Step3).

public static partial class PagePatterns
{
    private static string GetQuickStartWizard_Step1(string name, DataObjects.EntityDefinition? entity) => $@"@* {name}WizardStep1.App.razor - Wizard Step 1: Welcome *@
@* Generated by Page Pattern: QuickStartWizard *@

<div class=""text-center mb-4"">
    <div class=""display-1 text-primary mb-3"">
        <i class=""fa-solid fa-hand-wave""></i>
    </div>
    <h2>Welcome to {name}!</h2>
    <p class=""text-muted lead"">Let's get you set up. This will only take a few minutes.</p>
</div>

<EditForm Model=""State"" OnValidSubmit=""Validate"">
    <DataAnnotationsValidator />

    <div class=""row g-3"">
        <div class=""col-md-6"">
            <label class=""form-label"">Your Name <span class=""text-danger"">*</span></label>
            <div class=""input-group"">
                <span class=""input-group-text""><i class=""fa-solid fa-user""></i></span>
                <input type=""text"" class=""form-control"" placeholder=""Enter your name""
                       @bind=""State.UserName"" @bind:event=""oninput"" @bind:after=""Validate"" />
            </div>
            <ValidationMessage For=""@(() => State.UserName)"" />
        </div>

        <div class=""col-md-6"">
            <label class=""form-label"">Organization Name</label>
            <div class=""input-group"">
                <span class=""input-group-text""><i class=""fa-solid fa-building""></i></span>
                <input type=""text"" class=""form-control"" placeholder=""Your company or team""
                       @bind=""State.OrganizationName"" @bind:event=""oninput"" />
            </div>
        </div>
    </div>

    <div class=""alert alert-info mt-4 mb-0"">
        <i class=""fa-solid fa-lightbulb me-2""></i>
        <strong>Tip:</strong> You can always update these settings later from your profile page.
    </div>
</EditForm>

@code {{
    [Parameter] public {name}WizardState State {{ get; set; }} = new();
    [Parameter] public EventCallback<bool> OnValidated {{ get; set; }}

    protected override void OnInitialized()
    {{
        Validate();
    }}

    private async Task Validate()
    {{
        bool isValid = !string.IsNullOrWhiteSpace(State.UserName);
        await OnValidated.InvokeAsync(isValid);
    }}
}}
";

    // ============================================================
    // STEP 2 - Configuration Settings
    // ============================================================

    private static string GetQuickStartWizard_Step2(string name, DataObjects.EntityDefinition? entity) => $@"@* {name}WizardStep2.App.razor - Wizard Step 2: Configuration *@
@* Generated by Page Pattern: QuickStartWizard *@

<div class=""mb-4"">
    <h3><i class=""fa-solid fa-gear me-2 text-primary""></i>Configuration</h3>
    <p class=""text-muted"">Set up your regional and localization preferences.</p>
</div>

<div class=""row g-4"">
    <div class=""col-md-6"">
        <div class=""card h-100"">
            <div class=""card-body"">
                <h5 class=""card-title"">
                    <i class=""fa-solid fa-globe me-2 text-muted""></i>Time Zone
                </h5>
                <select class=""form-select"" @bind=""State.TimeZone"" @bind:after=""Validate"">
                    <option value=""UTC"">UTC (Coordinated Universal Time)</option>
                    <option value=""America/New_York"">Eastern Time (ET)</option>
                    <option value=""America/Chicago"">Central Time (CT)</option>
                    <option value=""America/Denver"">Mountain Time (MT)</option>
                    <option value=""America/Los_Angeles"">Pacific Time (PT)</option>
                    <option value=""Europe/London"">London (GMT/BST)</option>
                    <option value=""Europe/Paris"">Paris (CET/CEST)</option>
                    <option value=""Asia/Tokyo"">Tokyo (JST)</option>
                    <option value=""Australia/Sydney"">Sydney (AEST/AEDT)</option>
                </select>
            </div>
        </div>
    </div>

    <div class=""col-md-6"">
        <div class=""card h-100"">
            <div class=""card-body"">
                <h5 class=""card-title"">
                    <i class=""fa-solid fa-calendar me-2 text-muted""></i>Date Format
                </h5>
                <select class=""form-select"" @bind=""State.DateFormat"" @bind:after=""Validate"">
                    <option value=""MM/dd/yyyy"">MM/DD/YYYY (12/31/2024)</option>
                    <option value=""dd/MM/yyyy"">DD/MM/YYYY (31/12/2024)</option>
                    <option value=""yyyy-MM-dd"">YYYY-MM-DD (2024-12-31)</option>
                    <option value=""dd.MM.yyyy"">DD.MM.YYYY (31.12.2024)</option>
                </select>
            </div>
        </div>
    </div>

    <div class=""col-md-6"">
        <div class=""card h-100"">
            <div class=""card-body"">
                <h5 class=""card-title"">
                    <i class=""fa-solid fa-language me-2 text-muted""></i>Language
                </h5>
                <select class=""form-select"" @bind=""State.Language"" @bind:after=""Validate"">
                    <option value=""en"">English</option>
                    <option value=""es"">Español (Spanish)</option>
                    <option value=""fr"">Français (French)</option>
                    <option value=""de"">Deutsch (German)</option>
                    <option value=""pt"">Português (Portuguese)</option>
                    <option value=""ja"">日本語 (Japanese)</option>
                    <option value=""zh"">中文 (Chinese)</option>
                </select>
            </div>
        </div>
    </div>
</div>

@code {{
    [Parameter] public {name}WizardState State {{ get; set; }} = new();
    [Parameter] public EventCallback<bool> OnValidated {{ get; set; }}

    protected override void OnInitialized()
    {{
        Validate();
    }}

    private async Task Validate()
    {{
        // Step 2 is always valid as all fields have defaults
        bool isValid = !string.IsNullOrEmpty(State.TimeZone) &&
                       !string.IsNullOrEmpty(State.DateFormat) &&
                       !string.IsNullOrEmpty(State.Language);
        await OnValidated.InvokeAsync(isValid);
    }}
}}
";

    // ============================================================
    // STEP 3 - Preferences
    // ============================================================

    private static string GetQuickStartWizard_Step3(string name, DataObjects.EntityDefinition? entity) => $@"@* {name}WizardStep3.App.razor - Wizard Step 3: Preferences *@
@* Generated by Page Pattern: QuickStartWizard *@

<div class=""mb-4"">
    <h3><i class=""fa-solid fa-sliders me-2 text-primary""></i>Preferences</h3>
    <p class=""text-muted"">Customize your experience with these optional settings.</p>
</div>

<div class=""row g-4"">
    <!-- Notifications -->
    <div class=""col-12"">
        <div class=""card"">
            <div class=""card-body"">
                <h5 class=""card-title mb-3"">
                    <i class=""fa-solid fa-bell me-2 text-muted""></i>Notifications
                </h5>

                <div class=""d-flex justify-content-between align-items-center py-2 border-bottom"">
                    <div>
                        <strong>In-app Notifications</strong>
                        <p class=""text-muted mb-0 small"">Receive notifications within the application</p>
                    </div>
                    <div class=""form-check form-switch"">
                        <input class=""form-check-input"" type=""checkbox"" role=""switch""
                               @bind=""State.EnableNotifications"" style=""width: 3em; height: 1.5em;"" />
                    </div>
                </div>

                <div class=""d-flex justify-content-between align-items-center py-2"">
                    <div>
                        <strong>Email Alerts</strong>
                        <p class=""text-muted mb-0 small"">Get important updates sent to your email</p>
                    </div>
                    <div class=""form-check form-switch"">
                        <input class=""form-check-input"" type=""checkbox"" role=""switch""
                               @bind=""State.EnableEmailAlerts"" style=""width: 3em; height: 1.5em;"" />
                    </div>
                </div>
            </div>
        </div>
    </div>

    <!-- Theme -->
    <div class=""col-12"">
        <div class=""card"">
            <div class=""card-body"">
                <h5 class=""card-title mb-3"">
                    <i class=""fa-solid fa-palette me-2 text-muted""></i>Appearance
                </h5>

                <div class=""row g-3"">
                    <div class=""col-md-4"">
                        <div class=""card @(State.Theme == ""light"" ? ""border-primary"" : """")""
                             style=""cursor: pointer;"" @onclick=""@(() => SetTheme(""light""))"">
                            <div class=""card-body text-center py-4"">
                                <i class=""fa-solid fa-sun fa-2x mb-2 @(State.Theme == ""light"" ? ""text-primary"" : ""text-muted"")""></i>
                                <div class=""@(State.Theme == ""light"" ? ""fw-semibold"" : """")"">Light</div>
                            </div>
                        </div>
                    </div>
                    <div class=""col-md-4"">
                        <div class=""card @(State.Theme == ""dark"" ? ""border-primary"" : """")""
                             style=""cursor: pointer;"" @onclick=""@(() => SetTheme(""dark""))"">
                            <div class=""card-body text-center py-4"">
                                <i class=""fa-solid fa-moon fa-2x mb-2 @(State.Theme == ""dark"" ? ""text-primary"" : ""text-muted"")""></i>
                                <div class=""@(State.Theme == ""dark"" ? ""fw-semibold"" : """")"">Dark</div>
                            </div>
                        </div>
                    </div>
                    <div class=""col-md-4"">
                        <div class=""card @(State.Theme == ""auto"" ? ""border-primary"" : """")""
                             style=""cursor: pointer;"" @onclick=""@(() => SetTheme(""auto""))"">
                            <div class=""card-body text-center py-4"">
                                <i class=""fa-solid fa-circle-half-stroke fa-2x mb-2 @(State.Theme == ""auto"" ? ""text-primary"" : ""text-muted"")""></i>
                                <div class=""@(State.Theme == ""auto"" ? ""fw-semibold"" : """")"">Auto</div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>
</div>

@code {{
    [Parameter] public {name}WizardState State {{ get; set; }} = new();
    [Parameter] public EventCallback<bool> OnValidated {{ get; set; }}

    protected override void OnInitialized()
    {{
        Validate();
    }}

    private void SetTheme(string theme)
    {{
        State.Theme = theme;
        StateHasChanged();
    }}

    private async Task Validate()
    {{
        // Step 3 is always valid (all preferences have defaults)
        await OnValidated.InvokeAsync(true);
    }}
}}
";

    // ============================================================
    // WIZARD CONTAINER - Main wizard with step navigation
    // ============================================================
}

#endregion

// ---- PagePatterns.SettingsHub ----

#region PagePatterns - SettingsHub
// ============================================================================
// PAGE PATTERN: SETTINGS HUB
// Multi-section settings page with sidebar navigation
//
// Generated Files:
// - {projectName}SettingsHub.App.razor (main container with layout)
// - {projectName}SettingsSidebar.App.razor (collapsible sidebar with section links)
// - {projectName}ProfileSettings.App.razor (user profile settings section)
// - {projectName}NotificationSettings.App.razor (notification preferences section)
//
// Key Features:
// - Collapsible sidebar
// - Section highlighting
// - Role-based visibility
// - Unsaved warning
// ============================================================================

public static partial class PagePatterns
{
    /// <summary>
    /// Generate SettingsHub files for the given project.
    /// </summary>
    public static List<(string FileName, string FileType, string Content)> GetSettingsHubFiles(
        string projectName,
        DataObjects.EntityDefinition? entity = null){
        List<(string, string, string)> files = new();

        files.Add(($"{projectName}.App.SettingsHub.razor", "RazorComponent", GetSettingsHub_Main(projectName, entity)));
        files.Add(($"{projectName}.App.SettingsSidebar.razor", "RazorComponent", GetSettingsHub_Sidebar(projectName, entity)));
        files.Add(($"{projectName}.App.ProfileSettings.razor", "RazorComponent", GetSettingsHub_ProfileSettings(projectName, entity)));
        files.Add(($"{projectName}.App.NotificationSettings.razor", "RazorComponent", GetSettingsHub_NotificationSettings(projectName, entity)));

        return files;
    }

    // ============================================================
    // SETTINGS HUB - MAIN CONTAINER
    // ============================================================

    private static string GetSettingsHub_Main(string name, DataObjects.EntityDefinition? entity) => $@"@* {name}SettingsHub.App.razor - Settings hub main container *@
@* Generated by SettingsHub Page Pattern *@
@page ""/Settings""
@page ""/{{TenantCode}}/Settings""
@implements IDisposable
@inject BlazorDataModel Model
@inject NavigationManager Navigation
@inject IJSRuntime JS

@if (Model.Loaded && Model.LoggedIn && Model.View == _pageName) {{
    <div class=""container-fluid py-3"">
        <div class=""@Model.StickyMenuClass mb-3"">
            <div class=""d-flex justify-content-between align-items-center"">
                <h1 class=""page-title mb-0"">
                    <i class=""fa-solid fa-sliders me-2""></i>Settings
                </h1>
                @if (_hasUnsavedChanges) {{
                    <span class=""badge bg-warning text-dark"">
                        <i class=""fa-solid fa-exclamation-triangle me-1""></i>Unsaved Changes
                    </span>
                }}
            </div>
        </div>

        <div class=""row"">
            @* Sidebar *@
            <div class=""col-md-3 col-lg-2"">
                <{name}_App_SettingsSidebar
                    ActiveSection=""@_activeSection""
                    OnSectionChange=""HandleSectionChange""
                    IsCollapsed=""@_sidebarCollapsed""
                    OnToggleCollapse=""ToggleSidebar""
                    UserRoles=""@Model.CurrentUser?.Roles"" />
            </div>

            @* Content Area *@
            <div class=""@(_sidebarCollapsed ? ""col-md-11 col-lg-11"" : ""col-md-9 col-lg-10"")"">
                <div class=""card shadow-sm"">
                    <div class=""card-body"">
                        @switch (_activeSection) {{
                            case ""profile"":
                                <{name}_App_ProfileSettings
                                    OnChange=""MarkUnsaved""
                                    OnSave=""HandleSave"" />
                                break;
                            case ""notifications"":
                                <{name}_App_NotificationSettings
                                    OnChange=""MarkUnsaved""
                                    OnSave=""HandleSave"" />
                                break;
                            case ""security"":
                                <div class=""text-center py-5 text-muted"">
                                    <i class=""fa-solid fa-shield-halved fa-4x mb-3 opacity-50""></i>
                                    <h4>Security Settings</h4>
                                    <p>Security configuration options will appear here.</p>
                                </div>
                                break;
                            case ""preferences"":
                                <div class=""text-center py-5 text-muted"">
                                    <i class=""fa-solid fa-palette fa-4x mb-3 opacity-50""></i>
                                    <h4>Preferences</h4>
                                    <p>User preference options will appear here.</p>
                                </div>
                                break;
                            case ""admin"":
                                <div class=""text-center py-5 text-muted"">
                                    <i class=""fa-solid fa-user-shield fa-4x mb-3 opacity-50""></i>
                                    <h4>Admin Settings</h4>
                                    <p>Administrative configuration will appear here.</p>
                                </div>
                                break;
                            default:
                                <div class=""text-center py-5 text-muted"">
                                    <i class=""fa-solid fa-sliders fa-4x mb-3 opacity-50""></i>
                                    <h4>Select a section</h4>
                                    <p>Choose a settings category from the sidebar.</p>
                                </div>
                                break;
                        }}
                    </div>
                </div>
            </div>
        </div>
    </div>

    @* Unsaved Changes Warning Modal *@
    @if (_showUnsavedWarning) {{
        <div class=""modal fade show d-block"" tabindex=""-1"" style=""background: rgba(0,0,0,0.5);"">
            <div class=""modal-dialog"">
                <div class=""modal-content"">
                    <div class=""modal-header bg-warning"">
                        <h5 class=""modal-title"">
                            <i class=""fa-solid fa-exclamation-triangle me-2""></i>Unsaved Changes
                        </h5>
                    </div>
                    <div class=""modal-body"">
                        <p>You have unsaved changes. Do you want to save before leaving?</p>
                    </div>
                    <div class=""modal-footer"">
                        <button type=""button"" class=""btn btn-secondary"" @onclick=""DiscardChanges"">Discard</button>
                        <button type=""button"" class=""btn btn-primary"" @onclick=""SaveAndNavigate"">Save Changes</button>
                        <button type=""button"" class=""btn btn-outline-secondary"" @onclick=""CancelNavigation"">Cancel</button>
                    </div>
                </div>
            </div>
        </div>
    }}
}}

@code {{
    private string _pageName = ""settings"";
    private string _activeSection = ""profile"";
    private bool _sidebarCollapsed = false;
    private bool _hasUnsavedChanges = false;
    private bool _showUnsavedWarning = false;
    private string _pendingSection = """";

    public void Dispose() {{ Model.OnChange -= StateHasChanged; }}

    protected override void OnInitialized()
    {{
        Model.View = _pageName;
        Model.OnChange += StateHasChanged;
    }}

    private async Task HandleSectionChange(string section)
    {{
        if (_hasUnsavedChanges) {{
            _pendingSection = section;
            _showUnsavedWarning = true;
        }} else {{
            _activeSection = section;
        }}
        await Task.CompletedTask;
    }}

    private void ToggleSidebar()
    {{
        _sidebarCollapsed = !_sidebarCollapsed;
    }}

    private void MarkUnsaved()
    {{
        _hasUnsavedChanges = true;
    }}

    private async Task HandleSave()
    {{
        _hasUnsavedChanges = false;
        await Task.CompletedTask;
    }}

    private async Task DiscardChanges()
    {{
        _hasUnsavedChanges = false;
        _showUnsavedWarning = false;
        _activeSection = _pendingSection;
        _pendingSection = """";
        await Task.CompletedTask;
    }}

    private async Task SaveAndNavigate()
    {{
        await HandleSave();
        _showUnsavedWarning = false;
        _activeSection = _pendingSection;
        _pendingSection = """";
    }}

    private void CancelNavigation()
    {{
        _showUnsavedWarning = false;
        _pendingSection = """";
    }}
}}
";

    // ============================================================
    // SETTINGS HUB - NOTIFICATION SETTINGS
    // ============================================================

    private static string GetSettingsHub_Sidebar(string name, DataObjects.EntityDefinition? entity) => $@"@* {name}SettingsSidebar.App.razor - Collapsible sidebar *@
@* Generated by SettingsHub Page Pattern *@

<div class=""card shadow-sm @(IsCollapsed ? ""settings-sidebar-collapsed"" : """")"">
    <div class=""card-header d-flex justify-content-between align-items-center py-2"">
        @if (!IsCollapsed) {{
            <span class=""fw-bold""><i class=""fa-solid fa-cog me-2""></i>Settings</span>
        }}
        <button class=""btn btn-sm btn-link text-secondary p-0"" @onclick=""ToggleCollapse"" title=""@(IsCollapsed ? ""Expand"" : ""Collapse"")"">
            <i class=""fa-solid @(IsCollapsed ? ""fa-chevron-right"" : ""fa-chevron-left"")""></i>
        </button>
    </div>
    <div class=""list-group list-group-flush"">
        @* User Settings Section *@
        @if (!IsCollapsed) {{
            <div class=""list-group-item bg-light text-muted small fw-bold py-2"">
                <i class=""fa-solid fa-user me-2""></i>User Settings
            </div>
        }}

        <button type=""button""
                class=""list-group-item list-group-item-action @(ActiveSection == ""profile"" ? ""active"" : """")""
                @onclick=""@(() => SelectSection(""profile""))"">
            <i class=""fa-solid fa-id-card @(IsCollapsed ? """" : ""me-2"")""></i>
            @if (!IsCollapsed) {{ <span>Profile</span> }}
        </button>

        <button type=""button""
                class=""list-group-item list-group-item-action @(ActiveSection == ""notifications"" ? ""active"" : """")""
                @onclick=""@(() => SelectSection(""notifications""))"">
            <i class=""fa-solid fa-bell @(IsCollapsed ? """" : ""me-2"")""></i>
            @if (!IsCollapsed) {{ <span>Notifications</span> }}
        </button>

        <button type=""button""
                class=""list-group-item list-group-item-action @(ActiveSection == ""security"" ? ""active"" : """")""
                @onclick=""@(() => SelectSection(""security""))"">
            <i class=""fa-solid fa-shield-halved @(IsCollapsed ? """" : ""me-2"")""></i>
            @if (!IsCollapsed) {{ <span>Security</span> }}
        </button>

        <button type=""button""
                class=""list-group-item list-group-item-action @(ActiveSection == ""preferences"" ? ""active"" : """")""
                @onclick=""@(() => SelectSection(""preferences""))"">
            <i class=""fa-solid fa-palette @(IsCollapsed ? """" : ""me-2"")""></i>
            @if (!IsCollapsed) {{ <span>Preferences</span> }}
        </button>

        @* Admin Settings Section - Role-based visibility *@
        @if (HasAdminRole) {{
            @if (!IsCollapsed) {{
                <div class=""list-group-item bg-light text-muted small fw-bold py-2 mt-2"">
                    <i class=""fa-solid fa-user-shield me-2""></i>Administration
                </div>
            }}

            <button type=""button""
                    class=""list-group-item list-group-item-action @(ActiveSection == ""admin"" ? ""active"" : """")""
                    @onclick=""@(() => SelectSection(""admin""))"">
                <i class=""fa-solid fa-cogs @(IsCollapsed ? """" : ""me-2"")""></i>
                @if (!IsCollapsed) {{ <span>Admin Settings</span> }}
            </button>
        }}
    </div>
</div>

<style>
    .settings-sidebar-collapsed {{
        max-width: 60px;
    }}
    .settings-sidebar-collapsed .list-group-item {{
        text-align: center;
        padding-left: 0.5rem;
        padding-right: 0.5rem;
    }}
</style>

@code {{
    [Parameter] public string ActiveSection {{ get; set; }} = ""profile"";
    [Parameter] public EventCallback<string> OnSectionChange {{ get; set; }}
    [Parameter] public bool IsCollapsed {{ get; set; }} = false;
    [Parameter] public EventCallback OnToggleCollapse {{ get; set; }}
    [Parameter] public List<string>? UserRoles {{ get; set; }}

    private bool HasAdminRole => UserRoles?.Any(r =>
        r.Equals(""Admin"", StringComparison.OrdinalIgnoreCase) ||
        r.Equals(""Administrator"", StringComparison.OrdinalIgnoreCase) ||
        r.Equals(""SuperAdmin"", StringComparison.OrdinalIgnoreCase)) ?? false;

    private async Task SelectSection(string section)
    {{
        await OnSectionChange.InvokeAsync(section);
    }}

    private async Task ToggleCollapse()
    {{
        await OnToggleCollapse.InvokeAsync();
    }}
}}
";
}

#endregion

// ---- PagePatterns.SettingsHub.Sections ----

#region PagePatterns - SettingsHub (Sections)
// Part of PagePatterns.SettingsHub: generator methods split out of the PagePatterns.SettingsHub section above, now merged into FreeManager.App.PagePatterns.cs (NotificationSettings, ProfileSettings).

public static partial class PagePatterns
{
    private static string GetSettingsHub_NotificationSettings(string name, DataObjects.EntityDefinition? entity) => $@"@* {name}NotificationSettings.App.razor - Notification preferences *@
@* Generated by SettingsHub Page Pattern *@

<div class=""settings-section"">
    <div class=""d-flex justify-content-between align-items-center mb-4"">
        <div>
            <h4 class=""mb-1""><i class=""fa-solid fa-bell me-2""></i>Notification Settings</h4>
            <p class=""text-muted mb-0"">Configure how and when you receive notifications.</p>
        </div>
    </div>

    <div class=""card mb-4"">
        <div class=""card-header"">
            <h6 class=""mb-0""><i class=""fa-solid fa-envelope me-2""></i>Email Notifications</h6>
        </div>
        <div class=""card-body"">
            <div class=""form-check form-switch mb-3"">
                <input class=""form-check-input"" type=""checkbox"" id=""emailUpdates"" @bind=""_settings.EmailUpdates"" @bind:after=""NotifyChange"" />
                <label class=""form-check-label"" for=""emailUpdates"">
                    <strong>Product Updates</strong>
                    <br /><small class=""text-muted"">Receive emails about new features and improvements</small>
                </label>
            </div>

            <div class=""form-check form-switch mb-3"">
                <input class=""form-check-input"" type=""checkbox"" id=""emailActivity"" @bind=""_settings.EmailActivity"" @bind:after=""NotifyChange"" />
                <label class=""form-check-label"" for=""emailActivity"">
                    <strong>Activity Alerts</strong>
                    <br /><small class=""text-muted"">Get notified about important activity in your account</small>
                </label>
            </div>

            <div class=""form-check form-switch mb-3"">
                <input class=""form-check-input"" type=""checkbox"" id=""emailDigest"" @bind=""_settings.EmailDigest"" @bind:after=""NotifyChange"" />
                <label class=""form-check-label"" for=""emailDigest"">
                    <strong>Weekly Digest</strong>
                    <br /><small class=""text-muted"">Receive a weekly summary of activity</small>
                </label>
            </div>

            <div class=""form-check form-switch"">
                <input class=""form-check-input"" type=""checkbox"" id=""emailMarketing"" @bind=""_settings.EmailMarketing"" @bind:after=""NotifyChange"" />
                <label class=""form-check-label"" for=""emailMarketing"">
                    <strong>Marketing Emails</strong>
                    <br /><small class=""text-muted"">Receive promotional emails and offers</small>
                </label>
            </div>
        </div>
    </div>

    <div class=""card mb-4"">
        <div class=""card-header"">
            <h6 class=""mb-0""><i class=""fa-solid fa-mobile-screen me-2""></i>Push Notifications</h6>
        </div>
        <div class=""card-body"">
            <div class=""form-check form-switch mb-3"">
                <input class=""form-check-input"" type=""checkbox"" id=""pushEnabled"" @bind=""_settings.PushEnabled"" @bind:after=""NotifyChange"" />
                <label class=""form-check-label"" for=""pushEnabled"">
                    <strong>Enable Push Notifications</strong>
                    <br /><small class=""text-muted"">Receive notifications in your browser</small>
                </label>
            </div>

            @if (_settings.PushEnabled) {{
                <div class=""ms-4"">
                    <div class=""form-check form-switch mb-2"">
                        <input class=""form-check-input"" type=""checkbox"" id=""pushMentions"" @bind=""_settings.PushMentions"" @bind:after=""NotifyChange"" />
                        <label class=""form-check-label"" for=""pushMentions"">Mentions and replies</label>
                    </div>
                    <div class=""form-check form-switch mb-2"">
                        <input class=""form-check-input"" type=""checkbox"" id=""pushAssignments"" @bind=""_settings.PushAssignments"" @bind:after=""NotifyChange"" />
                        <label class=""form-check-label"" for=""pushAssignments"">Task assignments</label>
                    </div>
                    <div class=""form-check form-switch"">
                        <input class=""form-check-input"" type=""checkbox"" id=""pushDeadlines"" @bind=""_settings.PushDeadlines"" @bind:after=""NotifyChange"" />
                        <label class=""form-check-label"" for=""pushDeadlines"">Upcoming deadlines</label>
                    </div>
                </div>
            }}
        </div>
    </div>

    <div class=""card mb-4"">
        <div class=""card-header"">
            <h6 class=""mb-0""><i class=""fa-solid fa-clock me-2""></i>Quiet Hours</h6>
        </div>
        <div class=""card-body"">
            <div class=""form-check form-switch mb-3"">
                <input class=""form-check-input"" type=""checkbox"" id=""quietHours"" @bind=""_settings.QuietHoursEnabled"" @bind:after=""NotifyChange"" />
                <label class=""form-check-label"" for=""quietHours"">
                    <strong>Enable Quiet Hours</strong>
                    <br /><small class=""text-muted"">Pause notifications during specified hours</small>
                </label>
            </div>

            @if (_settings.QuietHoursEnabled) {{
                <div class=""row ms-4"">
                    <div class=""col-md-4"">
                        <label class=""form-label small"">Start Time</label>
                        <input type=""time"" class=""form-control form-control-sm"" @bind=""_settings.QuietStart"" @bind:after=""NotifyChange"" />
                    </div>
                    <div class=""col-md-4"">
                        <label class=""form-label small"">End Time</label>
                        <input type=""time"" class=""form-control form-control-sm"" @bind=""_settings.QuietEnd"" @bind:after=""NotifyChange"" />
                    </div>
                </div>
            }}
        </div>
    </div>

    <hr class=""my-4"" />

    <div class=""d-flex justify-content-end gap-2"">
        <button type=""button"" class=""btn btn-secondary"" @onclick=""ResetSettings"">
            <i class=""fa-solid fa-undo me-1""></i>Reset
        </button>
        <button type=""button"" class=""btn btn-primary"" @onclick=""SaveSettings"">
            <i class=""fa-solid fa-save me-1""></i>Save Settings
        </button>
    </div>
</div>

@if (_showSuccess) {{
    <div class=""toast-container position-fixed bottom-0 end-0 p-3"">
        <div class=""toast show"" role=""alert"">
            <div class=""toast-header bg-success text-white"">
                <i class=""fa-solid fa-check-circle me-2""></i>
                <strong class=""me-auto"">Success</strong>
                <button type=""button"" class=""btn-close btn-close-white"" @onclick=""@(() => _showSuccess = false)""></button>
            </div>
            <div class=""toast-body"">
                Notification settings saved successfully.
            </div>
        </div>
    </div>
}}

@code {{
    [Parameter] public EventCallback OnChange {{ get; set; }}
    [Parameter] public EventCallback OnSave {{ get; set; }}

    private NotificationModel _settings = new();
    private NotificationModel _originalSettings = new();
    private bool _showSuccess = false;

    protected override void OnInitialized()
    {{
        _settings = new NotificationModel();
        _originalSettings = CloneSettings(_settings);
    }}

    private async Task NotifyChange()
    {{
        await OnChange.InvokeAsync();
    }}

    private async Task SaveSettings()
    {{
        _originalSettings = CloneSettings(_settings);
        _showSuccess = true;
        await OnSave.InvokeAsync();
        await Task.Delay(3000);
        _showSuccess = false;
        StateHasChanged();
    }}

    private void ResetSettings()
    {{
        _settings = CloneSettings(_originalSettings);
    }}

    private NotificationModel CloneSettings(NotificationModel source) => new()
    {{
        EmailUpdates = source.EmailUpdates,
        EmailActivity = source.EmailActivity,
        EmailDigest = source.EmailDigest,
        EmailMarketing = source.EmailMarketing,
        PushEnabled = source.PushEnabled,
        PushMentions = source.PushMentions,
        PushAssignments = source.PushAssignments,
        PushDeadlines = source.PushDeadlines,
        QuietHoursEnabled = source.QuietHoursEnabled,
        QuietStart = source.QuietStart,
        QuietEnd = source.QuietEnd
    }};

    private class NotificationModel
    {{
        public bool EmailUpdates {{ get; set; }} = true;
        public bool EmailActivity {{ get; set; }} = true;
        public bool EmailDigest {{ get; set; }} = false;
        public bool EmailMarketing {{ get; set; }} = false;
        public bool PushEnabled {{ get; set; }} = true;
        public bool PushMentions {{ get; set; }} = true;
        public bool PushAssignments {{ get; set; }} = true;
        public bool PushDeadlines {{ get; set; }} = true;
        public bool QuietHoursEnabled {{ get; set; }} = false;
        public string QuietStart {{ get; set; }} = ""22:00"";
        public string QuietEnd {{ get; set; }} = ""08:00"";
    }}
}}
";

    // ============================================================
    // SETTINGS HUB - PROFILE SETTINGS
    // ============================================================

    private static string GetSettingsHub_ProfileSettings(string name, DataObjects.EntityDefinition? entity) => $@"@* {name}ProfileSettings.App.razor - User profile settings *@
@* Generated by SettingsHub Page Pattern *@
@inject BlazorDataModel Model

<div class=""settings-section"">
    <div class=""d-flex justify-content-between align-items-center mb-4"">
        <div>
            <h4 class=""mb-1""><i class=""fa-solid fa-id-card me-2""></i>Profile Settings</h4>
            <p class=""text-muted mb-0"">Manage your personal information and profile details.</p>
        </div>
    </div>

    <EditForm Model=""_profile"" OnValidSubmit=""SaveProfile"">
        <DataAnnotationsValidator />

        <div class=""row"">
            <div class=""col-md-6 mb-3"">
                <label class=""form-label"">First Name <span class=""text-danger"">*</span></label>
                <InputText class=""form-control"" @bind-Value=""_profile.FirstName"" @oninput=""NotifyChange"" />
                <ValidationMessage For=""@(() => _profile.FirstName)"" />
            </div>
            <div class=""col-md-6 mb-3"">
                <label class=""form-label"">Last Name <span class=""text-danger"">*</span></label>
                <InputText class=""form-control"" @bind-Value=""_profile.LastName"" @oninput=""NotifyChange"" />
                <ValidationMessage For=""@(() => _profile.LastName)"" />
            </div>
        </div>

        <div class=""mb-3"">
            <label class=""form-label"">Email Address <span class=""text-danger"">*</span></label>
            <InputText type=""email"" class=""form-control"" @bind-Value=""_profile.Email"" @oninput=""NotifyChange"" />
            <ValidationMessage For=""@(() => _profile.Email)"" />
        </div>

        <div class=""mb-3"">
            <label class=""form-label"">Phone Number</label>
            <InputText class=""form-control"" @bind-Value=""_profile.Phone"" @oninput=""NotifyChange"" placeholder=""(optional)"" />
        </div>

        <div class=""mb-3"">
            <label class=""form-label"">Job Title</label>
            <InputText class=""form-control"" @bind-Value=""_profile.JobTitle"" @oninput=""NotifyChange"" placeholder=""(optional)"" />
        </div>

        <div class=""mb-3"">
            <label class=""form-label"">Bio</label>
            <InputTextArea class=""form-control"" rows=""3"" @bind-Value=""_profile.Bio"" @oninput=""NotifyChange"" placeholder=""Tell us about yourself..."" />
        </div>

        <div class=""mb-3"">
            <label class=""form-label"">Timezone</label>
            <InputSelect class=""form-select"" @bind-Value=""_profile.Timezone"" @onchange=""NotifyChange"">
                <option value=""UTC"">UTC</option>
                <option value=""America/New_York"">Eastern Time (US & Canada)</option>
                <option value=""America/Chicago"">Central Time (US & Canada)</option>
                <option value=""America/Denver"">Mountain Time (US & Canada)</option>
                <option value=""America/Los_Angeles"">Pacific Time (US & Canada)</option>
                <option value=""Europe/London"">London</option>
                <option value=""Europe/Paris"">Paris</option>
                <option value=""Asia/Tokyo"">Tokyo</option>
                <option value=""Australia/Sydney"">Sydney</option>
            </InputSelect>
        </div>

        <hr class=""my-4"" />

        <div class=""d-flex justify-content-end gap-2"">
            <button type=""button"" class=""btn btn-secondary"" @onclick=""ResetProfile"">
                <i class=""fa-solid fa-undo me-1""></i>Reset
            </button>
            <button type=""submit"" class=""btn btn-primary"">
                <i class=""fa-solid fa-save me-1""></i>Save Profile
            </button>
        </div>
    </EditForm>
</div>

@if (_showSuccess) {{
    <div class=""toast-container position-fixed bottom-0 end-0 p-3"">
        <div class=""toast show"" role=""alert"">
            <div class=""toast-header bg-success text-white"">
                <i class=""fa-solid fa-check-circle me-2""></i>
                <strong class=""me-auto"">Success</strong>
                <button type=""button"" class=""btn-close btn-close-white"" @onclick=""@(() => _showSuccess = false)""></button>
            </div>
            <div class=""toast-body"">
                Profile settings saved successfully.
            </div>
        </div>
    </div>
}}

@code {{
    [Parameter] public EventCallback OnChange {{ get; set; }}
    [Parameter] public EventCallback OnSave {{ get; set; }}

    private ProfileModel _profile = new();
    private ProfileModel _originalProfile = new();
    private bool _showSuccess = false;

    protected override void OnInitialized()
    {{
        if (Model.CurrentUser != null) {{
            _profile = new ProfileModel
            {{
                FirstName = Model.CurrentUser.FirstName ?? """",
                LastName = Model.CurrentUser.LastName ?? """",
                Email = Model.CurrentUser.Email ?? """",
                Phone = """",
                JobTitle = """",
                Bio = """",
                Timezone = ""UTC""
            }};
            _originalProfile = CloneProfile(_profile);
        }}
    }}

    private async Task NotifyChange()
    {{
        await OnChange.InvokeAsync();
    }}

    private async Task SaveProfile()
    {{
        _originalProfile = CloneProfile(_profile);
        _showSuccess = true;
        await OnSave.InvokeAsync();
        await Task.Delay(3000);
        _showSuccess = false;
        StateHasChanged();
    }}

    private void ResetProfile()
    {{
        _profile = CloneProfile(_originalProfile);
    }}

    private ProfileModel CloneProfile(ProfileModel source) => new()
    {{
        FirstName = source.FirstName,
        LastName = source.LastName,
        Email = source.Email,
        Phone = source.Phone,
        JobTitle = source.JobTitle,
        Bio = source.Bio,
        Timezone = source.Timezone
    }};

    private class ProfileModel
    {{
        public string FirstName {{ get; set; }} = """";
        public string LastName {{ get; set; }} = """";
        public string Email {{ get; set; }} = """";
        public string Phone {{ get; set; }} = """";
        public string JobTitle {{ get; set; }} = """";
        public string Bio {{ get; set; }} = """";
        public string Timezone {{ get; set; }} = ""UTC"";
    }}
}}
";

    // ============================================================
    // SETTINGS HUB - SIDEBAR
    // ============================================================
}

#endregion

// ---- PagePatterns.SplitPanelMasterDetail ----

#region PagePatterns - SplitPanelMasterDetail
// ============================================================================
// PAGE PATTERN: SPLIT PANEL MASTER-DETAIL
// Email-style interface: list on left, detail on right
//
// Generated Files:
// - {Entity}SplitView.App.razor: Main container with resizable split panels
// - {Entity}ListPanel.App.razor: Left panel with scrollable list and selection
// - {Entity}DetailPanel.App.razor: Right panel showing selected item details with tabs
// - {Entity}ListItem.App.razor: Individual list item component with selection highlighting
// ============================================================================

public static partial class PagePatterns
{
    /// <summary>
    /// Generate SplitPanelMasterDetail files for the given entity/project.
    /// </summary>
    public static List<(string FileName, string FileType, string Content)> GetSplitPanelMasterDetailFiles(
        string projectName,
        DataObjects.EntityDefinition? entity = null){
        List<(string, string, string)> files = new();
        string entityName = entity?.Name ?? "Item";
        string entityPlural = entity?.PluralName ?? "Items";

        files.Add(($"{projectName}.App.{entityName}SplitView.razor", "RazorComponent", GetSplitPanelMasterDetail_SplitView(projectName, entityName, entityPlural)));
        files.Add(($"{projectName}.App.{entityName}ListPanel.razor", "RazorComponent", GetSplitPanelMasterDetail_ListPanel(projectName, entityName, entityPlural)));
        files.Add(($"{projectName}.App.{entityName}DetailPanel.razor", "RazorComponent", GetSplitPanelMasterDetail_DetailPanel(projectName, entityName, entityPlural)));
        files.Add(($"{projectName}.App.{entityName}ListItem.razor", "RazorComponent", GetSplitPanelMasterDetail_ListItem(projectName, entityName, entityPlural)));

        return files;
    }

    // ============================================================
    // DETAIL PANEL - Right Panel with Tabs
    // ============================================================

    private static string GetSplitPanelMasterDetail_DetailPanel(string projectName, string entityName, string entityPlural) => $@"@* {entityName}DetailPanel.App.razor - Detail Panel with Tabs *@
@* Generated by Page Pattern: SplitPanelMasterDetail *@

<div class=""detail-panel"">
    <style>
        .detail-panel {{
            display: flex;
            flex-direction: column;
            height: 100%;
        }}
        .detail-panel-header {{
            padding: 1rem 1.5rem;
            border-bottom: 1px solid var(--bs-border-color);
            background: var(--bs-light);
        }}
        .detail-panel-tabs {{
            border-bottom: 1px solid var(--bs-border-color);
            background: var(--bs-body-bg);
        }}
        .detail-panel-tabs .nav-link {{
            border-radius: 0;
            border: none;
            border-bottom: 2px solid transparent;
            color: var(--bs-secondary);
            padding: 0.75rem 1.25rem;
        }}
        .detail-panel-tabs .nav-link:hover {{
            border-bottom-color: var(--bs-border-color);
            color: var(--bs-body-color);
        }}
        .detail-panel-tabs .nav-link.active {{
            border-bottom-color: var(--bs-primary);
            color: var(--bs-primary);
            background: transparent;
        }}
        .detail-panel-content {{
            flex: 1;
            overflow-y: auto;
            padding: 1.5rem;
        }}
        .detail-empty-state {{
            display: flex;
            flex-direction: column;
            align-items: center;
            justify-content: center;
            height: 100%;
            text-align: center;
            color: var(--bs-secondary);
        }}
        .detail-empty-state i {{
            font-size: 4rem;
            margin-bottom: 1.5rem;
            opacity: 0.3;
        }}
        .detail-field {{
            margin-bottom: 1.25rem;
        }}
        .detail-field-label {{
            font-size: 0.75rem;
            text-transform: uppercase;
            letter-spacing: 0.05em;
            color: var(--bs-secondary);
            margin-bottom: 0.25rem;
        }}
        .detail-field-value {{
            font-size: 1rem;
        }}
    </style>

    @if (Item == null) {{
        <div class=""detail-empty-state"">
            <i class=""fa-solid fa-arrow-left""></i>
            <h5>Select a {entityName.ToLower()}</h5>
            <p class=""text-muted mb-0"">Choose an item from the list to view details</p>
        </div>
    }} else {{
        <div class=""detail-panel-header"">
            <div class=""d-flex justify-content-between align-items-start"">
                <div>
                    <h4 class=""mb-1"">@(string.IsNullOrEmpty(Item.Name) ? ""New {entityName}"" : Item.Name)</h4>
                    <small class=""text-muted"">
                        @if (Item.{entityName}Id == default) {{
                            <span>Unsaved</span>
                        }} else {{
                            <span>ID: @Item.{entityName}Id.ToString()[..8]...</span>
                        }}
                    </small>
                </div>
                <div class=""btn-group"">
                    <button class=""btn btn-outline-secondary btn-sm"" @onclick=""() => OnClose.InvokeAsync()"" title=""Close"">
                        <i class=""fa-solid fa-times""></i>
                    </button>
                    <button class=""btn btn-outline-danger btn-sm"" @onclick=""() => OnDelete.InvokeAsync(Item)""
                            disabled=""@(Item.{entityName}Id == default)"" title=""Delete"">
                        <i class=""fa-solid fa-trash""></i>
                    </button>
                    <button class=""btn btn-primary btn-sm"" @onclick=""Save"" title=""Save"">
                        <i class=""fa-solid fa-save me-1""></i>Save
                    </button>
                </div>
            </div>
        </div>

        <div class=""detail-panel-tabs"">
            <ul class=""nav nav-tabs border-0"">
                <li class=""nav-item"">
                    <button class=""nav-link @(_activeTab == ""details"" ? ""active"" : """")""
                            @onclick=""@(() => _activeTab = ""details"")"">
                        <i class=""fa-solid fa-info-circle me-1""></i>Details
                    </button>
                </li>
                <li class=""nav-item"">
                    <button class=""nav-link @(_activeTab == ""activity"" ? ""active"" : """")""
                            @onclick=""@(() => _activeTab = ""activity"")"">
                        <i class=""fa-solid fa-clock me-1""></i>Activity
                    </button>
                </li>
                <li class=""nav-item"">
                    <button class=""nav-link @(_activeTab == ""related"" ? ""active"" : """")""
                            @onclick=""@(() => _activeTab = ""related"")"">
                        <i class=""fa-solid fa-link me-1""></i>Related
                    </button>
                </li>
            </ul>
        </div>

        <div class=""detail-panel-content"">
            @switch (_activeTab) {{
                case ""details"":
                    <EditForm Model=""_editModel"" OnValidSubmit=""Save"">
                        <DataAnnotationsValidator />

                        <div class=""mb-3"">
                            <label class=""form-label"">Name <span class=""text-danger"">*</span></label>
                            <input type=""text"" class=""form-control"" @bind=""_editModel.Name"" />
                            <ValidationMessage For=""@(() => _editModel.Name)"" />
                        </div>

                        <div class=""mb-3"">
                            <label class=""form-label"">Description</label>
                            <textarea class=""form-control"" rows=""4"" @bind=""_editModel.Description""></textarea>
                        </div>

                        <div class=""mb-3"">
                            <div class=""form-check"">
                                <input type=""checkbox"" class=""form-check-input"" id=""isComplete"" @bind=""_editModel.IsComplete"" />
                                <label class=""form-check-label"" for=""isComplete"">Mark as Complete</label>
                            </div>
                        </div>
                    </EditForm>
                    break;

                case ""activity"":
                    <div class=""timeline"">
                        @if (Item.{entityName}Id != default) {{
                            <div class=""timeline-item"">
                                <div class=""timeline-marker bg-success""></div>
                                <div class=""timeline-content"">
                                    <small class=""text-muted"">@Item.CreatedAt.ToString(""MMM dd, yyyy HH:mm"")</small>
                                    <p class=""mb-0"">Created</p>
                                </div>
                            </div>
                            @if (Item.UpdatedAt != Item.CreatedAt) {{
                                <div class=""timeline-item"">
                                    <div class=""timeline-marker bg-info""></div>
                                    <div class=""timeline-content"">
                                        <small class=""text-muted"">@Item.UpdatedAt.ToString(""MMM dd, yyyy HH:mm"")</small>
                                        <p class=""mb-0"">Last updated</p>
                                    </div>
                                </div>
                            }}
                            @if (Item.IsComplete && Item.CompletedAt.HasValue) {{
                                <div class=""timeline-item"">
                                    <div class=""timeline-marker bg-primary""></div>
                                    <div class=""timeline-content"">
                                        <small class=""text-muted"">@Item.CompletedAt.Value.ToString(""MMM dd, yyyy HH:mm"")</small>
                                        <p class=""mb-0"">Completed</p>
                                    </div>
                                </div>
                            }}
                        }} else {{
                            <p class=""text-muted"">No activity yet - save the item first</p>
                        }}
                    </div>
                    <style>
                        .timeline {{ position: relative; padding-left: 2rem; }}
                        .timeline-item {{ position: relative; padding-bottom: 1.5rem; }}
                        .timeline-item:last-child {{ padding-bottom: 0; }}
                        .timeline-item::before {{
                            content: '';
                            position: absolute;
                            left: -1.5rem;
                            top: 0.5rem;
                            bottom: -0.5rem;
                            width: 2px;
                            background: var(--bs-border-color);
                        }}
                        .timeline-item:last-child::before {{ display: none; }}
                        .timeline-marker {{
                            position: absolute;
                            left: -1.75rem;
                            top: 0.25rem;
                            width: 12px;
                            height: 12px;
                            border-radius: 50%;
                        }}
                    </style>
                    break;

                case ""related"":
                    <div class=""text-center text-muted py-4"">
                        <i class=""fa-solid fa-link fa-2x mb-2 opacity-50""></i>
                        <p>No related items</p>
                    </div>
                    break;
            }}
        </div>
    }}
</div>

@code {{
    [Parameter] public {entityName}? Item {{ get; set; }}
    [Parameter] public EventCallback<{entityName}> OnSave {{ get; set; }}
    [Parameter] public EventCallback<{entityName}> OnDelete {{ get; set; }}
    [Parameter] public EventCallback OnClose {{ get; set; }}

    private string _activeTab = ""details"";
    private {entityName} _editModel = new();

    protected override void OnParametersSet()
    {{
        if (Item != null)
        {{
            // Clone the item for editing
            _editModel = new {entityName}
            {{
                {entityName}Id = Item.{entityName}Id,
                Name = Item.Name,
                Description = Item.Description,
                IsComplete = Item.IsComplete,
                CreatedAt = Item.CreatedAt,
                UpdatedAt = Item.UpdatedAt,
                CompletedAt = Item.CompletedAt
            }};
        }}
        else
        {{
            _editModel = new();
        }}
        _activeTab = ""details"";
    }}

    private async Task Save()
    {{
        await OnSave.InvokeAsync(_editModel);
    }}
}}
";

    // ============================================================
    // LIST ITEM - Individual Item with Selection Highlighting
    // ============================================================

    private static string GetSplitPanelMasterDetail_SplitView(string projectName, string entityName, string entityPlural) => $@"@* {entityName}SplitView.App.razor - Split-Panel Master-Detail View *@
@* Generated by Page Pattern: SplitPanelMasterDetail *@
@page ""/{entityPlural}""
@page ""/{{TenantCode}}/{entityPlural}""
@implements IDisposable
@inject BlazorDataModel Model
@inject HttpClient Http
@inject IJSRuntime JS

@if (Model.Loaded && Model.LoggedIn && Model.View == _pageName) {{
    <div class=""split-view-container"" @onkeydown=""HandleKeyDown"" tabindex=""0"" @ref=""_containerRef"">
        <style>
            .split-view-container {{
                display: flex;
                height: calc(100vh - 60px);
                overflow: hidden;
                outline: none;
            }}
            .split-panel-left {{
                width: var(--left-panel-width, 350px);
                min-width: 250px;
                max-width: 600px;
                border-right: 1px solid var(--bs-border-color);
                display: flex;
                flex-direction: column;
                background: var(--bs-body-bg);
            }}
            .split-panel-right {{
                flex: 1;
                overflow: hidden;
                display: flex;
                flex-direction: column;
                background: var(--bs-body-bg);
            }}
            .split-resizer {{
                width: 4px;
                cursor: col-resize;
                background: transparent;
                transition: background 0.2s;
                position: relative;
            }}
            .split-resizer:hover,
            .split-resizer.dragging {{
                background: var(--bs-primary);
            }}
            .split-resizer::after {{
                content: '';
                position: absolute;
                left: -4px;
                right: -4px;
                top: 0;
                bottom: 0;
            }}
        </style>

        <div class=""split-panel-left"" style=""--left-panel-width: @(_leftPanelWidth)px;"">
            <{projectName}_App_{entityName}ListPanel
                Items=""_items""
                SelectedItem=""_selectedItem""
                Loading=""_loading""
                OnItemSelected=""SelectItem""
                OnRefresh=""LoadData""
                OnCreateNew=""CreateNew"" />
        </div>

        <div class=""split-resizer @(_isDragging ? ""dragging"" : """")""
             @onmousedown=""StartResize""
             @onmouseup=""StopResize"">
        </div>

        <div class=""split-panel-right"">
            <{projectName}_App_{entityName}DetailPanel
                Item=""_selectedItem""
                OnSave=""SaveItem""
                OnDelete=""DeleteItem""
                OnClose=""ClearSelection"" />
        </div>
    </div>
}}

@code {{
    private string _pageName = ""{entityPlural.ToLower()}"";
    private bool _loading = true;
    private List<{entityName}> _items = new();
    private {entityName}? _selectedItem;
    private int _selectedIndex = -1;
    private double _leftPanelWidth = 350;
    private bool _isDragging = false;
    private ElementReference _containerRef;

    public void Dispose() {{ Model.OnChange -= StateHasChanged; }}

    protected override void OnInitialized()
    {{
        Model.View = _pageName;
        Model.OnChange += StateHasChanged;
    }}

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {{
        if (firstRender)
        {{
            await LoadData();
            await _containerRef.FocusAsync();
        }}
    }}

    private async Task LoadData()
    {{
        _loading = true;
        StateHasChanged();

        var result = await Helpers.GetOrPost<{entityName}FilterResult>(
            DataObjects.Endpoints.{projectName}.Get{entityPlural}, new {entityName}Filter()) ?? new();
        _items = result.Records ?? new();

        // Maintain selection if possible
        if (_selectedItem != null)
        {{
            _selectedItem = _items.FirstOrDefault(x => x.{entityName}Id == _selectedItem.{entityName}Id);
            _selectedIndex = _selectedItem != null ? _items.IndexOf(_selectedItem) : -1;
        }}

        _loading = false;
        StateHasChanged();
    }}

    private void SelectItem({entityName} item)
    {{
        _selectedItem = item;
        _selectedIndex = _items.IndexOf(item);
        StateHasChanged();
    }}

    private void ClearSelection()
    {{
        _selectedItem = null;
        _selectedIndex = -1;
        StateHasChanged();
    }}

    private void CreateNew()
    {{
        _selectedItem = new {entityName}();
        _selectedIndex = -1;
        StateHasChanged();
    }}

    private async Task SaveItem({entityName} item)
    {{
        await Helpers.GetOrPost<{entityName}>(DataObjects.Endpoints.{projectName}.Save{entityName}, item);
        await LoadData();
    }}

    private async Task DeleteItem({entityName} item)
    {{
        if (await Helpers.Confirm(""Delete this {entityName.ToLower()}?""))
        {{
            await Helpers.GetOrPost<bool>(DataObjects.Endpoints.{projectName}.Delete{entityName}, item.{entityName}Id);
            _selectedItem = null;
            _selectedIndex = -1;
            await LoadData();
        }}
    }}

    // Keyboard navigation
    private void HandleKeyDown(KeyboardEventArgs e)
    {{
        if (_items.Count == 0) return;

        switch (e.Key)
        {{
            case ""ArrowUp"":
                if (_selectedIndex > 0)
                {{
                    _selectedIndex--;
                    _selectedItem = _items[_selectedIndex];
                }}
                break;
            case ""ArrowDown"":
                if (_selectedIndex < _items.Count - 1)
                {{
                    _selectedIndex++;
                    _selectedItem = _items[_selectedIndex];
                }}
                else if (_selectedIndex == -1 && _items.Count > 0)
                {{
                    _selectedIndex = 0;
                    _selectedItem = _items[0];
                }}
                break;
            case ""Escape"":
                ClearSelection();
                break;
            case ""Enter"":
                // Focus detail panel for editing
                break;
        }}
        StateHasChanged();
    }}

    // Panel resizing
    private void StartResize(MouseEventArgs e)
    {{
        _isDragging = true;
    }}

    private void StopResize(MouseEventArgs e)
    {{
        _isDragging = false;
    }}

    [JSInvokable]
    public void UpdatePanelWidth(double width)
    {{
        _leftPanelWidth = Math.Clamp(width, 250, 600);
        StateHasChanged();
    }}
}}
";
}

#endregion

// ---- PagePatterns.SplitPanelMasterDetail.List ----

#region PagePatterns - SplitPanelMasterDetail (List)
// Part of PagePatterns.SplitPanelMasterDetail: generator methods split out of the PagePatterns.SplitPanelMasterDetail section above, now merged into FreeManager.App.PagePatterns.cs (ListPanel, ListItem).

public static partial class PagePatterns
{
    private static string GetSplitPanelMasterDetail_ListItem(string projectName, string entityName, string entityPlural) => $@"@* {entityName}ListItem.App.razor - List Item Component *@
@* Generated by Page Pattern: SplitPanelMasterDetail *@

<div class=""list-item @(IsSelected ? ""selected"" : """")"" @onclick=""OnClick"">
    <style>
        .list-item {{
            padding: 0.875rem 1rem;
            border-bottom: 1px solid var(--bs-border-color);
            cursor: pointer;
            transition: background-color 0.15s ease;
        }}
        .list-item:hover {{
            background-color: var(--bs-light);
        }}
        .list-item.selected {{
            background-color: rgba(var(--bs-primary-rgb), 0.1);
            border-left: 3px solid var(--bs-primary);
            padding-left: calc(1rem - 3px);
        }}
        .list-item-title {{
            font-weight: 500;
            margin-bottom: 0.25rem;
            display: flex;
            align-items: center;
            gap: 0.5rem;
        }}
        .list-item-meta {{
            font-size: 0.8125rem;
            color: var(--bs-secondary);
            display: flex;
            align-items: center;
            gap: 0.75rem;
        }}
        .list-item-badge {{
            font-size: 0.6875rem;
            padding: 0.125rem 0.5rem;
        }}
    </style>

    <div class=""list-item-title"">
        @if (Item.IsComplete) {{
            <i class=""fa-solid fa-check-circle text-success"" title=""Complete""></i>
        }}
        <span class=""@(Item.IsComplete ? ""text-muted"" : """")"">@Item.Name</span>
    </div>

    <div class=""list-item-meta"">
        @if (!string.IsNullOrEmpty(Item.Description)) {{
            <span class=""text-truncate"" style=""max-width: 200px;"">@Item.Description</span>
        }}
        <span>
            <i class=""fa-regular fa-clock me-1""></i>
            @FormatDate(Item.UpdatedAt)
        </span>
    </div>
</div>

@code {{
    [Parameter] public {entityName} Item {{ get; set; }} = new();
    [Parameter] public bool IsSelected {{ get; set; }}
    [Parameter] public EventCallback OnClick {{ get; set; }}

    private string FormatDate(DateTime date)
    {{
        var diff = DateTime.UtcNow - date;
        if (diff.TotalMinutes < 1) return ""Just now"";
        if (diff.TotalMinutes < 60) return $""{{(int)diff.TotalMinutes}}m ago"";
        if (diff.TotalHours < 24) return $""{{(int)diff.TotalHours}}h ago"";
        if (diff.TotalDays < 7) return $""{{(int)diff.TotalDays}}d ago"";
        return date.ToString(""MMM dd"");
    }}
}}
";

    // ============================================================
    // LIST PANEL - Left Panel with Scrollable List
    // ============================================================

    private static string GetSplitPanelMasterDetail_ListPanel(string projectName, string entityName, string entityPlural) => $@"@* {entityName}ListPanel.App.razor - List Panel Component *@
@* Generated by Page Pattern: SplitPanelMasterDetail *@

<div class=""list-panel"">
    <style>
        .list-panel {{
            display: flex;
            flex-direction: column;
            height: 100%;
        }}
        .list-panel-header {{
            padding: 1rem;
            border-bottom: 1px solid var(--bs-border-color);
            background: var(--bs-light);
        }}
        .list-panel-search {{
            position: relative;
        }}
        .list-panel-search i {{
            position: absolute;
            left: 12px;
            top: 50%;
            transform: translateY(-50%);
            color: var(--bs-secondary);
        }}
        .list-panel-search input {{
            padding-left: 36px;
        }}
        .list-panel-content {{
            flex: 1;
            overflow-y: auto;
        }}
        .list-panel-footer {{
            padding: 0.75rem 1rem;
            border-top: 1px solid var(--bs-border-color);
            background: var(--bs-light);
            font-size: 0.875rem;
            color: var(--bs-secondary);
        }}
        .list-empty-state {{
            display: flex;
            flex-direction: column;
            align-items: center;
            justify-content: center;
            height: 100%;
            padding: 2rem;
            text-align: center;
            color: var(--bs-secondary);
        }}
        .list-empty-state i {{
            font-size: 3rem;
            margin-bottom: 1rem;
            opacity: 0.5;
        }}
    </style>

    <div class=""list-panel-header"">
        <div class=""d-flex justify-content-between align-items-center mb-2"">
            <h5 class=""mb-0"">{entityPlural}</h5>
            <div class=""btn-group btn-group-sm"">
                <button class=""btn btn-outline-secondary"" @onclick=""OnRefresh"" title=""Refresh"">
                    <i class=""fa-solid fa-refresh""></i>
                </button>
                <button class=""btn btn-primary"" @onclick=""OnCreateNew"" title=""New {entityName}"">
                    <i class=""fa-solid fa-plus""></i>
                </button>
            </div>
        </div>
        <div class=""list-panel-search"">
            <i class=""fa-solid fa-search""></i>
            <input type=""text"" class=""form-control form-control-sm""
                   placeholder=""Search {entityPlural.ToLower()}...""
                   @bind=""_searchText"" @bind:event=""oninput"" @bind:after=""FilterItems"" />
        </div>
    </div>

    <div class=""list-panel-content"">
        @if (Loading) {{
            <div class=""d-flex justify-content-center align-items-center h-100"">
                <div class=""spinner-border text-primary"" role=""status"">
                    <span class=""visually-hidden"">Loading...</span>
                </div>
            </div>
        }} else if (!FilteredItems.Any()) {{
            <div class=""list-empty-state"">
                <i class=""fa-solid fa-inbox""></i>
                <h6>No {entityPlural.ToLower()} found</h6>
                <p class=""mb-0"">@(string.IsNullOrEmpty(_searchText) ? ""Create your first {entityName.ToLower()} to get started"" : ""Try adjusting your search"")</p>
            </div>
        }} else {{
            @foreach (var item in FilteredItems) {{
                <{projectName}_App_{entityName}ListItem
                    Item=""item""
                    IsSelected=""item == SelectedItem""
                    OnClick=""() => OnItemSelected.InvokeAsync(item)"" />
            }}
        }}
    </div>

    <div class=""list-panel-footer"">
        <i class=""fa-solid fa-list me-1""></i>
        @FilteredItems.Count() of @Items.Count {entityPlural.ToLower()}
    </div>
</div>

@code {{
    [Parameter] public List<{entityName}> Items {{ get; set; }} = new();
    [Parameter] public {entityName}? SelectedItem {{ get; set; }}
    [Parameter] public bool Loading {{ get; set; }}
    [Parameter] public EventCallback<{entityName}> OnItemSelected {{ get; set; }}
    [Parameter] public EventCallback OnRefresh {{ get; set; }}
    [Parameter] public EventCallback OnCreateNew {{ get; set; }}

    private string _searchText = string.Empty;

    private IEnumerable<{entityName}> FilteredItems => string.IsNullOrWhiteSpace(_searchText)
        ? Items
        : Items.Where(x => x.Name.Contains(_searchText, StringComparison.OrdinalIgnoreCase));

    private void FilterItems()
    {{
        StateHasChanged();
    }}
}}
";

    // ============================================================
    // SPLIT VIEW - Main Container with Resizable Panels
    // ============================================================
}

#endregion

// ---- PagePatterns.TimelineActivityFeed ----

#region PagePatterns - TimelineActivityFeed
// ============================================================================
// PAGE PATTERN: TIMELINE ACTIVITY FEED
// Chronological event history grouped by date
//
// Generated Files:
// Timeline.App.razor, TimelineDay.App.razor, TimelineEvent.App.razor, TimelineFilters.App.razor
// ============================================================================

public static partial class PagePatterns
{
    /// <summary>
    /// Generate TimelineActivityFeed files for the given entity/project.
    /// </summary>
    public static List<(string FileName, string FileType, string Content)> GetTimelineActivityFeedFiles(
        string projectName,
        DataObjects.EntityDefinition? entity = null){
        List<(string, string, string)> files = new();

        files.Add(($"{projectName}.App.Timeline.razor", "RazorComponent", GetTimelineActivityFeed_Timeline(projectName, entity)));
        files.Add(($"{projectName}.App.TimelineDay.razor", "RazorComponent", GetTimelineActivityFeed_TimelineDay(projectName, entity)));
        files.Add(($"{projectName}.App.TimelineEvent.razor", "RazorComponent", GetTimelineActivityFeed_TimelineEvent(projectName, entity)));
        files.Add(($"{projectName}.App.TimelineFilters.razor", "RazorComponent", GetTimelineActivityFeed_TimelineFilters(projectName, entity)));

        return files;
    }

    // ============================================================
    // TIMELINE.RAZOR - Main timeline container with infinite scroll
    // ============================================================
    private static string GetTimelineActivityFeed_Timeline(string name, DataObjects.EntityDefinition? entity) => $@"@* {name} - Timeline Activity Feed *@
@* Generated by Page Pattern: TimelineActivityFeed *@
@page ""/{name}/Timeline""
@page ""/{{TenantCode}}/{name}/Timeline""
@implements IDisposable
@inject BlazorDataModel Model
@inject HttpClient Http
@inject IJSRuntime JS

@if (Model.Loaded && Model.LoggedIn && Model.View == _pageName) {{
    <div class=""container-fluid"">
        <div class=""@Model.StickyMenuClass"">
            <div class=""d-flex justify-content-between align-items-center"">
                <h1 class=""page-title mb-0"">
                    <i class=""fa-solid fa-clock-rotate-left me-2""></i>Activity Timeline
                </h1>
                <button class=""btn btn-outline-secondary"" @onclick=""ToggleFilters"">
                    <i class=""fa-solid fa-filter me-1""></i>Filters
                    @if (_filtersActive) {{
                        <span class=""badge bg-primary ms-1"">@_activeFilterCount</span>
                    }}
                </button>
            </div>
        </div>

        @if (_showFilters) {{
            <{name}_App_TimelineFilters
                Filter=""_filter""
                OnFilterChanged=""ApplyFilters""
                OnClose=""ToggleFilters"" />
        }}

        <div class=""timeline-container mt-3"">
            @if (_loading && _events.Count == 0) {{
                <div class=""text-center py-5"">
                    <i class=""fa-solid fa-spinner fa-spin fa-2x""></i>
                    <p class=""mt-2 text-muted"">Loading activity...</p>
                </div>
            }} else if (_events.Count == 0) {{
                <div class=""text-center py-5 text-muted"">
                    <i class=""fa-solid fa-inbox fa-4x mb-3 opacity-50""></i>
                    <h4>No activity found</h4>
                    <p>Events will appear here as they occur</p>
                </div>
            }} else {{
                @foreach (var dayGroup in _groupedEvents) {{
                    <{name}_App_TimelineDay
                        Date=""dayGroup.Key""
                        Events=""dayGroup.Value""
                        OnEntityClick=""NavigateToEntity"" />
                }}

                @if (_hasMore) {{
                    <div class=""text-center py-3"" @ref=""_loadMoreTrigger"">
                        @if (_loadingMore) {{
                            <i class=""fa-solid fa-spinner fa-spin""></i>
                            <span class=""ms-2"">Loading more...</span>
                        }} else {{
                            <button class=""btn btn-outline-primary"" @onclick=""LoadMore"">
                                <i class=""fa-solid fa-chevron-down me-1""></i>Load More
                            </button>
                        }}
                    </div>
                }}
            }}
        </div>
    </div>
}}

@code {{
    private string _pageName = ""{name.ToLower()}-timeline"";
    private bool _loading = true;
    private bool _loadingMore = false;
    private bool _showFilters = false;
    private bool _filtersActive = false;
    private int _activeFilterCount = 0;
    private bool _hasMore = true;
    private int _page = 0;
    private const int _pageSize = 20;

    private TimelineFilter _filter = new();
    private List<TimelineEvent> _events = new();
    private Dictionary<DateTime, List<TimelineEvent>> _groupedEvents = new();
    private ElementReference _loadMoreTrigger;

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
        _page = 0;
        _events.Clear();
        StateHasChanged();

        var result = await Helpers.GetOrPost<TimelineResult>(
            DataObjects.Endpoints.{name}.GetTimelineEvents,
            new {{ Filter = _filter, Page = _page, PageSize = _pageSize }}) ?? new();

        _events = result.Events;
        _hasMore = result.HasMore;
        GroupEventsByDate();
        _loading = false;
        StateHasChanged();
    }}

    private async Task LoadMore()
    {{
        if (_loadingMore || !_hasMore) return;

        _loadingMore = true;
        _page++;
        StateHasChanged();

        var result = await Helpers.GetOrPost<TimelineResult>(
            DataObjects.Endpoints.{name}.GetTimelineEvents,
            new {{ Filter = _filter, Page = _page, PageSize = _pageSize }}) ?? new();

        _events.AddRange(result.Events);
        _hasMore = result.HasMore;
        GroupEventsByDate();
        _loadingMore = false;
        StateHasChanged();
    }}

    private void GroupEventsByDate()
    {{
        _groupedEvents = _events
            .GroupBy(e => e.Timestamp.Date)
            .OrderByDescending(g => g.Key)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(e => e.Timestamp).ToList());
    }}

    private void ToggleFilters()
    {{
        _showFilters = !_showFilters;
    }}

    private async Task ApplyFilters(TimelineFilter filter)
    {{
        _filter = filter;
        _filtersActive = !filter.IsEmpty();
        _activeFilterCount = filter.GetActiveCount();
        _showFilters = false;
        await LoadData();
    }}

    private void NavigateToEntity(TimelineEvent evt)
    {{
        if (!string.IsNullOrEmpty(evt.EntityUrl))
        {{
            Model.NavigateTo(evt.EntityUrl);
        }}
    }}

    // Data models
    public class TimelineFilter
    {{
        public DateTime? StartDate {{ get; set; }}
        public DateTime? EndDate {{ get; set; }}
        public List<string> EventTypes {{ get; set; }} = new();
        public List<Guid> UserIds {{ get; set; }} = new();
        public string SearchText {{ get; set; }} = string.Empty;

        public bool IsEmpty() =>
            !StartDate.HasValue && !EndDate.HasValue &&
            EventTypes.Count == 0 && UserIds.Count == 0 &&
            string.IsNullOrWhiteSpace(SearchText);

        public int GetActiveCount()
        {{
            int count = 0;
            if (StartDate.HasValue || EndDate.HasValue) count++;
            if (EventTypes.Count > 0) count++;
            if (UserIds.Count > 0) count++;
            if (!string.IsNullOrWhiteSpace(SearchText)) count++;
            return count;
        }}
    }}

    public class TimelineEvent
    {{
        public Guid Id {{ get; set; }}
        public DateTime Timestamp {{ get; set; }}
        public string EventType {{ get; set; }} = string.Empty;
        public string Icon {{ get; set; }} = ""fa-circle"";
        public string IconColor {{ get; set; }} = ""secondary"";
        public string Title {{ get; set; }} = string.Empty;
        public string Description {{ get; set; }} = string.Empty;
        public string UserName {{ get; set; }} = string.Empty;
        public string? UserAvatar {{ get; set; }}
        public Guid? EntityId {{ get; set; }}
        public string? EntityType {{ get; set; }}
        public string? EntityName {{ get; set; }}
        public string? EntityUrl {{ get; set; }}
        public Dictionary<string, object>? Metadata {{ get; set; }}
    }}

    public class TimelineResult
    {{
        public List<TimelineEvent> Events {{ get; set; }} = new();
        public bool HasMore {{ get; set; }}
        public int TotalCount {{ get; set; }}
    }}
}}
";

    // ============================================================
    // TIMELINEFILTERS.RAZOR - Filter panel for timeline
    // ============================================================
    private static string GetTimelineActivityFeed_TimelineFilters(string name, DataObjects.EntityDefinition? entity) => $@"@* {name} - Timeline Filters Component *@
@* Generated by Page Pattern: TimelineActivityFeed *@

<div class=""card shadow-sm mb-3"">
    <div class=""card-header d-flex justify-content-between align-items-center py-2"">
        <h6 class=""mb-0"">
            <i class=""fa-solid fa-filter me-2""></i>Filter Activity
        </h6>
        <button class=""btn btn-sm btn-close"" @onclick=""Close""></button>
    </div>
    <div class=""card-body"">
        <div class=""row g-3"">
            @* Date Range *@
            <div class=""col-md-3"">
                <label class=""form-label small text-muted"">From Date</label>
                <input type=""date"" class=""form-control form-control-sm""
                       @bind=""Filter.StartDate"" @bind:event=""onchange"" />
            </div>
            <div class=""col-md-3"">
                <label class=""form-label small text-muted"">To Date</label>
                <input type=""date"" class=""form-control form-control-sm""
                       @bind=""Filter.EndDate"" @bind:event=""onchange"" />
            </div>

            @* Event Type Filter *@
            <div class=""col-md-3"">
                <label class=""form-label small text-muted"">Event Type</label>
                <select class=""form-select form-select-sm"" @bind=""_selectedEventType"" @bind:after=""AddEventType"">
                    <option value="""">All Types</option>
                    <option value=""Created"">Created</option>
                    <option value=""Updated"">Updated</option>
                    <option value=""Deleted"">Deleted</option>
                    <option value=""Completed"">Completed</option>
                    <option value=""Assigned"">Assigned</option>
                    <option value=""Commented"">Commented</option>
                    <option value=""StatusChanged"">Status Changed</option>
                </select>
            </div>

            @* Search *@
            <div class=""col-md-3"">
                <label class=""form-label small text-muted"">Search</label>
                <div class=""input-group input-group-sm"">
                    <span class=""input-group-text""><i class=""fa-solid fa-search""></i></span>
                    <input type=""text"" class=""form-control"" placeholder=""Search events...""
                           @bind=""Filter.SearchText"" @bind:event=""oninput"" />
                </div>
            </div>
        </div>

        @* Active Filters Display *@
        @if (Filter.EventTypes.Count > 0) {{
            <div class=""mt-3"">
                <label class=""form-label small text-muted"">Active Event Types:</label>
                <div class=""d-flex flex-wrap gap-1"">
                    @foreach (var eventType in Filter.EventTypes) {{
                        <span class=""badge bg-primary d-flex align-items-center"">
                            @eventType
                            <button class=""btn btn-sm p-0 ms-1 text-white"" @onclick=""() => RemoveEventType(eventType)"">
                                <i class=""fa-solid fa-times"" style=""font-size: 10px;""></i>
                            </button>
                        </span>
                    }}
                </div>
            </div>
        }}

        @* Quick Filters *@
        <div class=""mt-3 pt-3 border-top"">
            <label class=""form-label small text-muted"">Quick Filters:</label>
            <div class=""btn-group btn-group-sm"" role=""group"">
                <button class=""btn btn-outline-secondary @(_quickFilter == ""today"" ? ""active"" : """")""
                        @onclick=""() => ApplyQuickFilter(""today"")"">
                    Today
                </button>
                <button class=""btn btn-outline-secondary @(_quickFilter == ""week"" ? ""active"" : """")""
                        @onclick=""() => ApplyQuickFilter(""week"")"">
                    This Week
                </button>
                <button class=""btn btn-outline-secondary @(_quickFilter == ""month"" ? ""active"" : """")""
                        @onclick=""() => ApplyQuickFilter(""month"")"">
                    This Month
                </button>
                <button class=""btn btn-outline-secondary @(_quickFilter == ""all"" ? ""active"" : """")""
                        @onclick=""() => ApplyQuickFilter(""all"")"">
                    All Time
                </button>
            </div>
        </div>

        @* Actions *@
        <div class=""mt-3 pt-3 border-top d-flex justify-content-between"">
            <button class=""btn btn-sm btn-outline-secondary"" @onclick=""ClearFilters"">
                <i class=""fa-solid fa-eraser me-1""></i>Clear All
            </button>
            <button class=""btn btn-sm btn-primary"" @onclick=""ApplyFilters"">
                <i class=""fa-solid fa-check me-1""></i>Apply Filters
            </button>
        </div>
    </div>
</div>

@code {{
    [Parameter] public TimelineFilter Filter {{ get; set; }} = new();
    [Parameter] public EventCallback<TimelineFilter> OnFilterChanged {{ get; set; }}
    [Parameter] public EventCallback OnClose {{ get; set; }}

    private string _selectedEventType = string.Empty;
    private string _quickFilter = ""all"";

    private void AddEventType()
    {{
        if (!string.IsNullOrEmpty(_selectedEventType) && !Filter.EventTypes.Contains(_selectedEventType))
        {{
            Filter.EventTypes.Add(_selectedEventType);
            _selectedEventType = string.Empty;
            StateHasChanged();
        }}
    }}

    private void RemoveEventType(string eventType)
    {{
        Filter.EventTypes.Remove(eventType);
        StateHasChanged();
    }}

    private void ApplyQuickFilter(string filter)
    {{
        _quickFilter = filter;
        var today = DateTime.Today;

        switch (filter)
        {{
            case ""today"":
                Filter.StartDate = today;
                Filter.EndDate = today;
                break;
            case ""week"":
                Filter.StartDate = today.AddDays(-(int)today.DayOfWeek);
                Filter.EndDate = today;
                break;
            case ""month"":
                Filter.StartDate = new DateTime(today.Year, today.Month, 1);
                Filter.EndDate = today;
                break;
            case ""all"":
                Filter.StartDate = null;
                Filter.EndDate = null;
                break;
        }}
        StateHasChanged();
    }}

    private void ClearFilters()
    {{
        Filter = new TimelineFilter();
        _quickFilter = ""all"";
        _selectedEventType = string.Empty;
        StateHasChanged();
    }}

    private async Task ApplyFilters()
    {{
        await OnFilterChanged.InvokeAsync(Filter);
    }}

    private async Task Close()
    {{
        await OnClose.InvokeAsync();
    }}

    // Reference to parent's TimelineFilter class
    public class TimelineFilter
    {{
        public DateTime? StartDate {{ get; set; }}
        public DateTime? EndDate {{ get; set; }}
        public List<string> EventTypes {{ get; set; }} = new();
        public List<Guid> UserIds {{ get; set; }} = new();
        public string SearchText {{ get; set; }} = string.Empty;

        public bool IsEmpty() =>
            !StartDate.HasValue && !EndDate.HasValue &&
            EventTypes.Count == 0 && UserIds.Count == 0 &&
            string.IsNullOrWhiteSpace(SearchText);

        public int GetActiveCount()
        {{
            int count = 0;
            if (StartDate.HasValue || EndDate.HasValue) count++;
            if (EventTypes.Count > 0) count++;
            if (UserIds.Count > 0) count++;
            if (!string.IsNullOrWhiteSpace(SearchText)) count++;
            return count;
        }}
    }}
}}
";
}

#endregion

// ---- PagePatterns.TimelineActivityFeed.Items ----

#region PagePatterns - TimelineActivityFeed (Items)
// Part of PagePatterns.TimelineActivityFeed: generator methods split out of the PagePatterns.TimelineActivityFeed section above, now merged into FreeManager.App.PagePatterns.cs (TimelineDay, TimelineEvent).

public static partial class PagePatterns
{
    // ============================================================
    // TIMELINEDAY.RAZOR - Group of events for a single day
    // ============================================================
    private static string GetTimelineActivityFeed_TimelineDay(string name, DataObjects.EntityDefinition? entity) => $@"@* {name} - Timeline Day Component *@
@* Generated by Page Pattern: TimelineActivityFeed *@

<div class=""timeline-day mb-4"">
    <div class=""timeline-day-header sticky-top bg-body py-2 mb-3"" style=""z-index: 10;"">
        <div class=""d-flex align-items-center"">
            <div class=""timeline-date-badge me-3"">
                <span class=""badge bg-primary fs-6 px-3 py-2"">
                    <i class=""fa-solid fa-calendar-day me-1""></i>
                    @GetDateLabel(Date)
                </span>
            </div>
            <div class=""flex-grow-1 border-bottom""></div>
            <span class=""ms-3 text-muted small"">@Events.Count @(Events.Count == 1 ? ""event"" : ""events"")</span>
        </div>
    </div>

    <div class=""timeline-events position-relative ps-4"">
        @* Vertical timeline line *@
        <div class=""timeline-line position-absolute""
             style=""left: 11px; top: 0; bottom: 0; width: 2px; background: linear-gradient(to bottom, var(--bs-primary), var(--bs-gray-300));"">
        </div>

        @foreach (var evt in Events) {{
            <{name}_App_TimelineEvent Event=""evt"" OnEntityClick=""HandleEntityClick"" />
        }}
    </div>
</div>

@code {{
    [Parameter] public DateTime Date {{ get; set; }}
    [Parameter] public List<TimelineEvent> Events {{ get; set; }} = new();
    [Parameter] public EventCallback<TimelineEvent> OnEntityClick {{ get; set; }}

    private string GetDateLabel(DateTime date)
    {{
        var today = DateTime.Today;
        if (date.Date == today) return ""Today"";
        if (date.Date == today.AddDays(-1)) return ""Yesterday"";
        if (date.Date > today.AddDays(-7)) return date.ToString(""dddd"");
        return date.ToString(""MMM dd, yyyy"");
    }}

    private async Task HandleEntityClick(TimelineEvent evt)
    {{
        await OnEntityClick.InvokeAsync(evt);
    }}

    // Reference to parent's TimelineEvent class
    public class TimelineEvent
    {{
        public Guid Id {{ get; set; }}
        public DateTime Timestamp {{ get; set; }}
        public string EventType {{ get; set; }} = string.Empty;
        public string Icon {{ get; set; }} = ""fa-circle"";
        public string IconColor {{ get; set; }} = ""secondary"";
        public string Title {{ get; set; }} = string.Empty;
        public string Description {{ get; set; }} = string.Empty;
        public string UserName {{ get; set; }} = string.Empty;
        public string? UserAvatar {{ get; set; }}
        public Guid? EntityId {{ get; set; }}
        public string? EntityType {{ get; set; }}
        public string? EntityName {{ get; set; }}
        public string? EntityUrl {{ get; set; }}
        public Dictionary<string, object>? Metadata {{ get; set; }}
    }}
}}

<style>
    .timeline-day-header {{
        backdrop-filter: blur(8px);
    }}

    .timeline-events {{
        padding-left: 2rem;
    }}
</style>
";

    // ============================================================
    // TIMELINEEVENT.RAZOR - Individual event item
    // ============================================================
    private static string GetTimelineActivityFeed_TimelineEvent(string name, DataObjects.EntityDefinition? entity) => $@"@* {name} - Timeline Event Component *@
@* Generated by Page Pattern: TimelineActivityFeed *@

<div class=""timeline-event mb-3 position-relative"">
    @* Event dot on timeline *@
    <div class=""timeline-dot position-absolute d-flex align-items-center justify-content-center rounded-circle bg-white border border-2""
         style=""left: -24px; top: 4px; width: 24px; height: 24px; border-color: var(--bs-@Event.IconColor) !important;"">
        <i class=""fa-solid @Event.Icon text-@Event.IconColor"" style=""font-size: 10px;""></i>
    </div>

    <div class=""card shadow-sm hover-shadow"" style=""transition: box-shadow 0.2s ease;"">
        <div class=""card-body py-2 px-3"">
            <div class=""d-flex justify-content-between align-items-start"">
                <div class=""flex-grow-1"">
                    <div class=""d-flex align-items-center mb-1"">
                        @* User avatar *@
                        @if (!string.IsNullOrEmpty(Event.UserAvatar)) {{
                            <img src=""@Event.UserAvatar"" alt=""@Event.UserName""
                                 class=""rounded-circle me-2"" style=""width: 24px; height: 24px; object-fit: cover;"" />
                        }} else {{
                            <div class=""rounded-circle bg-secondary text-white d-flex align-items-center justify-content-center me-2""
                                 style=""width: 24px; height: 24px; font-size: 11px;"">
                                @GetInitials(Event.UserName)
                            </div>
                        }}

                        <span class=""fw-semibold text-dark"">@Event.UserName</span>
                        <span class=""text-muted mx-1"">·</span>
                        <span class=""badge bg-@Event.IconColor bg-opacity-10 text-@Event.IconColor small"">
                            @Event.EventType
                        </span>
                    </div>

                    <div class=""event-content"">
                        <span class=""text-dark"">@Event.Title</span>

                        @if (Event.EntityId.HasValue && !string.IsNullOrEmpty(Event.EntityName)) {{
                            <button class=""btn btn-link btn-sm p-0 ms-1 text-decoration-none""
                                    @onclick=""() => HandleClick()""
                                    @onclick:stopPropagation=""true"">
                                <i class=""fa-solid fa-arrow-up-right-from-square me-1"" style=""font-size: 10px;""></i>
                                @Event.EntityName
                            </button>
                        }}
                    </div>

                    @if (!string.IsNullOrEmpty(Event.Description)) {{
                        <p class=""text-muted small mb-0 mt-1"">@Event.Description</p>
                    }}
                </div>

                <div class=""text-end ms-3"">
                    <small class=""text-muted"" title=""@Event.Timestamp.ToString(""f"")"">
                        @GetRelativeTime(Event.Timestamp)
                    </small>
                </div>
            </div>
        </div>
    </div>
</div>

@code {{
    [Parameter] public TimelineEvent Event {{ get; set; }} = new();
    [Parameter] public EventCallback<TimelineEvent> OnEntityClick {{ get; set; }}

    private string GetInitials(string name)
    {{
        if (string.IsNullOrWhiteSpace(name)) return ""?"";
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2)
            return $""{{parts[0][0]}}{{parts[1][0]}}"".ToUpper();
        return name.Length > 0 ? name[0].ToString().ToUpper() : ""?"";
    }}

    private string GetRelativeTime(DateTime timestamp)
    {{
        var diff = DateTime.UtcNow - timestamp;

        if (diff.TotalMinutes < 1) return ""Just now"";
        if (diff.TotalMinutes < 60) return $""{{(int)diff.TotalMinutes}}m ago"";
        if (diff.TotalHours < 24) return $""{{(int)diff.TotalHours}}h ago"";
        if (diff.TotalDays < 7) return $""{{(int)diff.TotalDays}}d ago"";
        return timestamp.ToString(""MMM dd"");
    }}

    private async Task HandleClick()
    {{
        await OnEntityClick.InvokeAsync(Event);
    }}

    // Reference to parent's TimelineEvent class
    public class TimelineEvent
    {{
        public Guid Id {{ get; set; }}
        public DateTime Timestamp {{ get; set; }}
        public string EventType {{ get; set; }} = string.Empty;
        public string Icon {{ get; set; }} = ""fa-circle"";
        public string IconColor {{ get; set; }} = ""secondary"";
        public string Title {{ get; set; }} = string.Empty;
        public string Description {{ get; set; }} = string.Empty;
        public string UserName {{ get; set; }} = string.Empty;
        public string? UserAvatar {{ get; set; }}
        public Guid? EntityId {{ get; set; }}
        public string? EntityType {{ get; set; }}
        public string? EntityName {{ get; set; }}
        public string? EntityUrl {{ get; set; }}
        public Dictionary<string, object>? Metadata {{ get; set; }}
    }}
}}

<style>
    .timeline-event .card:hover {{
        box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1) !important;
    }}

    .hover-shadow:hover {{
        transform: translateX(2px);
    }}
</style>
";
}

#endregion
