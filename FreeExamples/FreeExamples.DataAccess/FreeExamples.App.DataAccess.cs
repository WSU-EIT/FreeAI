using System.Collections.Concurrent;
using System.Text.Json;

namespace FreeExamples;

// ---- DataAccess.SampleData ----

public partial interface IDataAccess
{
    DataObjects.BooleanResponse DeleteSampleItems(List<Guid>? ids);
    DataObjects.SampleFileResponse GenerateSampleCsvExport();
    DataObjects.SampleFileResponse GenerateSampleTextFile();
    DataObjects.SampleDashboard GetSampleDashboard();
    DataObjects.SampleGraphData GetSampleGraphData();
    List<DataObjects.SampleItem> GetSampleItems(List<Guid>? ids);
    DataObjects.FilterSampleItems GetSampleItemsFiltered(DataObjects.FilterSampleItems filter);
    List<DataObjects.SampleItem> SaveSampleItems(List<DataObjects.SampleItem> items, DataObjects.User? currentUser);
}

public partial class DataAccess
{
    // In-memory store for sample data. Not persisted to DB — reseeds on app restart.
    private static readonly ConcurrentDictionary<Guid, DataObjects.SampleItem> _sampleItems = new();
    private static bool _sampleDataSeeded = false;

    // --- DeleteMany: soft delete (set Deleted flag) ---
    public DataObjects.BooleanResponse DeleteSampleItems(List<Guid>? ids)
    {
        SeedSampleDataIfNeeded();

        var output = new DataObjects.BooleanResponse();

        if (ids == null || ids.Count == 0) {
            output.Messages.Add("You must provide IDs to delete. Use GetSampleItems first to find them.");
            return output;
        }

        int deleted = 0;
        foreach (var id in ids) {
            if (_sampleItems.TryGetValue(id, out var item)) {
                item.Deleted = true;
                item.DeletedAt = DateTime.UtcNow;
                deleted++;
            }
        }

        output.Result = true;
        output.Messages.Add($"Deleted {deleted} item(s).");
        return output;
    }

    public DataObjects.SampleFileResponse GenerateSampleCsvExport()
    {
        SeedSampleDataIfNeeded();

        var items = _sampleItems.Values.OrderBy(x => x.Name).ToList();
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Name,Category,Status,Priority,Amount,Enabled,DueDate,Added");

        foreach (var item in items) {
            sb.AppendLine($"\"{item.Name}\",\"{item.Category}\",\"{item.Status}\",{item.Priority},{item.Amount},{item.Enabled},{item.DueDate?.ToString("yyyy-MM-dd") ?? ""},{item.Added:yyyy-MM-dd}");
        }

        return new DataObjects.SampleFileResponse {
            FileName = "SampleItems_" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss") + ".csv",
            FileData = System.Text.Encoding.UTF8.GetBytes(sb.ToString()),
        };
    }

    // --- Server-side file generation ---
    public DataObjects.SampleFileResponse GenerateSampleTextFile()
    {
        SeedSampleDataIfNeeded();

        var items = _sampleItems.Values.OrderBy(x => x.Name).ToList();
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("FreeExamples Sample Data Report");
        sb.AppendLine($"Generated: {DateTime.UtcNow:O}");
        sb.AppendLine($"Total Items: {items.Count}");
        sb.AppendLine(new string('-', 60));

        foreach (var item in items) {
            sb.AppendLine($"  {item.Name} | {item.Category} | {item.Status} | ${item.Amount:N2}");
        }

        return new DataObjects.SampleFileResponse {
            FileName = "SampleReport_" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss") + ".txt",
            FileData = System.Text.Encoding.UTF8.GetBytes(sb.ToString()),
        };
    }

    // --- Dashboard aggregate ---
    public DataObjects.SampleDashboard GetSampleDashboard()
    {
        SeedSampleDataIfNeeded();

        var items = _sampleItems.Values.ToList();

        var dashboard = new DataObjects.SampleDashboard {
            TotalItems = items.Count,
            ActiveItems = items.Count(x => x.Status == DataObjects.SampleItemStatus.Active),
            CompletedItems = items.Count(x => x.Status == DataObjects.SampleItemStatus.Completed),
            DraftItems = items.Count(x => x.Status == DataObjects.SampleItemStatus.Draft),
            ArchivedItems = items.Count(x => x.Status == DataObjects.SampleItemStatus.Archived),
        };

        dashboard.ByCategory = items
            .GroupBy(x => x.Category ?? "Uncategorized")
            .Select(g => new DataObjects.SampleCategorySummary {
                Category = g.Key,
                Count = g.Count(),
                TotalAmount = g.Sum(x => x.Amount),
            })
            .OrderBy(x => x.Category)
            .ToList();

        dashboard.ByMonth = items
            .GroupBy(x => x.Added.ToString("yyyy-MM"))
            .Select(g => new DataObjects.SampleTimelineSummary {
                Month = g.Key,
                Added = g.Count(),
                Completed = g.Count(x => x.Status == DataObjects.SampleItemStatus.Completed),
            })
            .OrderBy(x => x.Month)
            .ToList();

        return dashboard;
    }

    // --- Network graph data from sample items ---
    public DataObjects.SampleGraphData GetSampleGraphData()
    {
        SeedSampleDataIfNeeded();

        var items = _sampleItems.Values.ToList();
        var categories = items.Select(x => x.Category ?? "Uncategorized").Distinct().OrderBy(x => x).ToList();

        var graphData = new DataObjects.SampleGraphData();

        // Category nodes (ids 1-N)
        for (int i = 0; i < categories.Count; i++) {
            graphData.Nodes.Add(new DataObjects.SampleGraphNode {
                Id = i + 1,
                Label = categories[i],
                Group = "category",
            });
        }

        // Item nodes (ids 100+)
        int nodeId = 100;
        foreach (var item in items) {
            graphData.Nodes.Add(new DataObjects.SampleGraphNode {
                Id = nodeId,
                Label = item.Name,
                Group = item.Status.ToString().ToLower(),
            });

            // Edge from category → item
            int catIndex = categories.IndexOf(item.Category ?? "Uncategorized");
            graphData.Edges.Add(new DataObjects.SampleGraphEdge {
                From = catIndex + 1,
                To = nodeId,
                Label = item.Status.ToString(),
            });

            nodeId++;
        }

        // Cross-link items with same priority
        var byPriority = items.GroupBy(x => x.Priority).Where(g => g.Count() > 1);
        foreach (var group in byPriority) {
            var groupItems = group.ToList();
            for (int i = 0; i < groupItems.Count - 1 && i < 2; i++) {
                int fromId = 100 + items.IndexOf(groupItems[i]);
                int toId = 100 + items.IndexOf(groupItems[i + 1]);
                graphData.Edges.Add(new DataObjects.SampleGraphEdge {
                    From = fromId,
                    To = toId,
                    Label = $"P{group.Key}",
                });
            }
        }

        return graphData;
    }

    // --- GetMany: null/empty → all (non-deleted), IDs → filtered (includes deleted) ---
    public List<DataObjects.SampleItem> GetSampleItems(List<Guid>? ids)
    {
        SeedSampleDataIfNeeded();

        if (ids != null && ids.Count > 0) {
            return _sampleItems.Values
                .Where(x => ids.Contains(x.SampleItemId))
                .OrderBy(x => x.Name)
                .ToList();
        }

        return _sampleItems.Values.Where(x => !x.Deleted).OrderBy(x => x.Name).ToList();
    }

    // --- Filtered list with search, sort, pagination ---
    public DataObjects.FilterSampleItems GetSampleItemsFiltered(DataObjects.FilterSampleItems filter)
    {
        SeedSampleDataIfNeeded();

        var output = new DataObjects.FilterSampleItems {
            ActionResponse = GetNewActionResponse(true),
            Sort = filter.Sort ?? "name",
            SortOrder = filter.SortOrder ?? "ASC",
            ShowFilters = filter.ShowFilters,
            Keyword = filter.Keyword,
            Status = filter.Status,
            Category = filter.Category,
            Enabled = filter.Enabled,
            IncludeDeletedItems = filter.IncludeDeletedItems,
        };

        var query = _sampleItems.Values.AsEnumerable();

        // Exclude soft-deleted items unless Include Deleted is checked
        if (!filter.IncludeDeletedItems) {
            query = query.Where(x => !x.Deleted);
        }

        // Enabled filter (All / Enabled Only / Disabled Only)
        if (!string.IsNullOrWhiteSpace(filter.Enabled)) {
            if (filter.Enabled.Equals("enabled", StringComparison.OrdinalIgnoreCase)) {
                query = query.Where(x => x.Enabled);
            } else if (filter.Enabled.Equals("disabled", StringComparison.OrdinalIgnoreCase)) {
                query = query.Where(x => !x.Enabled);
            }
        }

        // Text search
        if (!string.IsNullOrWhiteSpace(filter.Keyword)) {
            string keyword = filter.Keyword.ToLower();
            query = query.Where(x =>
                (x.Name != null && x.Name.ToLower().Contains(keyword)) ||
                (x.Description != null && x.Description.ToLower().Contains(keyword)) ||
                (x.Category != null && x.Category.ToLower().Contains(keyword)));
        }

        // Status filter
        if (!string.IsNullOrWhiteSpace(filter.Status) &&
            Enum.TryParse<DataObjects.SampleItemStatus>(filter.Status, true, out var statusVal)) {
            query = query.Where(x => x.Status == statusVal);
        }

        // Category filter
        if (!string.IsNullOrWhiteSpace(filter.Category)) {
            query = query.Where(x => x.Category == filter.Category);
        }

        // Collect distinct categories for filter dropdown
        output.AvailableCategories = _sampleItems.Values
            .Select(x => x.Category ?? "")
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Distinct()
            .OrderBy(c => c)
            .ToList();

        output.RecordCount = query.Count();

        // Sorting
        string sortField = (output.Sort ?? "name").ToLower();
        bool desc = (output.SortOrder ?? "").ToLower() == "desc";

        query = sortField switch {
            "category" => desc ? query.OrderByDescending(x => x.Category) : query.OrderBy(x => x.Category),
            "status" => desc ? query.OrderByDescending(x => x.Status) : query.OrderBy(x => x.Status),
            "priority" => desc ? query.OrderByDescending(x => x.Priority) : query.OrderBy(x => x.Priority),
            "amount" => desc ? query.OrderByDescending(x => x.Amount) : query.OrderBy(x => x.Amount),
            "enabled" => desc ? query.OrderByDescending(x => x.Enabled) : query.OrderBy(x => x.Enabled),
            "added" => desc ? query.OrderByDescending(x => x.Added) : query.OrderBy(x => x.Added),
            "duedate" => desc ? query.OrderByDescending(x => x.DueDate) : query.OrderBy(x => x.DueDate),
            _ => desc ? query.OrderByDescending(x => x.Name) : query.OrderBy(x => x.Name),
        };

        // Pagination
        int page = filter.Page > 0 ? filter.Page : 1;
        int pageSize = filter.RecordsPerPage > 0 ? filter.RecordsPerPage : 10;
        output.Page = page;
        output.RecordsPerPage = pageSize;
        output.PageCount = (int)Math.Ceiling((double)output.RecordCount / pageSize);

        output.Records = query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        // Column definitions for PagedRecordset component
        output.Columns = new List<DataObjects.FilterColumn> {
            new() { Label = "Name", DataElementName = "Name", Sortable = true },
            new() { Label = "Category", DataElementName = "Category", Sortable = true },
            new() { Label = "Status", DataElementName = "Status", Sortable = true },
            new() { Label = "Priority", DataElementName = "Priority", Sortable = true, Align = "center" },
            new() { Label = "Amount", DataElementName = "Amount", Sortable = true, Align = "right", DataType = "decimal" },
            new() { Label = "Enabled", DataElementName = "Enabled", Sortable = true, Align = "center", BooleanIcon = "icon fa-regular fa-square-check" },
        };

        return output;
    }

    // --- SaveMany: PK exists → update, empty/new PK → insert ---
    public List<DataObjects.SampleItem> SaveSampleItems(List<DataObjects.SampleItem> items, DataObjects.User? currentUser)
    {
        SeedSampleDataIfNeeded();

        var saved = new List<DataObjects.SampleItem>();
        string modifiedBy = currentUser?.DisplayName ?? "Unknown";

        foreach (var item in items) {
            if (item.SampleItemId == Guid.Empty) {
                item.SampleItemId = Guid.NewGuid();
            }

            if (_sampleItems.TryGetValue(item.SampleItemId, out var existing)) {
                // Update
                existing.Name = item.Name;
                existing.Description = item.Description;
                existing.Category = item.Category;
                existing.Status = item.Status;
                existing.Priority = item.Priority;
                existing.Amount = item.Amount;
                existing.Enabled = item.Enabled;
                existing.DueDate = item.DueDate;
                existing.LastModified = DateTime.UtcNow;
                existing.LastModifiedBy = modifiedBy;
                saved.Add(existing);
            } else {
                // Insert
                item.Added = DateTime.UtcNow;
                item.AddedBy = modifiedBy;
                item.LastModified = DateTime.UtcNow;
                item.LastModifiedBy = modifiedBy;
                _sampleItems[item.SampleItemId] = item;
                saved.Add(item);
            }
        }

        return saved;
    }

    private void SeedSampleDataIfNeeded()
    {
        if (_sampleDataSeeded) return;
        _sampleDataSeeded = true;

        var categories = new[] { "Engineering", "Marketing", "Operations", "Finance", "Support" };
        var statuses = new[] {
            DataObjects.SampleItemStatus.Draft,
            DataObjects.SampleItemStatus.Active,
            DataObjects.SampleItemStatus.Completed,
            DataObjects.SampleItemStatus.Archived,
        };
        var names = new[] {
            "Website Redesign", "Q4 Budget Review", "Server Migration",
            "Onboarding Workflow", "API Documentation", "Security Audit",
            "Mobile App Launch", "Data Pipeline", "CI/CD Setup",
            "Customer Portal", "Email Templates", "Performance Review",
            "Inventory System", "Compliance Report", "Training Materials",
            "Brand Guidelines", "Help Desk Rollout", "Backup Strategy",
            "Dashboard Widgets", "SSO Integration", "Load Testing",
            "Release Notes", "Vendor Evaluation", "Cost Analysis",
            "Network Diagram",
        };

        var rng = new Random(42); // Deterministic seed for repeatable demo data

        for (int i = 0; i < names.Length; i++) {
            var id = Guid.NewGuid();
            var daysAgo = rng.Next(1, 180);
            var added = DateTime.UtcNow.AddDays(-daysAgo);

            _sampleItems[id] = new DataObjects.SampleItem {
                SampleItemId = id,
                TenantId = Guid.Empty,
                Name = names[i],
                Description = $"Sample description for {names[i]}. This is demo data generated on startup.",
                Category = categories[rng.Next(categories.Length)],
                Status = statuses[rng.Next(statuses.Length)],
                Priority = rng.Next(1, 6),
                Amount = Math.Round((decimal)(rng.NextDouble() * 10000), 2),
                Enabled = rng.NextDouble() > 0.2,
                DueDate = rng.NextDouble() > 0.3 ? DateTime.UtcNow.AddDays(rng.Next(-30, 90)) : null,
                Added = added,
                AddedBy = "System",
                LastModified = added.AddDays(rng.Next(0, Math.Max(1, daysAgo / 2))),
                LastModifiedBy = "System",
            };
        }
    }
}

// ---- DataAccess.JsonStore ----

public partial interface IDataAccess
{
    DataObjects.BooleanResponse DeleteJsonRecords<T>(List<Guid>? ids) where T : class, DataObjects.IJsonEntity;
    List<T> GetJsonRecords<T>(List<Guid>? ids) where T : class, DataObjects.IJsonEntity;
    List<T> SaveJsonRecords<T>(List<T> records, DataObjects.User? currentUser) where T : class, DataObjects.IJsonEntity;
}

public partial class DataAccess
{
    private static readonly ConcurrentDictionary<Guid, DataObjects.JsonRecord> _jsonStore = new();
    private static bool _jsonDataSeeded = false;

    private static readonly JsonSerializerOptions _jsonOptions = new() {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false,
    };

    public DataObjects.BooleanResponse DeleteJsonRecords<T>(List<Guid>? ids) where T : class, DataObjects.IJsonEntity
    {
        SeedJsonDataIfNeeded();
        var output = new DataObjects.BooleanResponse();

        if (ids == null || ids.Count == 0) {
            output.Messages.Add("You must provide IDs to delete.");
            return output;
        }

        int deleted = 0;
        foreach (var id in ids) {
            if (_jsonStore.TryGetValue(id, out var record) && record.RecordType == T.EntityType) {
                record.Deleted = true;
                record.DeletedAt = DateTime.UtcNow;
                deleted++;
            }
        }

        output.Result = true;
        output.Messages.Add($"Deleted {deleted} record(s).");
        return output;
    }

    public List<T> GetJsonRecords<T>(List<Guid>? ids) where T : class, DataObjects.IJsonEntity
    {
        SeedJsonDataIfNeeded();
        string entityType = T.EntityType;

        IEnumerable<DataObjects.JsonRecord> query = _jsonStore.Values
            .Where(r => r.RecordType == entityType);

        if (ids != null && ids.Count > 0) {
            query = query.Where(r => ids.Contains(r.RecordId));
        } else {
            query = query.Where(r => !r.Deleted);
        }

        var results = new List<T>();
        foreach (var record in query) {
            var entity = TryDeserialize<T>(record);
            if (entity != null) results.Add(entity);
        }
        return results;
    }

    public List<T> SaveJsonRecords<T>(List<T> records, DataObjects.User? currentUser) where T : class, DataObjects.IJsonEntity
    {
        SeedJsonDataIfNeeded();
        string modifiedBy = currentUser?.DisplayName ?? "Unknown";
        var saved = new List<T>();

        foreach (var entity in records) {
            if (entity.RecordId == Guid.Empty) {
                entity.RecordId = Guid.NewGuid();
            }

            var json = JsonSerializer.Serialize(entity, _jsonOptions);

            if (_jsonStore.TryGetValue(entity.RecordId, out var existing)) {
                existing.SchemaVersion = T.CurrentSchemaVersion;
                existing.Modified = DateTime.UtcNow;
                existing.ModifiedBy = modifiedBy;
                existing.Contents = json;
            } else {
                _jsonStore[entity.RecordId] = new DataObjects.JsonRecord {
                    RecordId = entity.RecordId,
                    TenantId = entity.TenantId,
                    RecordType = T.EntityType,
                    SchemaVersion = T.CurrentSchemaVersion,
                    Format = "json",
                    Created = DateTime.UtcNow,
                    CreatedBy = modifiedBy,
                    Modified = DateTime.UtcNow,
                    ModifiedBy = modifiedBy,
                    Contents = json,
                };
            }
            saved.Add(entity);
        }
        return saved;
    }

    private void SeedJsonDataIfNeeded()
    {
        if (_jsonDataSeeded) return;
        _jsonDataSeeded = true;

        SeedProjects();
        SeedTickets();
        SeedSprints();
        SeedBoardConfigs();
        SeedWorkOrders();
        SeedBudgetRequests();
        SeedEquipment();
        SeedEvaluations();
        SeedOnboarding();
    }

    private T? TryDeserialize<T>(DataObjects.JsonRecord record) where T : class, DataObjects.IJsonEntity
    {
        if (record.RecordType != T.EntityType) return null;
        if (record.SchemaVersion > T.CurrentSchemaVersion) return null;

        try {
            return JsonSerializer.Deserialize<T>(record.Contents, _jsonOptions);
        } catch {
            return null;
        }
    }
}

// ---- DataAccess.JsonStore.Seed ----

public partial class DataAccess
{
    // ── Seed: Board Configs ──
    private void SeedBoardConfigs()
    {
        var webId = Guid.Parse("a0000001-0000-0000-0000-000000000001");
        var configs = new List<DataObjects.BoardConfig> {
            new() { RecordId = Guid.NewGuid(), ProjectId = webId, BoardName = "Dev Board", BoardType = DataObjects.BoardType.Kanban, ColumnConfig = "[\"Backlog\",\"ToDo\",\"InProgress\",\"InReview\",\"Done\"]", WipLimits = "{\"InProgress\":5,\"InReview\":3}", CreatedBy = "Alice Chen" },
            new() { RecordId = Guid.NewGuid(), ProjectId = webId, BoardName = "Sprint Board", BoardType = DataObjects.BoardType.Sprint, ColumnConfig = "[\"ToDo\",\"InProgress\",\"InReview\",\"Testing\",\"Done\"]", SwimlaneField = "assignee", CreatedBy = "Alice Chen" },
        };
        SaveJsonRecords<DataObjects.BoardConfig>(configs, null);
    }

    // ── Seed: Budget Requests ──
    private void SeedBudgetRequests()
    {
        var requests = new List<DataObjects.BudgetRequest> {
            new() { RecordId = Guid.NewGuid(), Title = "New Lab Laptops", Justification = "Current laptops are 5+ years old and cannot run required software.", Department = "Engineering", FiscalYear = "FY2025", RequestedBy = "Alice Chen", RequestedDate = DateTime.UtcNow.AddDays(-30), Status = DataObjects.BudgetRequestStatus.Approved, AccountCode = "GL-5200", ApprovedAmount = 12500m, SupervisorName = "Dean Roberts", SupervisorDate = DateTime.UtcNow.AddDays(-25), FinanceReviewerName = "CFO Williams", FinanceDate = DateTime.UtcNow.AddDays(-20),
                LineItems = [
                    new() { LineItemId = Guid.NewGuid(), Description = "Dell Latitude 5540", Vendor = "Dell", Quantity = 10, UnitPrice = 1150m, Category = DataObjects.BudgetLineCategory.Equipment },
                    new() { LineItemId = Guid.NewGuid(), Description = "Docking Stations", Vendor = "Dell", Quantity = 10, UnitPrice = 95m, Category = DataObjects.BudgetLineCategory.Equipment },
                    new() { LineItemId = Guid.NewGuid(), Description = "Setup & Imaging", Vendor = "IT Services", Quantity = 1, UnitPrice = 500m, Category = DataObjects.BudgetLineCategory.Services },
                ], TotalAmount = 12450m },
            new() { RecordId = Guid.NewGuid(), Title = "Conference Travel — SIGCSE 2025", Justification = "Two faculty presenting papers at SIGCSE conference.", Department = "Computer Science", FiscalYear = "FY2025", RequestedBy = "Prof. Garcia", RequestedDate = DateTime.UtcNow.AddDays(-15), Status = DataObjects.BudgetRequestStatus.SupervisorApproved, AccountCode = "GL-6100", SupervisorName = "Dean Roberts", SupervisorDate = DateTime.UtcNow.AddDays(-12),
                LineItems = [
                    new() { LineItemId = Guid.NewGuid(), Description = "Airfare (2 travelers)", Quantity = 2, UnitPrice = 450m, Category = DataObjects.BudgetLineCategory.Travel },
                    new() { LineItemId = Guid.NewGuid(), Description = "Hotel (3 nights × 2 rooms)", Quantity = 6, UnitPrice = 189m, Category = DataObjects.BudgetLineCategory.Travel },
                    new() { LineItemId = Guid.NewGuid(), Description = "Registration Fee", Quantity = 2, UnitPrice = 375m, Category = DataObjects.BudgetLineCategory.Services },
                    new() { LineItemId = Guid.NewGuid(), Description = "Per Diem", Quantity = 6, UnitPrice = 75m, Category = DataObjects.BudgetLineCategory.Travel },
                ], TotalAmount = 3384m },
            new() { RecordId = Guid.NewGuid(), Title = "Office Supplies — Q3", Justification = "Quarterly supply order for department.", Department = "Human Resources", FiscalYear = "FY2025", RequestedBy = "Diana Lopez", RequestedDate = DateTime.UtcNow.AddDays(-5), Status = DataObjects.BudgetRequestStatus.Submitted, AccountCode = "GL-4100",
                LineItems = [
                    new() { LineItemId = Guid.NewGuid(), Description = "Copy Paper (cases)", Quantity = 20, UnitPrice = 45m, Category = DataObjects.BudgetLineCategory.Supplies },
                    new() { LineItemId = Guid.NewGuid(), Description = "Toner Cartridges", Vendor = "Staples", Quantity = 8, UnitPrice = 62m, Category = DataObjects.BudgetLineCategory.Supplies },
                    new() { LineItemId = Guid.NewGuid(), Description = "Binder Clips & Folders", Quantity = 1, UnitPrice = 120m, Category = DataObjects.BudgetLineCategory.Supplies },
                ], TotalAmount = 1516m },
            new() { RecordId = Guid.NewGuid(), Title = "Software License Renewal — JetBrains", Justification = "Annual renewal for development team IDE licenses.", Department = "Engineering", FiscalYear = "FY2025", RequestedBy = "Carol Davis", RequestedDate = DateTime.UtcNow.AddDays(-45), Status = DataObjects.BudgetRequestStatus.Completed, AccountCode = "GL-5300", ApprovedAmount = 2997m, SupervisorName = "Alice Chen", SupervisorDate = DateTime.UtcNow.AddDays(-43), FinanceReviewerName = "CFO Williams", FinanceDate = DateTime.UtcNow.AddDays(-40),
                LineItems = [
                    new() { LineItemId = Guid.NewGuid(), Description = "IntelliJ IDEA Ultimate", Vendor = "JetBrains", Quantity = 3, UnitPrice = 599m, Category = DataObjects.BudgetLineCategory.Software },
                    new() { LineItemId = Guid.NewGuid(), Description = "ReSharper", Vendor = "JetBrains", Quantity = 5, UnitPrice = 240m, Category = DataObjects.BudgetLineCategory.Software },
                ], TotalAmount = 2997m },
            new() { RecordId = Guid.NewGuid(), Title = "New Standing Desks", Justification = "Ergonomic improvement per HR wellness initiative.", Department = "Student Services", FiscalYear = "FY2025", RequestedBy = "Grace Kim", RequestedDate = DateTime.UtcNow.AddDays(-3), Status = DataObjects.BudgetRequestStatus.Draft, AccountCode = "GL-5400",
                LineItems = [
                    new() { LineItemId = Guid.NewGuid(), Description = "Electric Standing Desk", Vendor = "Uplift", Quantity = 6, UnitPrice = 649m, Category = DataObjects.BudgetLineCategory.Equipment },
                    new() { LineItemId = Guid.NewGuid(), Description = "Anti-Fatigue Mats", Quantity = 6, UnitPrice = 39m, Category = DataObjects.BudgetLineCategory.Supplies },
                ], TotalAmount = 4128m },
        };
        SaveJsonRecords<DataObjects.BudgetRequest>(requests, null);
    }

    // ── Seed: Equipment ──
    private void SeedEquipment()
    {
        var items = new List<DataObjects.Equipment> {
            new() { RecordId = Guid.NewGuid(), Name = "Dell Latitude 5540 #1", Category = DataObjects.EquipmentCategory.Laptop, AssetTag = "EQ-2024-0001", SerialNumber = "DL5540-A001", Location = "IT Office — Room 105", Condition = DataObjects.EquipmentCondition.Good, PurchaseDate = DateTime.UtcNow.AddDays(-365), PurchasePrice = 1150m,
                Checkouts = [
                    new() { CheckoutId = Guid.NewGuid(), BorrowerName = "Mark Staff", BorrowerEmail = "mstaff@example.edu", BorrowerDepartment = "Marketing", CheckoutDate = DateTime.UtcNow.AddDays(-10), DueDate = DateTime.UtcNow.AddDays(4), ConditionAtCheckout = DataObjects.EquipmentCondition.Good },
                ] },
            new() { RecordId = Guid.NewGuid(), Name = "Dell Latitude 5540 #2", Category = DataObjects.EquipmentCategory.Laptop, AssetTag = "EQ-2024-0002", SerialNumber = "DL5540-A002", Location = "IT Office — Room 105", Condition = DataObjects.EquipmentCondition.Good, PurchaseDate = DateTime.UtcNow.AddDays(-365), PurchasePrice = 1150m },
            new() { RecordId = Guid.NewGuid(), Name = "Epson PowerLite 2250U", Category = DataObjects.EquipmentCategory.Projector, AssetTag = "EQ-2023-0010", SerialNumber = "EP2250U-B010", Location = "AV Closet — Library 2F", Condition = DataObjects.EquipmentCondition.Good, PurchaseDate = DateTime.UtcNow.AddDays(-500), PurchasePrice = 1299m,
                Checkouts = [
                    new() { CheckoutId = Guid.NewGuid(), BorrowerName = "Prof. Garcia", BorrowerEmail = "garcia@example.edu", BorrowerDepartment = "CS", CheckoutDate = DateTime.UtcNow.AddDays(-30), DueDate = DateTime.UtcNow.AddDays(-16), ReturnDate = DateTime.UtcNow.AddDays(-17), ConditionAtCheckout = DataObjects.EquipmentCondition.Good, ConditionAtReturn = DataObjects.EquipmentCondition.Good, Notes = "Used for guest lecture" },
                ] },
            new() { RecordId = Guid.NewGuid(), Name = "Canon EOS R6 Mark II", Category = DataObjects.EquipmentCategory.Camera, AssetTag = "EQ-2024-0015", SerialNumber = "CEOSR6-C015", Location = "Media Lab — Student Union", Condition = DataObjects.EquipmentCondition.New, PurchaseDate = DateTime.UtcNow.AddDays(-60), PurchasePrice = 2499m },
            new() { RecordId = Guid.NewGuid(), Name = "Blue Yeti Microphone", Category = DataObjects.EquipmentCategory.Microphone, AssetTag = "EQ-2023-0020", Location = "Podcast Studio — Library B1", Condition = DataObjects.EquipmentCondition.Fair, PurchaseDate = DateTime.UtcNow.AddDays(-600), PurchasePrice = 129m,
                Checkouts = [
                    new() { CheckoutId = Guid.NewGuid(), BorrowerName = "Lisa Admin", BorrowerEmail = "ladmin@example.edu", CheckoutDate = DateTime.UtcNow.AddDays(-5), DueDate = DateTime.UtcNow.AddDays(-1), ConditionAtCheckout = DataObjects.EquipmentCondition.Fair, Notes = "For accessibility committee recording — OVERDUE" },
                ] },
            new() { RecordId = Guid.NewGuid(), Name = "iPad Pro 12.9\"", Category = DataObjects.EquipmentCategory.Tablet, AssetTag = "EQ-2024-0025", SerialNumber = "IPADPRO-D025", Location = "IT Office — Room 105", Condition = DataObjects.EquipmentCondition.Good, PurchaseDate = DateTime.UtcNow.AddDays(-180), PurchasePrice = 1099m },
            new() { RecordId = Guid.NewGuid(), Name = "T-Mobile 5G Hotspot", Category = DataObjects.EquipmentCategory.Hotspot, AssetTag = "EQ-2024-0030", Location = "Front Desk — Library", Condition = DataObjects.EquipmentCondition.Good, PurchaseDate = DateTime.UtcNow.AddDays(-90), PurchasePrice = 199m },
            new() { RecordId = Guid.NewGuid(), Name = "USB-C Hub Adapter", Category = DataObjects.EquipmentCategory.Adapter, AssetTag = "EQ-2024-0035", Location = "IT Office — Room 105", Condition = DataObjects.EquipmentCondition.Good, PurchaseDate = DateTime.UtcNow.AddDays(-200), PurchasePrice = 45m },
            new() { RecordId = Guid.NewGuid(), Name = "Logitech Webcam C920", Category = DataObjects.EquipmentCategory.Camera, AssetTag = "EQ-2022-0040", Location = "IT Office — Room 105", Condition = DataObjects.EquipmentCondition.NeedsRepair, PurchaseDate = DateTime.UtcNow.AddDays(-900), PurchasePrice = 79m, Description = "USB connector loose — intermittent connection" },
            new() { RecordId = Guid.NewGuid(), Name = "Dell XPS 15 (Retired)", Category = DataObjects.EquipmentCategory.Laptop, AssetTag = "EQ-2020-0050", Location = "Storage — Room B05", Condition = DataObjects.EquipmentCondition.Retired, PurchaseDate = DateTime.UtcNow.AddDays(-1800), PurchasePrice = 1499m, Description = "Battery no longer holds charge. Decommissioned." },
        };
        SaveJsonRecords<DataObjects.Equipment>(items, null);
    }

    // ── Seed: Evaluations ──
    private void SeedEvaluations()
    {
        var templateId = Guid.Parse("d0000001-0000-0000-0000-000000000001");
        var questions = new List<DataObjects.EvalQuestion> {
            new() { QuestionId = Guid.Parse("e0000001-0000-0000-0000-000000000001"), QuestionText = "The instructor explained concepts clearly.", QuestionType = DataObjects.EvalQuestionType.Likert5, IsRequired = true, DisplayOrder = 1 },
            new() { QuestionId = Guid.Parse("e0000001-0000-0000-0000-000000000002"), QuestionText = "Course materials were helpful and well-organized.", QuestionType = DataObjects.EvalQuestionType.Likert5, IsRequired = true, DisplayOrder = 2 },
            new() { QuestionId = Guid.Parse("e0000001-0000-0000-0000-000000000003"), QuestionText = "Assignments were relevant to learning objectives.", QuestionType = DataObjects.EvalQuestionType.Likert5, IsRequired = true, DisplayOrder = 3 },
            new() { QuestionId = Guid.Parse("e0000001-0000-0000-0000-000000000004"), QuestionText = "Overall, how would you rate this course?", QuestionType = DataObjects.EvalQuestionType.Rating10, IsRequired = true, DisplayOrder = 4 },
            new() { QuestionId = Guid.Parse("e0000001-0000-0000-0000-000000000005"), QuestionText = "What did the instructor do well?", QuestionType = DataObjects.EvalQuestionType.FreeText, IsRequired = false, DisplayOrder = 5 },
            new() { QuestionId = Guid.Parse("e0000001-0000-0000-0000-000000000006"), QuestionText = "What could be improved?", QuestionType = DataObjects.EvalQuestionType.FreeText, IsRequired = false, DisplayOrder = 6 },
            new() { QuestionId = Guid.Parse("e0000001-0000-0000-0000-000000000007"), QuestionText = "Would you recommend this course to a friend?", QuestionType = DataObjects.EvalQuestionType.YesNo, IsRequired = true, DisplayOrder = 7 },
        };

        var rng = new Random(200);
        var likertValues = new[] { "1", "2", "3", "4", "5" };
        var freeTextGood = new[] { "Great examples", "Very organized lectures", "Always available for office hours", "Made complex topics simple", "Engaging classroom discussions" };
        var freeTextImprove = new[] { "More hands-on projects", "Slower pace on difficult topics", "Better textbook", "More practice exams", "Earlier feedback on assignments" };

        List<DataObjects.EvalResponse> MakeResponses(int count)
        {
            var responses = new List<DataObjects.EvalResponse>();
            for (int i = 0; i < count; i++) {
                responses.Add(new DataObjects.EvalResponse {
                    ResponseId = Guid.NewGuid(),
                    SubmittedDate = DateTime.UtcNow.AddDays(-rng.Next(1, 20)),
                    Answers = [
                        new() { QuestionId = questions[0].QuestionId, Value = likertValues[rng.Next(2, 5)] },
                        new() { QuestionId = questions[1].QuestionId, Value = likertValues[rng.Next(2, 5)] },
                        new() { QuestionId = questions[2].QuestionId, Value = likertValues[rng.Next(1, 5)] },
                        new() { QuestionId = questions[3].QuestionId, Value = rng.Next(6, 10).ToString() },
                        new() { QuestionId = questions[4].QuestionId, Value = freeTextGood[rng.Next(freeTextGood.Length)] },
                        new() { QuestionId = questions[5].QuestionId, Value = freeTextImprove[rng.Next(freeTextImprove.Length)] },
                        new() { QuestionId = questions[6].QuestionId, Value = rng.NextDouble() > 0.15 ? "Yes" : "No" },
                    ]
                });
            }
            return responses;
        }

        var evals = new List<DataObjects.Evaluation> {
            new() { RecordId = Guid.NewGuid(), Title = "Fall 2024 — CS 101 — Dr. Smith", CourseCode = "CS 101", CourseName = "Intro to Computer Science", InstructorName = "Dr. Smith", Term = "Fall 2024", Department = "Computer Science", OpenDate = DateTime.UtcNow.AddDays(-60), CloseDate = DateTime.UtcNow.AddDays(-30), IsAnonymous = true, EnrollmentCount = 35, TemplateId = templateId, Questions = questions, Responses = MakeResponses(28) },
            new() { RecordId = Guid.NewGuid(), Title = "Fall 2024 — MATH 201 — Prof. Garcia", CourseCode = "MATH 201", CourseName = "Calculus II", InstructorName = "Prof. Garcia", Term = "Fall 2024", Department = "Mathematics", OpenDate = DateTime.UtcNow.AddDays(-60), CloseDate = DateTime.UtcNow.AddDays(-30), IsAnonymous = true, EnrollmentCount = 28, TemplateId = templateId, Questions = questions, Responses = MakeResponses(22) },
            new() { RecordId = Guid.NewGuid(), Title = "Spring 2025 — CS 301 — Dr. Smith", CourseCode = "CS 301", CourseName = "Data Structures", InstructorName = "Dr. Smith", Term = "Spring 2025", Department = "Computer Science", OpenDate = DateTime.UtcNow.AddDays(-10), CloseDate = DateTime.UtcNow.AddDays(10), IsAnonymous = true, EnrollmentCount = 30, TemplateId = templateId, Questions = questions, Responses = MakeResponses(12) },
            new() { RecordId = Guid.NewGuid(), Title = "Spring 2025 — ENG 102 — Ms. Davis", CourseCode = "ENG 102", CourseName = "English Composition", InstructorName = "Ms. Davis", Term = "Spring 2025", Department = "English", OpenDate = DateTime.UtcNow.AddDays(-10), CloseDate = DateTime.UtcNow.AddDays(10), IsAnonymous = true, EnrollmentCount = 25, TemplateId = templateId, Questions = questions, Responses = MakeResponses(8) },
            new() { RecordId = Guid.NewGuid(), Title = "Spring 2025 — BIO 110 — Dr. Nguyen", CourseCode = "BIO 110", CourseName = "General Biology", InstructorName = "Dr. Nguyen", Term = "Spring 2025", Department = "Biology", OpenDate = DateTime.UtcNow.AddDays(5), CloseDate = DateTime.UtcNow.AddDays(25), IsAnonymous = true, EnrollmentCount = 40, TemplateId = templateId, Questions = questions },
        };
        SaveJsonRecords<DataObjects.Evaluation>(evals, null);
    }

    // ── Seed: Onboarding ──
    private void SeedOnboarding()
    {
        List<DataObjects.ChecklistItem> MakeChecklist(DateTime startDate) {
            return [
                new() { ChecklistItemId = Guid.NewGuid(), TaskName = "Complete I-9 form", Category = DataObjects.ChecklistCategory.HR, AssignedTo = "HR Office", IsRequired = true, DueDate = startDate.AddDays(-3), DisplayOrder = 1 },
                new() { ChecklistItemId = Guid.NewGuid(), TaskName = "Submit W-4 tax form", Category = DataObjects.ChecklistCategory.HR, AssignedTo = "HR Office", IsRequired = true, DueDate = startDate.AddDays(-3), DisplayOrder = 2 },
                new() { ChecklistItemId = Guid.NewGuid(), TaskName = "Set up direct deposit", Category = DataObjects.ChecklistCategory.HR, AssignedTo = "HR Office", IsRequired = true, DueDate = startDate.AddDays(-1), DisplayOrder = 3 },
                new() { ChecklistItemId = Guid.NewGuid(), TaskName = "Emergency contact form", Category = DataObjects.ChecklistCategory.HR, AssignedTo = "HR Office", IsRequired = true, DueDate = startDate, DisplayOrder = 4 },
                new() { ChecklistItemId = Guid.NewGuid(), TaskName = "Provision laptop", Category = DataObjects.ChecklistCategory.IT, AssignedTo = "IT Helpdesk", IsRequired = true, DueDate = startDate.AddDays(-1), DisplayOrder = 5 },
                new() { ChecklistItemId = Guid.NewGuid(), TaskName = "Create email account", Category = DataObjects.ChecklistCategory.IT, AssignedTo = "IT Helpdesk", IsRequired = true, DueDate = startDate.AddDays(-2), DisplayOrder = 6 },
                new() { ChecklistItemId = Guid.NewGuid(), TaskName = "Set up VPN access", Category = DataObjects.ChecklistCategory.IT, AssignedTo = "IT Helpdesk", IsRequired = false, DueDate = startDate.AddDays(3), DisplayOrder = 7 },
                new() { ChecklistItemId = Guid.NewGuid(), TaskName = "Assign office/desk", Category = DataObjects.ChecklistCategory.Facilities, AssignedTo = "Facilities Manager", IsRequired = true, DueDate = startDate.AddDays(-1), DisplayOrder = 8 },
                new() { ChecklistItemId = Guid.NewGuid(), TaskName = "Issue building access card", Category = DataObjects.ChecklistCategory.Facilities, AssignedTo = "Facilities Manager", IsRequired = true, DueDate = startDate, DisplayOrder = 9 },
                new() { ChecklistItemId = Guid.NewGuid(), TaskName = "Parking permit", Category = DataObjects.ChecklistCategory.Facilities, AssignedTo = "Facilities Manager", IsRequired = false, DueDate = startDate.AddDays(5), DisplayOrder = 10 },
                new() { ChecklistItemId = Guid.NewGuid(), TaskName = "Department orientation", Category = DataObjects.ChecklistCategory.Department, AssignedTo = "Supervisor", IsRequired = true, DueDate = startDate, DisplayOrder = 11 },
                new() { ChecklistItemId = Guid.NewGuid(), TaskName = "Meet the team lunch", Category = DataObjects.ChecklistCategory.Department, AssignedTo = "Supervisor", IsRequired = false, DueDate = startDate.AddDays(2), DisplayOrder = 12 },
                new() { ChecklistItemId = Guid.NewGuid(), TaskName = "Safety training video", Category = DataObjects.ChecklistCategory.Training, AssignedTo = "Training Portal", IsRequired = true, DueDate = startDate.AddDays(5), DisplayOrder = 13 },
                new() { ChecklistItemId = Guid.NewGuid(), TaskName = "FERPA compliance training", Category = DataObjects.ChecklistCategory.Compliance, AssignedTo = "Training Portal", IsRequired = true, DueDate = startDate.AddDays(10), DisplayOrder = 14 },
                new() { ChecklistItemId = Guid.NewGuid(), TaskName = "Cybersecurity awareness training", Category = DataObjects.ChecklistCategory.Compliance, AssignedTo = "Training Portal", IsRequired = true, DueDate = startDate.AddDays(10), DisplayOrder = 15 },
            ];
        }

        void MarkComplete(List<DataObjects.ChecklistItem> items, int count) {
            for (int i = 0; i < Math.Min(count, items.Count); i++) {
                items[i].IsCompleted = true;
                items[i].CompletedDate = DateTime.UtcNow.AddDays(-new Random(i).Next(1, 10));
                items[i].CompletedBy = items[i].AssignedTo;
            }
        }

        var cl1 = MakeChecklist(DateTime.UtcNow.AddDays(-14)); MarkComplete(cl1, 15);
        var cl2 = MakeChecklist(DateTime.UtcNow.AddDays(-7)); MarkComplete(cl2, 10);
        var cl3 = MakeChecklist(DateTime.UtcNow.AddDays(-3)); MarkComplete(cl3, 5);
        var cl4 = MakeChecklist(DateTime.UtcNow.AddDays(3));
        var cl5 = MakeChecklist(DateTime.UtcNow.AddDays(-10)); MarkComplete(cl5, 12);
        var cl6 = MakeChecklist(DateTime.UtcNow.AddDays(7));

        var onboardings = new List<DataObjects.Onboarding> {
            new() { RecordId = Guid.NewGuid(), EmployeeName = "James Wilson", EmployeeEmail = "jwilson@example.edu", EmployeeTitle = "Software Developer", Department = "Engineering", HireDate = DateTime.UtcNow.AddDays(-21), StartDate = DateTime.UtcNow.AddDays(-14), SupervisorName = "Alice Chen", MentorName = "Carol Davis", EmploymentType = DataObjects.EmploymentType.FullTime, Status = DataObjects.OnboardingStatus.Completed, ChecklistItems = cl1 },
            new() { RecordId = Guid.NewGuid(), EmployeeName = "Maria Santos", EmployeeEmail = "msantos@example.edu", EmployeeTitle = "HR Coordinator", Department = "Human Resources", HireDate = DateTime.UtcNow.AddDays(-14), StartDate = DateTime.UtcNow.AddDays(-7), SupervisorName = "Diana Lopez", EmploymentType = DataObjects.EmploymentType.FullTime, Status = DataObjects.OnboardingStatus.InProgress, ChecklistItems = cl2 },
            new() { RecordId = Guid.NewGuid(), EmployeeName = "Kevin Park", EmployeeEmail = "kpark@example.edu", EmployeeTitle = "Research Assistant", Department = "Biology", HireDate = DateTime.UtcNow.AddDays(-10), StartDate = DateTime.UtcNow.AddDays(-3), SupervisorName = "Dr. Nguyen", EmploymentType = DataObjects.EmploymentType.GradAssistant, Status = DataObjects.OnboardingStatus.InProgress, ChecklistItems = cl3 },
            new() { RecordId = Guid.NewGuid(), EmployeeName = "Emily Turner", EmployeeEmail = "eturner@example.edu", EmployeeTitle = "Academic Advisor", Department = "Student Services", HireDate = DateTime.UtcNow.AddDays(-3), StartDate = DateTime.UtcNow.AddDays(3), SupervisorName = "Grace Kim", MentorName = "Fran Nguyen", EmploymentType = DataObjects.EmploymentType.FullTime, Status = DataObjects.OnboardingStatus.Pending, ChecklistItems = cl4 },
            new() { RecordId = Guid.NewGuid(), EmployeeName = "Tyler Brooks", EmployeeEmail = "tbrooks@example.edu", EmployeeTitle = "Lab Technician", Department = "Engineering", HireDate = DateTime.UtcNow.AddDays(-17), StartDate = DateTime.UtcNow.AddDays(-10), SupervisorName = "Alice Chen", EmploymentType = DataObjects.EmploymentType.PartTime, Status = DataObjects.OnboardingStatus.InProgress, ChecklistItems = cl5 },
            new() { RecordId = Guid.NewGuid(), EmployeeName = "Aisha Rahman", EmployeeEmail = "arahman@example.edu", EmployeeTitle = "Student Worker", Department = "Library", HireDate = DateTime.UtcNow, StartDate = DateTime.UtcNow.AddDays(7), SupervisorName = "Lisa Admin", EmploymentType = DataObjects.EmploymentType.StudentWorker, Status = DataObjects.OnboardingStatus.Pending, ChecklistItems = cl6 },
        };
        SaveJsonRecords<DataObjects.Onboarding>(onboardings, null);
    }
    // ── Seed: Projects ──
    private void SeedProjects()
    {
        var projects = new List<DataObjects.Project> {
            new() { RecordId = Guid.Parse("a0000001-0000-0000-0000-000000000001"), Name = "Website Redesign", ProjectKey = "WEB", LeadName = "Alice Chen", LeadEmail = "achen@example.edu", Department = "Engineering", Status = DataObjects.ProjectStatus.Active, StartDate = DateTime.UtcNow.AddDays(-60), TargetEndDate = DateTime.UtcNow.AddDays(30), Color = "#0d6efd", SortOrder = 1, NextTicketNumber = 13 },
            new() { RecordId = Guid.Parse("a0000001-0000-0000-0000-000000000002"), Name = "Frontend", ProjectKey = "FE", LeadName = "Bob Martinez", Department = "Engineering", Status = DataObjects.ProjectStatus.Active, ParentProjectId = Guid.Parse("a0000001-0000-0000-0000-000000000001"), Color = "#198754", SortOrder = 1, NextTicketNumber = 8 },
            new() { RecordId = Guid.Parse("a0000001-0000-0000-0000-000000000003"), Name = "Backend API", ProjectKey = "API", LeadName = "Carol Davis", Department = "Engineering", Status = DataObjects.ProjectStatus.Active, ParentProjectId = Guid.Parse("a0000001-0000-0000-0000-000000000001"), Color = "#6f42c1", SortOrder = 2, NextTicketNumber = 6 },
            new() { RecordId = Guid.Parse("a0000001-0000-0000-0000-000000000004"), Name = "Navigation", ProjectKey = "NAV", LeadName = "Bob Martinez", Department = "Engineering", Status = DataObjects.ProjectStatus.Active, ParentProjectId = Guid.Parse("a0000001-0000-0000-0000-000000000002"), Color = "#20c997", SortOrder = 1, NextTicketNumber = 4 },
            new() { RecordId = Guid.Parse("a0000001-0000-0000-0000-000000000005"), Name = "HR Portal", ProjectKey = "HR", LeadName = "Diana Lopez", LeadEmail = "dlopez@example.edu", Department = "Human Resources", Status = DataObjects.ProjectStatus.Planning, StartDate = DateTime.UtcNow.AddDays(15), TargetEndDate = DateTime.UtcNow.AddDays(120), Color = "#dc3545", SortOrder = 2, NextTicketNumber = 5 },
            new() { RecordId = Guid.Parse("a0000001-0000-0000-0000-000000000006"), Name = "Campus Safety App", ProjectKey = "SAF", LeadName = "Ed Thompson", Department = "Campus Safety", Status = DataObjects.ProjectStatus.Completed, StartDate = DateTime.UtcNow.AddDays(-120), TargetEndDate = DateTime.UtcNow.AddDays(-10), Color = "#ffc107", SortOrder = 3, NextTicketNumber = 9 },
            new() { RecordId = Guid.Parse("a0000001-0000-0000-0000-000000000007"), Name = "Data Migration", ProjectKey = "MIG", LeadName = "Fran Nguyen", Department = "IT", Status = DataObjects.ProjectStatus.OnHold, Color = "#6c757d", SortOrder = 4, NextTicketNumber = 3 },
            new() { RecordId = Guid.Parse("a0000001-0000-0000-0000-000000000008"), Name = "Student Portal", ProjectKey = "STU", LeadName = "Grace Kim", LeadEmail = "gkim@example.edu", Department = "Student Services", Status = DataObjects.ProjectStatus.Active, StartDate = DateTime.UtcNow.AddDays(-30), TargetEndDate = DateTime.UtcNow.AddDays(90), Color = "#0dcaf0", SortOrder = 5, NextTicketNumber = 7 },
        };
        SaveJsonRecords<DataObjects.Project>(projects, null);
    }

    // ── Seed: Sprints ──
    private void SeedSprints()
    {
        var webId = Guid.Parse("a0000001-0000-0000-0000-000000000001");
        var stuId = Guid.Parse("a0000001-0000-0000-0000-000000000008");

        var sprints = new List<DataObjects.Sprint> {
            new() { RecordId = Guid.Parse("b0000001-0000-0000-0000-000000000001"), ProjectId = webId, Name = "Sprint 1 — Foundation", Goal = "Core layout and API schema done", StartDate = DateTime.UtcNow.AddDays(-28), EndDate = DateTime.UtcNow.AddDays(-14), Status = DataObjects.SprintStatus.Completed, CapacityPoints = 21 },
            new() { RecordId = Guid.Parse("b0000001-0000-0000-0000-000000000002"), ProjectId = webId, Name = "Sprint 2 — Navigation & Auth", Goal = "Responsive nav and auth middleware", StartDate = DateTime.UtcNow.AddDays(-14), EndDate = DateTime.UtcNow.AddDays(0), Status = DataObjects.SprintStatus.Active, CapacityPoints = 26 },
            new() { RecordId = Guid.Parse("b0000001-0000-0000-0000-000000000003"), ProjectId = webId, Name = "Sprint 3 — Polish", Goal = "Bug fixes and performance", StartDate = DateTime.UtcNow.AddDays(1), EndDate = DateTime.UtcNow.AddDays(15), Status = DataObjects.SprintStatus.Planning, CapacityPoints = 21 },
            new() { RecordId = Guid.Parse("b0000001-0000-0000-0000-000000000004"), ProjectId = stuId, Name = "STU Sprint 1", Goal = "Course registration MVP", StartDate = DateTime.UtcNow.AddDays(-14), EndDate = DateTime.UtcNow.AddDays(0), Status = DataObjects.SprintStatus.Active, CapacityPoints = 18 },
            // Historical sprints for velocity charts
            new() { RecordId = Guid.Parse("b0000001-0000-0000-0000-000000000005"), ProjectId = webId, Name = "Sprint 0 — Setup", Goal = "Project setup and tooling", StartDate = DateTime.UtcNow.AddDays(-56), EndDate = DateTime.UtcNow.AddDays(-42), Status = DataObjects.SprintStatus.Completed, CapacityPoints = 13 },
            new() { RecordId = Guid.Parse("b0000001-0000-0000-0000-000000000006"), ProjectId = webId, Name = "Sprint -1 — Discovery", Goal = "Requirements gathering", StartDate = DateTime.UtcNow.AddDays(-70), EndDate = DateTime.UtcNow.AddDays(-56), Status = DataObjects.SprintStatus.Completed, CapacityPoints = 8 },
        };
        SaveJsonRecords<DataObjects.Sprint>(sprints, null);
    }

    // ── Seed: Tickets ──
    private void SeedTickets()
    {
        var webId = Guid.Parse("a0000001-0000-0000-0000-000000000001");
        var feId = Guid.Parse("a0000001-0000-0000-0000-000000000002");
        var apiId = Guid.Parse("a0000001-0000-0000-0000-000000000003");
        var hrId = Guid.Parse("a0000001-0000-0000-0000-000000000005");
        var stuId = Guid.Parse("a0000001-0000-0000-0000-000000000008");
        var sprint1 = Guid.Parse("b0000001-0000-0000-0000-000000000001");
        var sprint2 = Guid.Parse("b0000001-0000-0000-0000-000000000002");

        var tickets = new List<DataObjects.Ticket> {
            // Website Redesign tickets
            new() { RecordId = Guid.Parse("c0000001-0000-0000-0000-000000000001"), ProjectId = webId, TicketNumber = "WEB-1", Title = "Design new homepage layout", Type = DataObjects.TicketType.Story, Status = DataObjects.TicketStatus.Done, Priority = DataObjects.TicketPriority.High, AssignedTo = "Bob Martinez", ReporterName = "Alice Chen", StoryPoints = 8, SprintId = sprint1, Labels = "design,frontend", CompletedDate = DateTime.UtcNow.AddDays(-20), SortOrder = 1,
                Comments = [ new() { CommentId = Guid.NewGuid(), AuthorName = "Alice Chen", Body = "Great work on the hero section!", CreatedDate = DateTime.UtcNow.AddDays(-21) } ] },
            new() { RecordId = Guid.Parse("c0000001-0000-0000-0000-000000000002"), ProjectId = webId, TicketNumber = "WEB-2", Title = "Implement responsive navigation", Type = DataObjects.TicketType.Story, Status = DataObjects.TicketStatus.InProgress, Priority = DataObjects.TicketPriority.High, AssignedTo = "Bob Martinez", ReporterName = "Alice Chen", StoryPoints = 5, SprintId = sprint2, Labels = "frontend,nav", StartedDate = DateTime.UtcNow.AddDays(-5), SortOrder = 2 },
            new() { RecordId = Guid.Parse("c0000001-0000-0000-0000-000000000003"), ProjectId = webId, TicketNumber = "WEB-3", Title = "Fix broken image links on About page", Type = DataObjects.TicketType.Bug, Status = DataObjects.TicketStatus.ToDo, Priority = DataObjects.TicketPriority.Medium, ReporterName = "Diana Lopez", SprintId = sprint2, SortOrder = 3 },
            new() { RecordId = Guid.Parse("c0000001-0000-0000-0000-000000000004"), ProjectId = webId, TicketNumber = "WEB-4", Title = "Add dark mode support", Type = DataObjects.TicketType.Story, Status = DataObjects.TicketStatus.Backlog, Priority = DataObjects.TicketPriority.Low, ReporterName = "Bob Martinez", StoryPoints = 13, Labels = "enhancement,frontend", SortOrder = 4 },
            new() { RecordId = Guid.Parse("c0000001-0000-0000-0000-000000000005"), ProjectId = webId, TicketNumber = "WEB-5", Title = "Performance audit and optimization", Type = DataObjects.TicketType.Task, Status = DataObjects.TicketStatus.Backlog, Priority = DataObjects.TicketPriority.Medium, ReporterName = "Carol Davis", StoryPoints = 8, Labels = "performance", SortOrder = 5 },
            new() { RecordId = Guid.Parse("c0000001-0000-0000-0000-000000000006"), ProjectId = webId, TicketNumber = "WEB-6", Title = "Website Redesign Epic", Type = DataObjects.TicketType.Epic, Status = DataObjects.TicketStatus.InProgress, Priority = DataObjects.TicketPriority.High, AssignedTo = "Alice Chen", ReporterName = "Alice Chen", StoryPoints = 21, SortOrder = 0 },

            // Frontend tickets
            new() { RecordId = Guid.Parse("c0000001-0000-0000-0000-000000000007"), ProjectId = feId, TicketNumber = "FE-1", Title = "Create reusable card component", Type = DataObjects.TicketType.Task, Status = DataObjects.TicketStatus.Done, Priority = DataObjects.TicketPriority.Medium, AssignedTo = "Bob Martinez", ReporterName = "Bob Martinez", StoryPoints = 3, SprintId = sprint1, CompletedDate = DateTime.UtcNow.AddDays(-18), SortOrder = 1 },
            new() { RecordId = Guid.Parse("c0000001-0000-0000-0000-000000000008"), ProjectId = feId, TicketNumber = "FE-2", Title = "Form validation not showing errors", Type = DataObjects.TicketType.Bug, Status = DataObjects.TicketStatus.InReview, Priority = DataObjects.TicketPriority.High, AssignedTo = "Bob Martinez", ReporterName = "Diana Lopez", StoryPoints = 3, SprintId = sprint2, StartedDate = DateTime.UtcNow.AddDays(-3), SortOrder = 2 },
            new() { RecordId = Guid.Parse("c0000001-0000-0000-0000-000000000009"), ProjectId = feId, TicketNumber = "FE-3", Title = "Accessibility audit for all forms", Type = DataObjects.TicketType.Task, Status = DataObjects.TicketStatus.Backlog, Priority = DataObjects.TicketPriority.Medium, ReporterName = "Grace Kim", StoryPoints = 5, Labels = "accessibility", SortOrder = 3 },

            // Backend API tickets
            new() { RecordId = Guid.Parse("c0000001-0000-0000-0000-000000000010"), ProjectId = apiId, TicketNumber = "API-1", Title = "Design REST API schema", Type = DataObjects.TicketType.Story, Status = DataObjects.TicketStatus.Done, Priority = DataObjects.TicketPriority.Critical, AssignedTo = "Carol Davis", ReporterName = "Alice Chen", StoryPoints = 8, SprintId = sprint1, CompletedDate = DateTime.UtcNow.AddDays(-15), SortOrder = 1 },
            new() { RecordId = Guid.Parse("c0000001-0000-0000-0000-000000000011"), ProjectId = apiId, TicketNumber = "API-2", Title = "Implement authentication middleware", Type = DataObjects.TicketType.Story, Status = DataObjects.TicketStatus.Testing, Priority = DataObjects.TicketPriority.Critical, AssignedTo = "Carol Davis", ReporterName = "Carol Davis", StoryPoints = 8, SprintId = sprint2, StartedDate = DateTime.UtcNow.AddDays(-7), SortOrder = 2 },
            new() { RecordId = Guid.Parse("c0000001-0000-0000-0000-000000000012"), ProjectId = apiId, TicketNumber = "API-3", Title = "Rate limiting returns wrong HTTP status", Type = DataObjects.TicketType.Bug, Status = DataObjects.TicketStatus.ToDo, Priority = DataObjects.TicketPriority.High, ReporterName = "Ed Thompson", SprintId = sprint2, StoryPoints = 2, SortOrder = 3 },

            // HR Portal tickets
            new() { RecordId = Guid.Parse("c0000001-0000-0000-0000-000000000013"), ProjectId = hrId, TicketNumber = "HR-1", Title = "Employee directory page", Type = DataObjects.TicketType.Story, Status = DataObjects.TicketStatus.Backlog, Priority = DataObjects.TicketPriority.High, ReporterName = "Diana Lopez", StoryPoints = 8, SortOrder = 1 },
            new() { RecordId = Guid.Parse("c0000001-0000-0000-0000-000000000014"), ProjectId = hrId, TicketNumber = "HR-2", Title = "PTO request form", Type = DataObjects.TicketType.Story, Status = DataObjects.TicketStatus.Backlog, Priority = DataObjects.TicketPriority.Medium, ReporterName = "Diana Lopez", StoryPoints = 5, SortOrder = 2 },
            new() { RecordId = Guid.Parse("c0000001-0000-0000-0000-000000000015"), ProjectId = hrId, TicketNumber = "HR-3", Title = "Benefits enrollment wizard", Type = DataObjects.TicketType.Epic, Status = DataObjects.TicketStatus.Backlog, Priority = DataObjects.TicketPriority.Medium, ReporterName = "Diana Lopez", StoryPoints = 21, SortOrder = 3 },

            // Student Portal tickets
            new() { RecordId = Guid.Parse("c0000001-0000-0000-0000-000000000016"), ProjectId = stuId, TicketNumber = "STU-1", Title = "Course registration flow", Type = DataObjects.TicketType.Story, Status = DataObjects.TicketStatus.InProgress, Priority = DataObjects.TicketPriority.Critical, AssignedTo = "Grace Kim", ReporterName = "Grace Kim", StoryPoints = 13, SprintId = sprint2, StartedDate = DateTime.UtcNow.AddDays(-4), SortOrder = 1 },
            new() { RecordId = Guid.Parse("c0000001-0000-0000-0000-000000000017"), ProjectId = stuId, TicketNumber = "STU-2", Title = "Transcript download crashes on Safari", Type = DataObjects.TicketType.Bug, Status = DataObjects.TicketStatus.Backlog, Priority = DataObjects.TicketPriority.High, ReporterName = "Fran Nguyen", Labels = "browser,safari", SortOrder = 2 },
            new() { RecordId = Guid.Parse("c0000001-0000-0000-0000-000000000018"), ProjectId = stuId, TicketNumber = "STU-3", Title = "Add GPA calculator widget", Type = DataObjects.TicketType.Improvement, Status = DataObjects.TicketStatus.Backlog, Priority = DataObjects.TicketPriority.Low, ReporterName = "Grace Kim", StoryPoints = 5, SortOrder = 3 },
        };
        SaveJsonRecords<DataObjects.Ticket>(tickets, null);
    }

    // ── Seed: Work Orders ──
    private void SeedWorkOrders()
    {
        var rng = new Random(100);
        var buildings = new[] { "Science Hall", "Library", "Student Union", "Engineering Bldg", "Admin Tower" };
        var floors = new[] { "1st Floor", "2nd Floor", "3rd Floor", "Basement" };
        var rooms = new[] { "101", "205", "310", "B12", "118", "220", "315" };
        var techs = new[] { "Mike Johnson", "Sarah Wilson", "Tom Brown", null };
        var teams = new[] { "Plumbing Crew", "Electrical Crew", "HVAC Team", "General Maintenance" };
        var requesters = new[] { ("Jane Faculty", "jfaculty@example.edu"), ("Mark Staff", "mstaff@example.edu"), ("Lisa Admin", "ladmin@example.edu"), ("Prof. Garcia", "garcia@example.edu"), ("Dean Roberts", "droberts@example.edu") };

        var orders = new List<DataObjects.WorkOrder>();
        var titles = new[] {
            "Leaking faucet in restroom", "Flickering lights in hallway", "AC not working — room too hot",
            "Broken door lock", "Clogged drain in lab sink", "Power outlet sparking", "Heating vent blocked",
            "Window won't close properly", "Elevator stuck on 2nd floor", "Parking lot light out",
            "Graffiti on exterior wall", "Water stain on ceiling tile", "Thermostat reads wrong temp",
            "Emergency exit sign burnt out", "Toilet running continuously", "Carpet stain near entrance",
            "Mouse spotted in break room", "Squeaky door hinge", "Broken blinds in classroom"
        };
        var categories = new[] { DataObjects.WorkOrderCategory.Plumbing, DataObjects.WorkOrderCategory.Electrical, DataObjects.WorkOrderCategory.HVAC, DataObjects.WorkOrderCategory.Custodial, DataObjects.WorkOrderCategory.Plumbing, DataObjects.WorkOrderCategory.Electrical, DataObjects.WorkOrderCategory.HVAC, DataObjects.WorkOrderCategory.Other, DataObjects.WorkOrderCategory.Other, DataObjects.WorkOrderCategory.Grounds, DataObjects.WorkOrderCategory.Custodial, DataObjects.WorkOrderCategory.Plumbing, DataObjects.WorkOrderCategory.HVAC, DataObjects.WorkOrderCategory.Electrical, DataObjects.WorkOrderCategory.Plumbing, DataObjects.WorkOrderCategory.Custodial, DataObjects.WorkOrderCategory.Other, DataObjects.WorkOrderCategory.Other, DataObjects.WorkOrderCategory.Other };
        var statuses = new[] { DataObjects.WorkOrderStatus.Submitted, DataObjects.WorkOrderStatus.Assigned, DataObjects.WorkOrderStatus.InProgress, DataObjects.WorkOrderStatus.Completed, DataObjects.WorkOrderStatus.Closed };

        for (int i = 0; i < titles.Length; i++) {
            var req = requesters[rng.Next(requesters.Length)];
            var status = statuses[rng.Next(statuses.Length)];
            var tech = status >= DataObjects.WorkOrderStatus.Assigned ? techs[rng.Next(techs.Length)] : null;
            orders.Add(new DataObjects.WorkOrder {
                RecordId = Guid.NewGuid(),
                Title = titles[i],
                Description = $"Reported issue: {titles[i]}. Please address as soon as possible.",
                Building = buildings[rng.Next(buildings.Length)],
                Floor = floors[rng.Next(floors.Length)],
                RoomNumber = rooms[rng.Next(rooms.Length)],
                Category = categories[i],
                Urgency = (DataObjects.WorkOrderUrgency)rng.Next(4),
                Status = status,
                AssignedTo = tech,
                AssignedTeam = tech != null ? teams[rng.Next(teams.Length)] : null,
                RequestedBy = req.Item1,
                RequestedByEmail = req.Item2,
                RequestedDate = DateTime.UtcNow.AddDays(-rng.Next(1, 45)),
                CompletedDate = status >= DataObjects.WorkOrderStatus.Completed ? DateTime.UtcNow.AddDays(-rng.Next(0, 5)) : null,
                EstimatedHours = Math.Round((decimal)(rng.NextDouble() * 4 + 0.5), 1),
                ActualHours = status >= DataObjects.WorkOrderStatus.Completed ? Math.Round((decimal)(rng.NextDouble() * 5 + 0.5), 1) : null,
            });
        }
        SaveJsonRecords<DataObjects.WorkOrder>(orders, null);
    }
}

// ---- DataAccess.Language ----

public partial class DataAccess
{
    /// <summary>
    /// FreeExamples' language tags (page titles and labels for the example pages).
    /// Returned by AppLanguage in DataAccess.App.cs.
    /// </summary>
    private Dictionary<string, string> FreeExamplesAppLanguage {
        get {
            return new Dictionary<string, string> {
                // Dashboard & Data
                { "ExamplesDashboard", "Examples Dashboard" },
                { "DashboardData", "Dashboard & Data" },
                { "SampleItems", "Sample Items" },
                { "EditSampleItem", "Edit Sample Item" },
                { "SampleItemsV1", "Card View" },
                { "SampleItemsV2", "Split Panel" },
                { "SampleItemsV3", "Grouped View" },
                { "SampleItemsV4", "Timeline" },
                { "SampleItemsV5", "Stats Dashboard" },
                { "SearchAutocomplete", "Search & Autocomplete" },
                { "ComparisonTable", "Comparison Table" },
                { "ItemCards", "Item Cards" },

                // Files & Media
                { "FilesMedia", "Files & Media" },
                { "FileDemo", "File Demo" },
                { "FileDemoV1", "Profile Photo" },
                { "FileDemoV2", "Document Library" },
                { "FileDemoV3", "Resume Upload" },
                { "FileDemoV4", "Bulk Import" },
                { "FileDemoV5", "Case Attachments" },
                { "FileDemoV6", "Upload Policy" },
                { "ImageGallery", "Image Gallery" },
                { "Carousel", "Carousel" },
                { "SignatureDemo", "Signature Demo" },
                { "SignatureV1", "Job Application" },
                { "SignatureV2", "Doc Acknowledgment" },
                { "SignatureV3", "Upload & Sign" },
                { "SignatureV4", "GLBA Consent" },
                { "SignatureV5", "Contract Signing" },

                // UI Components
                { "UIComponents", "UI Components" },
                { "BootstrapShowcase", "Bootstrap Showcase" },
                { "BootstrapV1", "Email Builder" },
                { "BootstrapV2", "Settings Page" },
                { "BootstrapV3", "Error Pages" },
                { "BootstrapV4", "User Profile" },
                { "BootstrapV5", "Pricing Page" },
                { "BootstrapV6", "Filter Panel" },
                { "BootstrapV7", "Settings Admin" },
                { "BootstrapV8", "Offcanvas Sidebar" },
                { "BootstrapV9", "Master-Detail" },
                { "BootstrapV10", "Modals & Toasts" },
                { "BootstrapV11", "Data Table" },
                { "BootstrapV12", "Request Form" },
                { "KanbanBoard", "Kanban Board" },
                { "StatusBoard", "Status Board" },
                { "PipelineTracker", "Pipeline Tracker" },
                { "WizardDemo", "Wizard Demo" },
                { "CommandPalette", "Command Palette" },
                { "CommentThread", "Comment Thread" },
                { "ChatView", "Chat View" },

                // Charts & Viz
                { "ChartsViz", "Charts & Viz" },
                { "ChartsDashboard", "Charts Dashboard" },
                { "ChartsV1", "Sales Analytics" },
                { "ChartsV2", "Enrollment Stats" },
                { "ChartsV3", "System Health" },
                { "ChartsV4", "HR Analytics" },
                { "ChartsV5", "Web Analytics" },
                { "NetworkGraph", "Network Graph" },
                { "NetworkGraphV1", "Org Chart" },
                { "NetworkGraphV2", "Dependency Map" },

                // Code & Real-Time
                { "CodeRealTime", "Code & Real-Time" },
                { "CodeEditor", "Code Editor" },
                { "CodeEditorV1", "SQL Query Builder" },
                { "CodeEditorV2", "API Tester" },
                { "CodeEditorV3", "Config Editor" },
                { "CodeEditorV4", "Diff Viewer" },
                { "CodeEditorV5", "Template Engine" },
                { "CodePlayground", "Code Playground" },
                { "SignalRDemo", "SignalR Demo" },
                { "SignalRV1", "Live Notifications" },
                { "SignalRV2", "Online Users" },
                { "SignalRV3", "Live Poll" },
                { "SignalRV4", "Activity Feed" },
                { "SignalRV5", "Live Scoreboard" },
                { "TimerDemo", "Timer Demo" },
                { "TimerV1", "Pomodoro" },
                { "TimerV2", "Session Timeout" },
                { "TimerV3", "Auto-Refresh" },
                { "TimerV4", "Quiz Timer" },
                { "TimerV5", "Event Countdown" },
                { "GitBrowser", "Git Browser" },
                { "ApiKeyDemo", "API Key Demo" },
            };
        }
    }
}
