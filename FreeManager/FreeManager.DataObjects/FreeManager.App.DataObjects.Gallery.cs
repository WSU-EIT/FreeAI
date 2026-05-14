namespace FreeManager;

#region FreeManager Platform - Template Gallery
// ┌─────────────────────────────────────────────────────────────────┐
// │          DataObjects.App.FreeManager.Gallery.cs                 │
// │              Template Gallery & Built-in Templates               │
// ├─────────────────────────────────────────────────────────────────┤
// │ BUILT-IN ENTITY TEMPLATES                                       │
// │   BuiltInEntityTemplates.All → Ready-to-use entity starters:    │
// │     • Basic CRUD        → Simple name/description entity        │
// │     • Task/Ticket       → Status, priority, assignment          │
// │     • Customer/Contact  → Person with email, phone, address     │
// │     • Product/Inventory → SKU, price, quantity                  │
// │     • Document/Content  → Title, body, versioning               │
// ├─────────────────────────────────────────────────────────────────┤
// │ GALLERY TEMPLATE INFO                                           │
// │   GalleryTemplateInfo    → Template card display info           │
// │   GalleryTemplateType    → Entity, Project, or Page template    │
// ├─────────────────────────────────────────────────────────────────┤
// │ GALLERY CATALOG                                                 │
// │   GalleryTemplates.All   → Unified list of all templates        │
// │   GalleryTemplates.ByType(type) → Filter by template type       │
// └─────────────────────────────────────────────────────────────────┘
// Part of: DataObjects.App.FreeManager (partial)
// ============================================================================

public partial class DataObjects
{
    // ============================================================
    // BUILT-IN ENTITY TEMPLATES
    // ============================================================

    /// <summary>
    /// Built-in entity templates for quick start.
    /// </summary>
    public static class BuiltInEntityTemplates
    {
        public static readonly List<SavedEntityTemplate> All = new()
        {
            new SavedEntityTemplate
            {
                Id = Guid.Parse("00000001-0000-0000-0000-000000000001"),
                Name = "Basic CRUD",
                Description = "Simple entity with name, description, and standard fields",
                Icon = "fa-table",
                IsBuiltIn = true,
                Entity = new EntityDefinition
                {
                    Name = "Item",
                    PluralName = "Items",
                    PrimaryKeyName = "ItemId",
                    PrimaryKeyType = "Guid"
                }
            },
            new SavedEntityTemplate
            {
                Id = Guid.Parse("00000001-0000-0000-0000-000000000002"),
                Name = "Task/Ticket",
                Description = "Task tracking with status, priority, and assignment",
                Icon = "fa-list-check",
                IsBuiltIn = true,
                Entity = new EntityDefinition
                {
                    Name = "Task",
                    PluralName = "Tasks",
                    PrimaryKeyName = "TaskId",
                    PrimaryKeyType = "Guid"
                }
            },
            new SavedEntityTemplate
            {
                Id = Guid.Parse("00000001-0000-0000-0000-000000000003"),
                Name = "Audit Log",
                Description = "Read-only event log with user, action, and timestamp",
                Icon = "fa-clock-rotate-left",
                IsBuiltIn = true,
                Entity = new EntityDefinition
                {
                    Name = "AuditEntry",
                    PluralName = "AuditEntries",
                    PrimaryKeyName = "AuditEntryId",
                    PrimaryKeyType = "Guid"
                },
                Options = new EntityWizardOptions { IsReadOnly = true }
            }
        };
    }

    // ============================================================
    // TEMPLATE GALLERY - Unified Template Browsing
    // ============================================================

    /// <summary>
    /// Category of template for the gallery.
    /// </summary>
    public enum GalleryTemplateCategory
    {
        Entity,
        PagePattern,
        PropertySnippet,
        Wizard
    }

    /// <summary>
    /// Unified template info for the Template Gallery page.
    /// </summary>
    public class GalleryTemplate
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public GalleryTemplateCategory Category { get; set; }
        public string CategoryDisplay => Category switch {
            GalleryTemplateCategory.Entity => "Entity Template",
            GalleryTemplateCategory.PagePattern => "Page Pattern",
            GalleryTemplateCategory.PropertySnippet => "Property Snippet",
            GalleryTemplateCategory.Wizard => "Wizard Template",
            _ => "Template"
        };

        /// <summary>What this template generates</summary>
        public List<string> Outputs { get; set; } = new();

        /// <summary>Required inputs</summary>
        public List<GalleryTemplateInput> Inputs { get; set; } = new();

        /// <summary>When to use this template</summary>
        public string UseCase { get; set; } = string.Empty;

        /// <summary>Complexity: Low, Medium, High</summary>
        public string Complexity { get; set; } = "Medium";

        /// <summary>Is this template implemented?</summary>
        public bool IsImplemented { get; set; } = true;

        /// <summary>Preview code snippet</summary>
        public string PreviewCode { get; set; } = string.Empty;

        /// <summary>Preview language</summary>
        public string PreviewLanguage { get; set; } = "csharp";
    }

    /// <summary>
    /// An input field required by a template.
    /// </summary>
    public class GalleryTemplateInput
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = "string";
        public string Description { get; set; } = string.Empty;
        public bool IsRequired { get; set; } = true;
        public string? Example { get; set; }
    }

    /// <summary>
    /// Static catalog of all templates for the gallery.
    /// </summary>
    public static class GalleryTemplates
    {
        /// <summary>All available templates.</summary>
        public static List<GalleryTemplate> All => GetAllTemplates();

        /// <summary>Get templates by category.</summary>
        public static List<GalleryTemplate> GetByCategory(GalleryTemplateCategory category)
            => All.Where(t => t.Category == category).ToList();

        /// <summary>Get a specific template by ID.</summary>
        public static GalleryTemplate? GetById(string id)
            => All.FirstOrDefault(t => t.Id == id);

        private static List<GalleryTemplate> GetAllTemplates()
        {
            var templates = new List<GalleryTemplate>();
            templates.AddRange(GetEntityTemplates());
            templates.AddRange(GetPagePatternTemplates());
            templates.AddRange(GetPropertySnippetTemplates());
            return templates;
        }

        private static List<GalleryTemplate> GetEntityTemplates()
        {
            return new List<GalleryTemplate>
            {
                new GalleryTemplate
                {
                    Id = "entity-efmodel",
                    Name = "EF Model",
                    Description = "Entity Framework model class with data annotations",
                    Icon = "fa-database",
                    Category = GalleryTemplateCategory.Entity,
                    Complexity = "Low",
                    UseCase = "Generate a C# class for database mapping",
                    Outputs = new() { "{EntityName}Item.cs" },
                    IsImplemented = true
                },
                new GalleryTemplate
                {
                    Id = "entity-dataobjects",
                    Name = "DataObjects (DTOs)",
                    Description = "Data transfer objects and filter classes",
                    Icon = "fa-box",
                    Category = GalleryTemplateCategory.Entity,
                    Complexity = "Low",
                    UseCase = "Generate DTOs for API communication",
                    Outputs = new() { "DataObjects.App.{ProjectName}.cs" },
                    IsImplemented = true
                },
                new GalleryTemplate
                {
                    Id = "entity-dataaccess",
                    Name = "DataAccess (CRUD)",
                    Description = "Data access layer with Get, Save, Delete methods",
                    Icon = "fa-server",
                    Category = GalleryTemplateCategory.Entity,
                    Complexity = "Medium",
                    UseCase = "Generate repository methods for database operations",
                    Outputs = new() { "DataAccess.App.{ProjectName}.cs" },
                    IsImplemented = true
                },
                new GalleryTemplate
                {
                    Id = "entity-controller",
                    Name = "API Controller",
                    Description = "REST API endpoints with authentication",
                    Icon = "fa-plug",
                    Category = GalleryTemplateCategory.Entity,
                    Complexity = "Low",
                    UseCase = "Generate web API endpoints",
                    Outputs = new() { "DataController.App.{ProjectName}.cs" },
                    IsImplemented = true
                },
                new GalleryTemplate
                {
                    Id = "entity-listpage",
                    Name = "List Page (Razor)",
                    Description = "Paginated list page with search and filters",
                    Icon = "fa-table-list",
                    Category = GalleryTemplateCategory.Entity,
                    Complexity = "Medium",
                    UseCase = "Generate a Blazor page for entity records",
                    Outputs = new() { "{PluralName}Page.App.razor" },
                    IsImplemented = true
                },
                new GalleryTemplate
                {
                    Id = "entity-editpage",
                    Name = "Edit Modal (Razor)",
                    Description = "Create/edit form with validation",
                    Icon = "fa-pen-to-square",
                    Category = GalleryTemplateCategory.Entity,
                    Complexity = "Medium",
                    UseCase = "Generate a modal form for editing records",
                    Outputs = new() { "Edit{EntityName}.App.razor" },
                    IsImplemented = true
                }
            };
        }

        private static List<GalleryTemplate> GetPagePatternTemplates()
        {
            return FMPagePatterns.All.Select(p => new GalleryTemplate
            {
                Id = $"page-{p.Pattern.ToString().ToLower()}",
                Name = p.Name,
                Description = p.Description,
                Icon = p.Icon,
                Category = GalleryTemplateCategory.PagePattern,
                Complexity = p.Complexity,
                UseCase = p.UseCase,
                Outputs = p.GeneratedFiles,
                IsImplemented = p.IsImplemented
            }).ToList();
        }

        private static List<GalleryTemplate> GetPropertySnippetTemplates()
        {
            return PropertySnippets.All.Select(s => new GalleryTemplate
            {
                Id = $"snippet-{s.Id}",
                Name = s.Name,
                Description = s.Description,
                Icon = s.Icon,
                Category = GalleryTemplateCategory.PropertySnippet,
                Complexity = "Low",
                UseCase = $"Quick-add: {string.Join(", ", s.Properties.Select(p => p.Name))}",
                Outputs = s.Properties.Select(p => $"{p.Name} ({p.Type})").ToList(),
                IsImplemented = true
            }).ToList();
        }
    }
}

#endregion
