using System.Text;

namespace FreeManager;

#region EntityTemplates - DataAccess Generation
// ============================================================================
// ENTITY WIZARD - DATAACCESS TEMPLATES
// Generates data access layer with CRUD operations.
// Part of: EntityTemplates.App (partial)
// ============================================================================

public static partial class EntityTemplates
{
    // ============================================================
    // MULTI-ENTITY DATAACCESS TEMPLATE
    // ============================================================

    private static string GenerateDataAccessMulti(
        List<DataObjects.EntityDefinition> entities,
        List<DataObjects.RelationshipDefinition> relationships,
        DataObjects.EntityWizardOptions options,
        string projectName)
    {
        var sb = new StringBuilder();

        sb.AppendLine("using Microsoft.EntityFrameworkCore;");
        sb.AppendLine();
        sb.AppendLine($"namespace {projectName};");
        sb.AppendLine();
        sb.AppendLine($"// ============================================================================");
        sb.AppendLine($"// {projectName.ToUpper()} PROJECT DATA ACCESS");
        sb.AppendLine($"// ============================================================================");
        sb.AppendLine();

        foreach (var entity in entities)
        {
            sb.AppendLine(GenerateDataAccess(entity, relationships, entities, options, projectName));
            sb.AppendLine();
        }

        return sb.ToString();
    }

    // ============================================================
    // SINGLE-ENTITY DATAACCESS TEMPLATE (Relationship-Aware)
    // ============================================================

    private static string GenerateDataAccess(
        DataObjects.EntityDefinition entity,
        List<DataObjects.RelationshipDefinition> relationships,
        List<DataObjects.EntityDefinition> allEntities,
        DataObjects.EntityWizardOptions options,
        string projectName)
    {
        var sb = new StringBuilder();
        var pkProp = entity.Properties.FirstOrDefault(p => p.IsPrimaryKey);
        var pkName = pkProp?.Name ?? $"{entity.Name}Id";
        var pkType = pkProp?.Type ?? "Guid";

        var parentRels = GetParentRelationships(entity.Name, relationships);

        sb.AppendLine($"// {entity.Name} Data Access Methods");
        sb.AppendLine($"public partial class DataAccess");
        sb.AppendLine($"{{");
        sb.AppendLine($"    #region {entity.Name}");
        sb.AppendLine();

        // Get filtered list
        sb.AppendLine($"    public async Task<DataObjects.{entity.Name}FilterResult> Get{entity.PluralName}Async(DataObjects.{entity.Name}Filter filter)");
        sb.AppendLine($"    {{");
        sb.AppendLine($"        var query = data.{entity.PluralName}");

        foreach (var rel in parentRels)
        {
            // Use default nav property name if not specified
            var navPropName = !string.IsNullOrWhiteSpace(rel.TargetNavigationProperty)
                ? rel.TargetNavigationProperty
                : rel.SourceEntityName;
            sb.AppendLine($"            .Include(x => x.{navPropName})");
        }
        sb.AppendLine($"            .AsQueryable();");
        sb.AppendLine();

        if (options.IncludeSoftDelete)
        {
            sb.AppendLine($"        if (!filter.IncludeDeleted)");
            sb.AppendLine($"            query = query.Where(x => !x.Deleted);");
            sb.AppendLine();
        }

        foreach (var rel in parentRels)
        {
            var fkPropName = !string.IsNullOrWhiteSpace(rel.ForeignKeyProperty)
                ? rel.ForeignKeyProperty
                : rel.SourceEntityName + "Id";
            var fkNullable = !rel.IsRequired;
            if (fkNullable)
            {
                sb.AppendLine($"        if (filter.{fkPropName}Filter.HasValue)");
                sb.AppendLine($"            query = query.Where(x => x.{fkPropName} == filter.{fkPropName}Filter.Value);");
            }
            else
            {
                sb.AppendLine($"        if (filter.{fkPropName}Filter != default)");
                sb.AppendLine($"            query = query.Where(x => x.{fkPropName} == filter.{fkPropName}Filter);");
            }
            sb.AppendLine();
        }

        sb.AppendLine($"        if (!string.IsNullOrWhiteSpace(filter.Search))");
        sb.AppendLine($"        {{");
        var searchableProps = entity.Properties.Where(p => p.Type == "string" && !p.IsSystemField).Take(3).ToList();
        if (searchableProps.Any())
        {
            var searchConditions = string.Join(" || ", searchableProps.Select(p => $"x.{p.Name}.Contains(filter.Search)"));
            sb.AppendLine($"            query = query.Where(x => {searchConditions});");
        }
        sb.AppendLine($"        }}");
        sb.AppendLine();
        sb.AppendLine($"        var total = await query.CountAsync();");
        sb.AppendLine();
        sb.AppendLine($"        query = filter.SortColumn switch");
        sb.AppendLine($"        {{");
        foreach (var prop in entity.Properties.Where(p => !p.IsSystemField).Take(5))
        {
            sb.AppendLine($"            \"{prop.Name}\" => filter.SortDescending ? query.OrderByDescending(x => x.{prop.Name}) : query.OrderBy(x => x.{prop.Name}),");
        }
        sb.AppendLine($"            _ => query.OrderByDescending(x => x.{pkName})");
        sb.AppendLine($"        }};");
        sb.AppendLine();
        sb.AppendLine($"        var items = await query.Skip(filter.Skip).Take(filter.PageSize).ToListAsync();");
        sb.AppendLine();
        sb.AppendLine($"        return new DataObjects.{entity.Name}FilterResult");
        sb.AppendLine($"        {{");
        sb.AppendLine($"            Records = items.Select(x => new DataObjects.{entity.Name}");
        sb.AppendLine($"            {{");
        foreach (var prop in entity.Properties.Where(p => !p.IsSystemField || p.IsPrimaryKey))
        {
            sb.AppendLine($"                {prop.Name} = x.{prop.Name},");
        }
        foreach (var rel in parentRels)
        {
            var parentEntity = allEntities.FirstOrDefault(e => e.Name == rel.SourceEntityName);
            var displayProp = parentEntity?.Properties
                .FirstOrDefault(p => p.Name.Contains("Name") || p.Name.Contains("Title") || p.Name.Contains("Description"))
                ?? parentEntity?.Properties.FirstOrDefault(p => p.Type == "string" && !p.IsPrimaryKey);

            var navPropName = !string.IsNullOrWhiteSpace(rel.TargetNavigationProperty)
                ? rel.TargetNavigationProperty
                : rel.SourceEntityName;

            if (displayProp != null)
            {
                sb.AppendLine($"                {rel.SourceEntityName}Name = x.{navPropName}?.{displayProp.Name} ?? string.Empty,");
            }
            else
            {
                sb.AppendLine($"                {rel.SourceEntityName}Name = x.{navPropName}?.ToString() ?? string.Empty,");
            }
        }
        sb.AppendLine($"            }}).ToList(),");
        sb.AppendLine($"            TotalRecords = total,");
        sb.AppendLine($"            Page = filter.Page,");
        sb.AppendLine($"            PageSize = filter.PageSize");
        sb.AppendLine($"        }};");
        sb.AppendLine($"    }}");
        sb.AppendLine();

        // Get by ID
        sb.AppendLine($"    public async Task<DataObjects.{entity.Name}?> Get{entity.Name}Async({pkType} id)");
        sb.AppendLine($"    {{");
        sb.AppendLine($"        var item = await data.{entity.PluralName}");
        foreach (var rel in parentRels)
        {
            var navPropName = !string.IsNullOrWhiteSpace(rel.TargetNavigationProperty)
                ? rel.TargetNavigationProperty
                : rel.SourceEntityName;
            sb.AppendLine($"            .Include(x => x.{navPropName})");
        }
        sb.AppendLine($"            .FirstOrDefaultAsync(x => x.{pkName} == id);");
        sb.AppendLine($"        if (item == null) return null;");
        sb.AppendLine();
        sb.AppendLine($"        return new DataObjects.{entity.Name}");
        sb.AppendLine($"        {{");
        foreach (var prop in entity.Properties.Where(p => !p.IsSystemField || p.IsPrimaryKey))
        {
            sb.AppendLine($"            {prop.Name} = item.{prop.Name},");
        }
        foreach (var rel in parentRels)
        {
            var parentEntity = allEntities.FirstOrDefault(e => e.Name == rel.SourceEntityName);
            var displayProp = parentEntity?.Properties
                .FirstOrDefault(p => p.Name.Contains("Name") || p.Name.Contains("Title") || p.Name.Contains("Description"))
                ?? parentEntity?.Properties.FirstOrDefault(p => p.Type == "string" && !p.IsPrimaryKey);

            var navPropName = !string.IsNullOrWhiteSpace(rel.TargetNavigationProperty)
                ? rel.TargetNavigationProperty
                : rel.SourceEntityName;

            if (displayProp != null)
            {
                sb.AppendLine($"            {rel.SourceEntityName}Name = item.{navPropName}?.{displayProp.Name} ?? string.Empty,");
            }
        }
        sb.AppendLine($"        }};");
        sb.AppendLine($"    }}");

        if (!options.IsReadOnly)
        {
            sb.AppendLine();
            // Save
            sb.AppendLine($"    public async Task<DataObjects.{entity.Name}?> Save{entity.Name}Async(DataObjects.{entity.Name} dto)");
            sb.AppendLine($"    {{");
            sb.AppendLine($"        EFModels.EFModels.{entity.Name}Item item;");
            sb.AppendLine($"        var isNew = dto.{pkName} == default;");
            sb.AppendLine();
            sb.AppendLine($"        if (isNew)");
            sb.AppendLine($"        {{");
            sb.AppendLine($"            item = new EFModels.EFModels.{entity.Name}Item();");
            if (pkType == "Guid")
            {
                sb.AppendLine($"                item.{pkName} = Guid.NewGuid();");
            }
            // Set Added if it exists in the entity (FreeManager convention)
            if (entity.Properties.Any(p => p.Name == "Added"))
            {
                sb.AppendLine($"                item.Added = DateTime.UtcNow;");
            }
            sb.AppendLine($"            data.{entity.PluralName}.Add(item);");
            sb.AppendLine($"        }}");
            sb.AppendLine($"        else");
            sb.AppendLine($"        {{");
            sb.AppendLine($"            item = await data.{entity.PluralName}.FindAsync(dto.{pkName});");
            sb.AppendLine($"            if (item == null) return null;");
            sb.AppendLine($"        }}");
            sb.AppendLine();
            foreach (var prop in entity.Properties.Where(p => !p.IsPrimaryKey && !p.IsSystemField))
            {
                sb.AppendLine($"        item.{prop.Name} = dto.{prop.Name};");
            }
            // Set LastModified if it exists in the entity (FreeManager convention)
            if (entity.Properties.Any(p => p.Name == "LastModified"))
            {
                sb.AppendLine($"        item.LastModified = DateTime.UtcNow;");
            }
            sb.AppendLine();
            sb.AppendLine($"        await data.SaveChangesAsync();");
            sb.AppendLine($"        dto.{pkName} = item.{pkName};");
            sb.AppendLine($"        return dto;");
            sb.AppendLine($"    }}");
            sb.AppendLine();

            // Delete
            if (options.IncludeSoftDelete)
            {
                sb.AppendLine($"    public async Task<bool> Delete{entity.Name}Async({pkType} id)");
                sb.AppendLine($"    {{");
                sb.AppendLine($"        var item = await data.{entity.PluralName}.FindAsync(id);");
                sb.AppendLine($"        if (item == null) return false;");
                sb.AppendLine($"        item.Deleted = true;");
                sb.AppendLine($"        item.DeletedAt = DateTime.UtcNow;");
                sb.AppendLine($"        await data.SaveChangesAsync();");
                sb.AppendLine($"        return true;");
                sb.AppendLine($"    }}");
            }
            else
            {
                sb.AppendLine($"    public async Task<bool> Delete{entity.Name}Async({pkType} id)");
                sb.AppendLine($"    {{");
                sb.AppendLine($"        var item = await data.{entity.PluralName}.FindAsync(id);");
                sb.AppendLine($"        if (item == null) return false;");
                sb.AppendLine($"        data.{entity.PluralName}.Remove(item);");
                sb.AppendLine($"        await data.SaveChangesAsync();");
                sb.AppendLine($"        return true;");
                sb.AppendLine($"    }}");
            }
        }

        // Get Lookups (for FK dropdowns)
        if (!entity.IsJoinTable)
        {
            var displayProp = entity.Properties
                .FirstOrDefault(p => p.Name.Contains("Name") || p.Name.Contains("Title") || p.Name.Contains("Description"))
                ?? entity.Properties.FirstOrDefault(p => p.Type == "string" && !p.IsPrimaryKey);

            sb.AppendLine();
            sb.AppendLine($"    public async Task<List<DataObjects.{entity.Name}Lookup>> Get{entity.Name}LookupsAsync()");
            sb.AppendLine($"    {{");
            sb.AppendLine($"        return await data.{entity.PluralName}");
            if (options.IncludeSoftDelete)
            {
                sb.AppendLine($"            .Where(x => !x.Deleted)");
            }
            sb.AppendLine($"            .Select(x => new DataObjects.{entity.Name}Lookup");
            sb.AppendLine($"            {{");
            sb.AppendLine($"                {pkName} = x.{pkName},");
            if (displayProp != null)
            {
                sb.AppendLine($"                DisplayName = x.{displayProp.Name}");
            }
            else
            {
                sb.AppendLine($"                DisplayName = x.{pkName}.ToString();");
            }
            sb.AppendLine($"            }})");
            sb.AppendLine($"            .ToListAsync();");
            sb.AppendLine($"    }}");
        }

        sb.AppendLine();
        sb.AppendLine($"    #endregion");
        sb.AppendLine($"}}");

        return sb.ToString();
    }

    // Backward compatibility overload
    private static string GenerateDataAccess(DataObjects.EntityDefinition entity, DataObjects.EntityWizardOptions options, string projectName)
    {
        return GenerateDataAccess(entity, new List<DataObjects.RelationshipDefinition>(), new List<DataObjects.EntityDefinition>(), options, projectName);
    }

    // ============================================================
    // IDATAACCESS INTERFACE PARTIAL GENERATION
    // ============================================================

    /// <summary>
    /// Generates a partial interface for IDataAccess with method signatures for all entities.
    /// </summary>
    private static string GenerateIDataAccessPartial(
        List<DataObjects.EntityDefinition> entities,
        DataObjects.EntityWizardOptions options,
        string projectName)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"namespace {projectName};");
        sb.AppendLine();
        sb.AppendLine($"// ============================================================================");
        sb.AppendLine($"// {projectName.ToUpper()} PROJECT - IDataAccess Extension");
        sb.AppendLine($"// Interface method signatures for generated entities");
        sb.AppendLine($"// Generated by Entity Builder Wizard");
        sb.AppendLine($"// ============================================================================");
        sb.AppendLine();
        sb.AppendLine($"public partial interface IDataAccess");
        sb.AppendLine($"{{");

        foreach (var entity in entities.Where(e => !e.IsJoinTable))
        {
            var pkProp = entity.Properties.FirstOrDefault(p => p.IsPrimaryKey);
            var pkType = pkProp?.Type ?? "Guid";

            sb.AppendLine($"    // {entity.Name} methods");

            // Get filtered list
            sb.AppendLine($"    Task<DataObjects.{entity.Name}FilterResult> Get{entity.PluralName}Async(DataObjects.{entity.Name}Filter filter);");

            // Get by ID
            sb.AppendLine($"    Task<DataObjects.{entity.Name}?> Get{entity.Name}Async({pkType} id);");

            // Get Lookups (for FK dropdowns)
            sb.AppendLine($"    Task<List<DataObjects.{entity.Name}Lookup>> Get{entity.Name}LookupsAsync();");

            if (!options.IsReadOnly)
            {
                // Save
                sb.AppendLine($"    Task<DataObjects.{entity.Name}?> Save{entity.Name}Async(DataObjects.{entity.Name} dto);");

                // Delete
                sb.AppendLine($"    Task<bool> Delete{entity.Name}Async({pkType} id);");
            }

            sb.AppendLine();
        }

        sb.AppendLine($"}}");

        return sb.ToString();
    }
}

#endregion
