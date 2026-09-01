using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Playwright;

// FreeGLBA showcase runner: drives the live app with Playwright, seeds realistic
// data, and captures the screenshots used in the project report.

const string BaseUrl = "https://localhost:7271";
var shotsDir = args.Length > 0 ? args[0] : Path.Combine(AppContext.BaseDirectory, "shots");
Directory.CreateDirectory(shotsDir);

string apiKey = "";

var pw = await Playwright.CreateAsync();
IBrowser browser;
try {
    browser = await pw.Chromium.LaunchAsync(new() { Headless = true });
    Log("Launched bundled Chromium");
} catch (Exception ex) {
    Log($"Bundled Chromium unavailable ({ex.Message.Split('\n')[0]}); falling back to Edge");
    browser = await pw.Chromium.LaunchAsync(new() { Headless = true, Channel = "msedge" });
}

var ctx = await browser.NewContextAsync(new() {
    IgnoreHTTPSErrors = true,
    ViewportSize = new() { Width = 1600, Height = 1000 },
});
var page = await ctx.NewPageAsync();
page.SetDefaultTimeout(60000);

var retake = args.Contains("--retake08");
var reportsMode = args.Contains("--reports");

// ---------------------------------------------------------------- login
Log("Opening app...");
await page.GotoAsync(BaseUrl + "/", new() { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 120000 });

// The login page may show provider buttons first, or the local form directly.
if (!await page.Locator("#login-email").IsVisibleAsync()) {
    var localButton = page.Locator("#login-button-local");
    if (await localButton.IsVisibleAsync()) {
        await localButton.ClickAsync();
    } else {
        // May need to navigate to Login explicitly
        await page.GotoAsync(BaseUrl + "/Login", new() { WaitUntil = WaitUntilState.NetworkIdle });
        if (!await page.Locator("#login-email").IsVisibleAsync()) {
            await page.Locator("#login-button-local").ClickAsync();
        }
    }
}
await page.WaitForSelectorAsync("#login-email");
await page.FillAsync("#login-email", "admin");
await page.FillAsync("#login-password", "admin");
await Shot("01-login");
await page.Keyboard.PressAsync("Enter");

Log("Waiting for app shell after login...");
await page.WaitForSelectorAsync("text=GLBA Dashboard", new() { Timeout = 120000 });
await page.WaitForTimeoutAsync(1500);

if (args.Contains("--probe")) {
    // Diagnose the dashboard chart: dump console output and runtime state.
    page.Console += (_, msg) => Log($"  [console:{msg.Type}] {msg.Text}");
    page.PageError += (_, err) => Log($"  [pageerror] {err}");
    await page.GotoAsync(BaseUrl + "/GlbaDashboard", new() { WaitUntil = WaitUntilState.NetworkIdle });
    await page.WaitForSelectorAsync("text=Events Today", new() { Timeout = 60000 });
    await page.WaitForTimeoutAsync(8000);
    await page.EvaluateAsync("() => document.getElementById('glba-trend-chart')?.scrollIntoView({ block: 'center' })");
    await page.WaitForTimeoutAsync(600);
    await Shot("06b-dashboard-chart");
    await page.EvaluateAsync("() => [...document.querySelectorAll('.card-header')].find(x => x.textContent.includes('Needs Attention'))?.scrollIntoView({ block: 'start' })");
    await page.WaitForTimeoutAsync(600);
    await Shot("06c-dashboard-attention");
    await browser.CloseAsync();
    return;
}

if (args.Contains("--subject")) {
    // Verifies the DSAR-style subject access-history PDF end to end through the UI.
    Log("Subject mode: creating source system and seeding...");
    await page.GotoAsync(BaseUrl + "/SourceSystems", new() { WaitUntil = WaitUntilState.NetworkIdle });
    await page.ClickAsync("button:has-text('New Source System')");
    await page.WaitForSelectorAsync("#edit-sourcesystem-Name");
    await page.FillAsync("#edit-sourcesystem-Name", "Banner-SIS");
    await page.FillAsync("input[placeholder='Friendly name for display']", "Banner Student Information System");
    await page.FillAsync("#edit-sourcesystem-OwnerName", "Alice Nguyen");
    await page.FillAsync("#edit-sourcesystem-OwnerEmail", "alice.nguyen@university.edu");
    await page.FillAsync("#edit-sourcesystem-OwnerDepartment", "Financial Aid Office");
    await page.ClickAsync(".modal-footer button.btn-primary");
    await page.WaitForSelectorAsync(".alert-success input");
    apiKey = await page.Locator(".alert-success input").InputValueAsync();
    await page.ClickAsync(".modal-footer button.btn-success");
    await SeedHistory(apiKey);

    Log("Opening the most-accessed data subject...");
    await page.GotoAsync(BaseUrl + "/DataSubjects", new() { WaitUntil = WaitUntilState.NetworkIdle });
    await page.WaitForSelectorAsync("table tbody tr");
    await page.Locator("table tbody tr").First.ClickAsync();
    await page.WaitForSelectorAsync("button:has-text('History PDF')");
    await page.WaitForTimeoutAsync(800);
    await Shot("15-subject-history-button");

    Log("Downloading subject access history PDF...");
    var download = await page.RunAndWaitForDownloadAsync(async () => {
        await page.ClickAsync("button:has-text('History PDF')");
    }, new() { Timeout = 60000 });
    var pdfPath = Path.Combine(shotsDir, "subject-history.pdf");
    await download.SaveAsAsync(pdfPath);
    var bytes = await File.ReadAllBytesAsync(pdfPath);
    Log($"  PDF: {bytes.Length} bytes, magic = {System.Text.Encoding.ASCII.GetString(bytes[..4])}, file = {download.SuggestedFilename}");
    Log("DONE (subject)");
    await browser.CloseAsync();
    return;
}

if (args.Contains("--integrity")) {
    // Screenshots the GLBA Settings page and the intact-chain verification modal.
    Log("Integrity mode: creating source system and seeding...");
    await page.GotoAsync(BaseUrl + "/SourceSystems", new() { WaitUntil = WaitUntilState.NetworkIdle });
    await page.ClickAsync("button:has-text('New Source System')");
    await page.WaitForSelectorAsync("#edit-sourcesystem-Name");
    await page.FillAsync("#edit-sourcesystem-Name", "Banner-SIS");
    await page.FillAsync("input[placeholder='Friendly name for display']", "Banner Student Information System");
    await page.FillAsync("#edit-sourcesystem-OwnerName", "Alice Nguyen");
    await page.FillAsync("#edit-sourcesystem-OwnerEmail", "alice.nguyen@university.edu");
    await page.FillAsync("#edit-sourcesystem-OwnerDepartment", "Financial Aid Office");
    await page.ClickAsync(".modal-footer button.btn-primary");
    await page.WaitForSelectorAsync(".alert-success input");
    apiKey = await page.Locator(".alert-success input").InputValueAsync();
    await page.ClickAsync(".modal-footer button.btn-success");
    await SeedHistory(apiKey);

    Log("GLBA Settings page...");
    await page.GotoAsync(BaseUrl + "/GlbaSettings", new() { WaitUntil = WaitUntilState.NetworkIdle });
    await page.WaitForSelectorAsync("#glba-webhook-url");
    await page.CheckAsync("#glba-alerts-enabled");
    await page.FillAsync("#glba-webhook-url", "https://hooks.slack.com/services/T000/B000/EXAMPLE");
    await page.SelectOptionAsync("#glba-timezone", "Pacific Standard Time");
    await page.CheckAsync("#glba-afterhours-alert");
    await page.ClickAsync("button:has-text('Save Settings')");
    await page.WaitForTimeoutAsync(1200);
    await Shot("16-glba-settings");

    Log("Verifying audit-trail integrity...");
    await page.GotoAsync(BaseUrl + "/SourceSystems", new() { WaitUntil = WaitUntilState.NetworkIdle });
    await page.WaitForSelectorAsync("table tbody tr");
    await page.Locator("table tbody tr").First.Locator("button[title='Verify audit-trail integrity']").ClickAsync();
    await page.WaitForSelectorAsync("text=Chain intact", new() { Timeout = 30000 });
    await page.WaitForTimeoutAsync(500);
    await Shot("17-integrity-verified");
    Log("DONE (integrity)");
    await browser.CloseAsync();
    return;
}

if (reportsMode) {
    // Verifies compliance-report generation end to end: seed data, create a
    // report via the authenticated page context, download PDF + CSV, check bytes.
    Log("Reports mode: creating source system...");
    await page.GotoAsync(BaseUrl + "/SourceSystems", new() { WaitUntil = WaitUntilState.NetworkIdle });
    await page.ClickAsync("button:has-text('New Source System')");
    await page.WaitForSelectorAsync("#edit-sourcesystem-Name");
    await page.FillAsync("#edit-sourcesystem-Name", "Banner-SIS");
    await page.FillAsync("input[placeholder='Friendly name for display']", "Banner Student Information System");
    await page.FillAsync("#edit-sourcesystem-OwnerName", "Alice Nguyen");
    await page.FillAsync("#edit-sourcesystem-OwnerEmail", "alice.nguyen@university.edu");
    await page.FillAsync("#edit-sourcesystem-OwnerDepartment", "Financial Aid Office");
    await page.ClickAsync(".modal-footer button.btn-primary");
    await page.WaitForSelectorAsync(".alert-success input");
    apiKey = await page.Locator(".alert-success input").InputValueAsync();
    await page.ClickAsync(".modal-footer button.btn-success");
    Log("Seeding events...");
    await SeedHistory(apiKey);

    Log("Creating a compliance report via the authenticated page context...");
    var start = DateTime.UtcNow.AddDays(-30).ToString("yyyy-MM-dd");
    var end = DateTime.UtcNow.ToString("yyyy-MM-dd");
    var createResult = await page.EvaluateAsync<string>(
        "async () => { const r = await fetch('api/Data/SaveComplianceReport', { method: 'POST', headers: { 'Content-Type': 'application/json' }, " +
        $"body: JSON.stringify({{ reportType: 'Monthly Access Summary', periodStart: '{start}', periodEnd: '{end}' }}) }}); return r.status + ' ' + (await r.text()).substring(0, 200); }}");
    Log($"  SaveComplianceReport -> {createResult}");

    await page.GotoAsync(BaseUrl + "/ComplianceReports", new() { WaitUntil = WaitUntilState.NetworkIdle });
    await page.WaitForSelectorAsync("table tbody tr");

    Log("Downloading PDF...");
    var pdfDownload = await page.RunAndWaitForDownloadAsync(async () => {
        await page.Locator("table tbody tr").First.Locator("button[title='Download PDF summary']").ClickAsync();
    }, new() { Timeout = 60000 });
    var pdfPath = Path.Combine(shotsDir, "report.pdf");
    await pdfDownload.SaveAsAsync(pdfPath);
    var pdfBytes = await File.ReadAllBytesAsync(pdfPath);
    Log($"  PDF: {pdfBytes.Length} bytes, magic = {System.Text.Encoding.ASCII.GetString(pdfBytes[..4])}");

    Log("Downloading CSV...");
    var csvDownload = await page.RunAndWaitForDownloadAsync(async () => {
        await page.Locator("table tbody tr").First.Locator("button[title*='Download CSV']").ClickAsync();
    }, new() { Timeout = 60000 });
    var csvPath = Path.Combine(shotsDir, "report.csv");
    await csvDownload.SaveAsAsync(csvPath);
    var csvLines = (await File.ReadAllLinesAsync(csvPath)).Length;
    Log($"  CSV: {csvLines} lines (header + events)");

    await page.WaitForTimeoutAsync(800);
    await Shot("14-compliance-reports");
    Log("DONE (reports)");
    await browser.CloseAsync();
    return;
}

if (retake) {
    // Only re-take the ownership-history shot, with the table scrolled into view.
    Log("Retaking ownership history shot...");
    await page.GotoAsync(BaseUrl + "/SourceSystems", new() { WaitUntil = WaitUntilState.NetworkIdle });
    await page.WaitForSelectorAsync("table tbody tr");
    await page.Locator("table tbody tr").First.Locator("button[title='Edit']").ClickAsync();
    await page.WaitForSelectorAsync("text=Ownership History");
    await page.WaitForTimeoutAsync(800);
    await page.EvalOnSelectorAsync(".modal-body", "el => el.scrollTop = el.scrollHeight");
    await page.WaitForTimeoutAsync(400);
    await Shot("08-ownership-history");
    Log("DONE (retake)");
    await browser.CloseAsync();
    return;
}

// ------------------------------------------- create source system + owner
Log("Creating source system with data owner...");
await page.GotoAsync(BaseUrl + "/SourceSystems", new() { WaitUntil = WaitUntilState.NetworkIdle });
await page.ClickAsync("button:has-text('New Source System')");
await page.WaitForSelectorAsync("#edit-sourcesystem-Name");
await page.FillAsync("#edit-sourcesystem-Name", "Banner-SIS");
await page.FillAsync("input[placeholder='Friendly name for display']", "Banner Student Information System");
await page.FillAsync("#edit-sourcesystem-ContactEmail", "banner-admin@university.edu");
await page.FillAsync("#edit-sourcesystem-OwnerName", "Alice Nguyen");
await page.FillAsync("#edit-sourcesystem-OwnerEmail", "alice.nguyen@university.edu");
await page.FillAsync("#edit-sourcesystem-OwnerDepartment", "Financial Aid Office");
await page.FillAsync("#edit-sourcesystem-OwnerPhone", "509-555-0142");
await Shot("02-sourcesystem-form");
await page.ClickAsync(".modal-footer button.btn-primary");

Log("Capturing generated API key...");
await page.WaitForSelectorAsync(".alert-success input");
apiKey = await page.Locator(".alert-success input").InputValueAsync();
Log($"API key captured ({apiKey.Length} chars)");
await Shot("03-sourcesystem-apikey");
await page.ClickAsync(".modal-footer button.btn-success");
await page.WaitForTimeoutAsync(1000);

// ---------------------------------------------------------- API explorer
Log("Driving the API Explorer...");
await page.GotoAsync(BaseUrl + "/ApiExplorer", new() { WaitUntil = WaitUntilState.NetworkIdle });
await page.WaitForSelectorAsync("#api-explorer-key");
await page.FillAsync("#api-explorer-key", apiKey);
var singleSection = page.Locator("section[aria-labelledby='sample-single-heading']");
await singleSection.Locator("button:has-text('Send')").First.ClickAsync();
await singleSection.Locator(".badge.text-bg-success").WaitForAsync(new() { Timeout = 30000 });
await Shot("04-apiexplorer-201");

// Same body again -> deduplicated with 409.
await singleSection.Locator("button:has-text('Send again')").ClickAsync();
await singleSection.Locator(".badge.text-bg-danger").WaitForAsync(new() { Timeout = 30000 });
await Shot("05-apiexplorer-409-duplicate");

// ------------------------------------------------ seed realistic history
Log("Seeding realistic event history through the batch API...");
await SeedHistory(apiKey);

// -------------------------------------------------------------- dashboard
Log("Dashboard...");
await page.GotoAsync(BaseUrl + "/GlbaDashboard", new() { WaitUntil = WaitUntilState.NetworkIdle });
await page.WaitForSelectorAsync("text=Events Today");
// Give the Highcharts CDN scripts and the trend chart time to render.
try { await page.WaitForSelectorAsync("#glba-trend-chart svg", new() { Timeout = 20000 }); } catch { Log("  (trend chart did not render)"); }
await page.WaitForTimeoutAsync(1500);
await Shot("06-dashboard", fullPage: true);

// ------------------------------------------------- events + detail panel
Log("Access events detail (ownership intact)...");
await page.GotoAsync(BaseUrl + "/AccessEvents", new() { WaitUntil = WaitUntilState.NetworkIdle });
await page.WaitForSelectorAsync("table tbody tr");
await page.Locator("table tbody tr").First.ClickAsync();
await page.WaitForSelectorAsync("text=Data Ownership");
await page.WaitForTimeoutAsync(600);
await Shot("07-event-detail-ownership", fullPage: true);

// ------------------------------------------------------- change the owner
Log("Transferring data ownership to a new owner...");
await page.GotoAsync(BaseUrl + "/SourceSystems", new() { WaitUntil = WaitUntilState.NetworkIdle });
await page.WaitForSelectorAsync("table tbody tr");
await page.Locator("table tbody tr").First.Locator("button[title='Edit']").ClickAsync();
await page.WaitForSelectorAsync("#edit-sourcesystem-OwnerName");
await page.FillAsync("#edit-sourcesystem-OwnerName", "Marcus Chen");
await page.FillAsync("#edit-sourcesystem-OwnerEmail", "marcus.chen@university.edu");
await page.FillAsync("#edit-sourcesystem-OwnerDepartment", "Bursar Office");
await page.FillAsync("#edit-sourcesystem-OwnerPhone", "509-555-0177");
await page.ClickAsync(".modal-footer button.btn-primary");
await page.WaitForTimeoutAsync(1500);

// Reopen to show ownership history (old + new owner).
await page.Locator("table tbody tr").First.Locator("button[title='Edit']").ClickAsync();
await page.WaitForSelectorAsync("text=Ownership History");
await page.WaitForTimeoutAsync(800);
await Shot("08-ownership-history", fullPage: true);
await page.ClickAsync(".modal-footer button.btn-secondary"); // Cancel
await page.WaitForTimeoutAsync(500);

// ----------------------------------- event whose data changed hands since
Log("Event detail with changed-hands warning...");
await page.GotoAsync(BaseUrl + "/AccessEvents", new() { WaitUntil = WaitUntilState.NetworkIdle });
await page.WaitForSelectorAsync("table tbody tr");
await page.Locator("table tbody tr").First.ClickAsync();
await page.WaitForSelectorAsync("text=changed hands");
await page.WaitForTimeoutAsync(600);
await Shot("09-event-ownership-changed", fullPage: true);

// -------------------------------------------------- live SignalR arrival
Log("Posting a live event while watching the list (SignalR)...");
var beforeCount = await page.Locator("table tbody tr").CountAsync();
await PostLiveEvent(apiKey);
await page.WaitForTimeoutAsync(4000); // list refreshes itself over SignalR
await Shot("10-live-event-arrived");
Log($"Rows before live event: {beforeCount}; after: {await page.Locator("table tbody tr").CountAsync()}");

// -------------------------------------------------------------- accessors
Log("Accessors...");
await page.GotoAsync(BaseUrl + "/Accessors", new() { WaitUntil = WaitUntilState.NetworkIdle });
await page.WaitForSelectorAsync("table tbody tr");
await page.WaitForTimeoutAsync(600);
await Shot("11-accessors");

// ---------------------------------------------------------- data subjects
Log("Data subjects...");
await page.GotoAsync(BaseUrl + "/DataSubjects", new() { WaitUntil = WaitUntilState.NetworkIdle });
await page.WaitForSelectorAsync("table tbody tr");
await page.Locator("table tbody tr").First.ClickAsync();
await page.WaitForTimeoutAsync(1000);
await Shot("12-datasubjects", fullPage: true);

// ------------------------------------------------------ source system list
Log("Source systems list with owner column...");
await page.GotoAsync(BaseUrl + "/SourceSystems", new() { WaitUntil = WaitUntilState.NetworkIdle });
await page.WaitForSelectorAsync("table tbody tr");
await page.WaitForTimeoutAsync(600);
await Shot("13-sourcesystems-owner-column");

Log("DONE");
await browser.CloseAsync();
return;

// ------------------------------------------------------------------ helpers

void Log(string message) => Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {message}");

async Task Shot(string name, bool fullPage = false)
{
    var path = Path.Combine(shotsDir, name + ".png");
    await page.ScreenshotAsync(new() { Path = path, FullPage = fullPage });
    Log($"  saved {name}.png");
}

static HttpClient NewApiClient(string key)
{
    var handler = new HttpClientHandler {
        ServerCertificateCustomValidationCallback = (_, _, _, _) => true,
    };
    var client = new HttpClient(handler) { BaseAddress = new Uri(BaseUrl) };
    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", key);
    return client;
}

static async Task SeedHistory(string key)
{
    var random = new Random(42);
    string[] users = { "E102938|Sarah Mitchell|Financial Aid", "E445210|James Okafor|Financial Aid", "E220145|Linda Park|Bursar", "E318876|Robert Diaz|Bursar", "E509321|Emily Watson|Registrar", "E612044|David Kim|Student Accounts", "E733190|Maria Gonzalez|Financial Aid", "E845552|Thomas Lee|Enrollment Services", "E927716|Anna Kowalski|Student Accounts", "E014873|Chris Turner|Business Office" };
    string[] accessTypes = { "View", "View", "View", "Query", "Export", "Print", "Download" };
    string[] categories = { "Financial Aid", "Account Balance", "Payment History", "Loan Info", "FAFSA Data", "1098-T", "Scholarship Data" };
    string[] purposes = { "Enrollment verification", "Aid disbursement review", "Account inquiry from student", "Payment processing", "Annual audit sampling", "Compliance check", "1098-T reissue request", "Scholarship eligibility review", "Collections follow-up", "Data verification" };
    var subjects = Enumerable.Range(0, 40).Select(_ => $"S{random.Next(10000000, 99999999)}").ToArray();

    var events = new List<object>();
    for (int i = 0; i < 180; i++) {
        var user = users[random.Next(users.Length)].Split('|');
        var accessedAt = DateTime.UtcNow.AddDays(-random.Next(0, 30)).AddMinutes(-random.Next(0, 1440));
        var isBulk = random.Next(12) == 0;

        if (isBulk) {
            var bulkSubjects = subjects.OrderBy(_ => random.Next()).Take(random.Next(5, 15)).ToList();
            events.Add(new {
                sourceEventId = $"SEED-{i:D5}",
                userId = user[0], userName = user[1], userDepartment = user[2],
                userEmail = $"{user[1].ToLower().Replace(" ", ".")}@university.edu",
                subjectIds = bulkSubjects, subjectType = "Student",
                accessType = random.Next(2) == 0 ? "Export" : "Download",
                dataCategory = categories[random.Next(categories.Length)],
                purpose = "Batch export: " + purposes[random.Next(purposes.Length)],
                accessedAt,
                ipAddress = $"10.{random.Next(1, 255)}.{random.Next(1, 255)}.{random.Next(1, 255)}",
            });
        } else {
            events.Add(new {
                sourceEventId = $"SEED-{i:D5}",
                userId = user[0], userName = user[1], userDepartment = user[2],
                userEmail = $"{user[1].ToLower().Replace(" ", ".")}@university.edu",
                subjectId = subjects[random.Next(subjects.Length)], subjectType = "Student",
                accessType = accessTypes[random.Next(accessTypes.Length)],
                dataCategory = categories[random.Next(categories.Length)],
                purpose = purposes[random.Next(purposes.Length)],
                accessedAt,
                ipAddress = $"10.{random.Next(1, 255)}.{random.Next(1, 255)}.{random.Next(1, 255)}",
                agreementText = "I acknowledge that I am accessing protected financial information in accordance with GLBA requirements and institutional policy.",
                agreementAcknowledgedAt = accessedAt.AddSeconds(-random.Next(5, 45)),
            });
        }
    }

    // One oversized export so the dashboard's anomaly detector has something to flag.
    events.Add(new {
        sourceEventId = "SEED-JUMBO",
        userId = "E509321", userName = "Emily Watson", userDepartment = "Registrar",
        userEmail = "emily.watson@university.edu",
        subjectIds = Enumerable.Range(0, 120).Select(_ => $"S{random.Next(10000000, 99999999)}").Distinct().ToList(),
        subjectType = "Student",
        accessType = "Export",
        dataCategory = "FAFSA Data",
        purpose = "Year-end federal reporting extract",
        accessedAt = DateTime.UtcNow.AddDays(-2),
    });

    using var client = NewApiClient(key);
    var response = await client.PostAsJsonAsync("/api/glba/events/batch", events);
    var body = await response.Content.ReadAsStringAsync();
    Console.WriteLine($"[seed] {(int)response.StatusCode}: {body[..Math.Min(body.Length, 300)]}");
}

static async Task PostLiveEvent(string key)
{
    using var client = NewApiClient(key);
    var response = await client.PostAsJsonAsync("/api/glba/events", new {
        sourceEventId = $"LIVE-{Guid.NewGuid():N}",
        userId = "E999001", userName = "Live Demo",
        userDepartment = "Financial Aid",
        subjectId = "S55555555", subjectType = "Student",
        accessType = "View", dataCategory = "Financial Aid",
        purpose = "Real-time SignalR demonstration",
        accessedAt = DateTime.UtcNow,
    });
    Console.WriteLine($"[live] {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
}
