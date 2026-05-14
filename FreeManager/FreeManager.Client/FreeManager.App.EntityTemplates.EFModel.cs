using System.Text;

namespace FreeManager;

#region EntityTemplates - EF Model Generation
// ============================================================================
// ENTITY WIZARD - EF MODEL TEMPLATES
// Generates Entity Framework model classes with relationships.
// Part of: EntityTemplates.App (partial)
// ============================================================================

public static partial class EntityTemplates
{
    // ============================================================
    // EF MODEL TEMPLATE (with relationship support)
    // ============================================================

    private static string GenerateEFModel(
        DataObjects.EntityDefinition entity,
        List<DataObjects.RelationshipDefinition> relationships,
        DataObjects.EntityWizardOptions options,
        string projectName)
    {
        var sb = new StringBuilder();
        var tableName = string.IsNullOrWhiteSpace(entity.TableName) ? entity.PluralName : entity.TableName;

        sb.AppendLine("using System.ComponentModel.DataAnnotations;");
        sb.AppendLine("using System.ComponentModel.DataAnnotations.Schema;");
        sb.AppendLine();
        sb.AppendLine($"namespace {projectName}.EFModels.EFModels;");
        sb.AppendLine();
        sb.AppendLine($"/// <summary>");
        sb.AppendLine($"/// {entity.Name} entity - stored in [{tableName}] table.");
        if (entity.IsJoinTable)
        {
            sb.AppendLine($"/// This is a join table for many-to-many relationships.");
        }
        sb.AppendLine($"/// </summary>");
        sb.AppendLine($"[Table(\"{tableName}\")]");
        sb.AppendLine($"public partial class {entity.Name}Item");
        sb.AppendLine("{");

        // Properties
        foreach (var prop in entity.Properties.OrderBy(p => p.SortOrder).ThenBy(p => p.Name))
        {
            if (prop.IsPrimaryKey)
            {
                sb.AppendLine("    [Key]");
            }
            if (prop.MaxLength.HasValue)
            {
                sb.AppendLine($"    [MaxLength({prop.MaxLength})]");
            }

            var csharpType = GetEFType(prop, entity.Enums);
            var defaultValue = GetEFDefaultValue(prop, entity.Enums);

            sb.Append($"    public {csharpType} {prop.Name} {{ get; set; }}");
            if (!string.IsNullOrWhiteSpace(defaultValue))
            {
                sb.Append($" = {defaultValue};");
            }
            sb.AppendLine();
            sb.AppendLine();
        }

        // Navigation properties from relationships
        var sourceRels = relationships.Where(r => r.SourceEntityName == entity.Name).ToList();
        var targetRels = relationships.Where(r => r.TargetEntityName == entity.Name && r.SourceEntityName != entity.Name).ToList();
        var selfRefs = relationships.Where(r => r.Type == DataObjects.RelationshipType.SelfReference && r.SourceEntityName == entity.Name).ToList();

        if (sourceRels.Any() || targetRels.Any() || selfRefs.Any())
        {
            sb.AppendLine("    // Navigation properties");
        }

        // Source side navigation (collections for 1:N)
        foreach (var rel in sourceRels)
        {
            // Default navigation property name if not specified
            var navPropName = !string.IsNullOrWhiteSpace(rel.SourceNavigationProperty)
                ? rel.SourceNavigationProperty
                : rel.TargetEntityName + "s"; // e.g., "AccessEvents"

            if (rel.Type == DataObjects.RelationshipType.OneToMany || rel.Type == DataObjects.RelationshipType.ManyToMany)
            {
                sb.AppendLine($"    public virtual ICollection<{rel.TargetEntityName}Item> {navPropName} {{ get; set; }} = new List<{rel.TargetEntityName}Item>();");
            }
            else if (rel.Type == DataObjects.RelationshipType.OneToOne)
            {
                sb.AppendLine($"    public virtual {rel.TargetEntityName}Item? {navPropName} {{ get; set; }}");
            }
        }

        // Target side navigation (references for 1:N)
        foreach (var rel in targetRels)
        {
            var fkNullable = !rel.IsRequired;
            var fkType = "Guid" + (fkNullable ? "?" : "");

            // Default FK property name if not specified
            var fkPropName = !string.IsNullOrWhiteSpace(rel.ForeignKeyProperty)
                ? rel.ForeignKeyProperty
                : rel.SourceEntityName + "Id"; // e.g., "SourceSystemId"

            // Default navigation property name if not specified
            var navPropName = !string.IsNullOrWhiteSpace(rel.TargetNavigationProperty)
                ? rel.TargetNavigationProperty
                : rel.SourceEntityName; // e.g., "SourceSystem"

            // Only add FK property if not already defined in entity properties
            var fkAlreadyDefined = entity.Properties.Any(p =>
                p.Name.Equals(fkPropName, StringComparison.OrdinalIgnoreCase));

            if (!fkAlreadyDefined)
            {
                sb.AppendLine($"    public {fkType} {fkPropName} {{ get; set; }}");
            }

            var navNullable = fkNullable ? "?" : "";
            var initValue = fkNullable ? "" : " = null!";
            sb.AppendLine($"    [ForeignKey(\"{fkPropName}\")]");
            sb.AppendLine($"    public virtual {rel.SourceEntityName}Item{navNullable} {navPropName} {{ get; set; }}{initValue};");
            sb.AppendLine();
        }

        // Self-referencing navigation
        foreach (var rel in selfRefs)
        {
            var fkPropName = !string.IsNullOrWhiteSpace(rel.ForeignKeyProperty)
                ? rel.ForeignKeyProperty
                : "Parent" + entity.Name + "Id";
            var parentNavName = !string.IsNullOrWhiteSpace(rel.TargetNavigationProperty)
                ? rel.TargetNavigationProperty
                : "Parent";
            var childrenNavName = !string.IsNullOrWhiteSpace(rel.SourceNavigationProperty)
                ? rel.SourceNavigationProperty
                : "Children";

            sb.AppendLine($"    public Guid? {fkPropName} {{ get; set; }}");
            sb.AppendLine($"    [ForeignKey(\"{fkPropName}\")]");
            sb.AppendLine($"    public virtual {entity.Name}Item? {parentNavName} {{ get; set; }}");
            sb.AppendLine($"    public virtual ICollection<{entity.Name}Item> {childrenNavName} {{ get; set; }} = new List<{entity.Name}Item>();");
        }

        sb.AppendLine("}");

        return sb.ToString();
    }

    // Keep single-entity overload for backward compatibility
    private static string GenerateEFModel(DataObjects.EntityDefinition entity, DataObjects.EntityWizardOptions options)
    {
        return GenerateEFModel(entity, new List<DataObjects.RelationshipDefinition>(), options, "FreeManager");
    }

    // ============================================================
    // EF TYPE HELPERS
    // ============================================================

    private static string GetEFType(DataObjects.PropertyDefinition prop, List<DataObjects.EnumDefinition> enums)
    {
        if (prop.Type.StartsWith("enum:"))
        {
            return prop.IsNullable ? "string?" : "string";
        }

        var baseType = prop.Type;
        if (prop.IsNullable && !baseType.EndsWith("?"))
        {
            baseType += "?";
        }
        return baseType;
    }

    private static string GetEFDefaultValue(DataObjects.PropertyDefinition prop, List<DataObjects.EnumDefinition> enums)
    {
        if (!string.IsNullOrWhiteSpace(prop.DefaultValue))
        {
            if (prop.Type.StartsWith("enum:"))
            {
                var enumDef = enums.FirstOrDefault(e => e.Name == prop.EnumName);
                var defaultVal = prop.DefaultValue ?? enumDef?.DefaultValue ?? enumDef?.Values.FirstOrDefault() ?? "";
                return $"\"{defaultVal}\"";
            }
            return prop.DefaultValue;
        }

        if (prop.Type.StartsWith("enum:"))
        {
            var enumDef = enums.FirstOrDefault(e => e.Name == prop.EnumName);
            var defaultVal = enumDef?.DefaultValue ?? enumDef?.Values.FirstOrDefault() ?? "";
            return $"\"{defaultVal}\"";
        }

        return prop.Type switch
        {
            "string" => "string.Empty",
            "bool" => "false",
            "Guid" when prop.IsPrimaryKey => null!,
            "Guid" => "Guid.Empty",
            _ => null!
        };
    }

    // ============================================================
    // EFDATAMODEL PARTIAL GENERATION
    // ============================================================

    /// <summary>
    /// Generates a partial class for EFDataModel with DbSet properties for all entities.
    /// </summary>
    private static string GenerateEFDataModelPartial(
        List<DataObjects.EntityDefinition> entities,
        string projectName)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"using Microsoft.EntityFrameworkCore;");
        sb.AppendLine();
        sb.AppendLine($"namespace {projectName}.EFModels.EFModels;");
        sb.AppendLine();
        sb.AppendLine($"// ============================================================================");
        sb.AppendLine($"// {projectName.ToUpper()} PROJECT - EFDataModel Extension");
        sb.AppendLine($"// DbSet properties for generated entities");
        sb.AppendLine($"// Generated by Entity Builder Wizard");
        sb.AppendLine($"// ============================================================================");
        sb.AppendLine();
        sb.AppendLine($"public partial class EFDataModel : DbContext");
        sb.AppendLine($"{{");

        foreach (var entity in entities)
        {
            sb.AppendLine($"    public virtual DbSet<{entity.Name}Item> {entity.PluralName} {{ get; set; }} = null!;");
        }

        sb.AppendLine($"}}");

        return sb.ToString();
    }
}

#endregion
