namespace FreeManager.Client;

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
        DataObjects.EntityDefinition? entity = null)
    {
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
    // ENDPOINT TREE - Left Navigation Panel
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
    // ENDPOINT DETAIL - Right Detail Panel
    // ============================================================

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
    // TRY IT PANEL - Interactive Request Testing
    // ============================================================

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

    // ============================================================
    // RESPONSE VIEWER - JSON Response Display
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
}

#endregion
