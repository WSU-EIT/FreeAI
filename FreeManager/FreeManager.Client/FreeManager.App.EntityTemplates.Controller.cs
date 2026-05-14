using System.Text;

namespace FreeManager;

#region EntityTemplates - Controller Generation
// ============================================================================
// ENTITY WIZARD - CONTROLLER TEMPLATES
// Generates REST API endpoints for CRUD operations.
// Part of: EntityTemplates.App (partial)
// ============================================================================

public static partial class EntityTemplates
{
    // ============================================================
    // MULTI-ENTITY CONTROLLER TEMPLATE
    // ============================================================

    private static string GenerateControllerMulti(
        List<DataObjects.EntityDefinition> entities,
        List<DataObjects.RelationshipDefinition> relationships,
        DataObjects.EntityWizardOptions options,
        string projectName)
    {
        var sb = new StringBuilder();

        sb.AppendLine("using Microsoft.AspNetCore.Mvc;");
        sb.AppendLine("using Microsoft.EntityFrameworkCore;");
        sb.AppendLine();
        sb.AppendLine($"namespace {projectName}.Server.Controllers;");
        sb.AppendLine();
        sb.AppendLine($"// ============================================================================");
        sb.AppendLine($"// {projectName.ToUpper()} PROJECT API ENDPOINTS");
        sb.AppendLine($"// ============================================================================");
        sb.AppendLine();
        sb.AppendLine($"public partial class DataController");
        sb.AppendLine($"{{");

        foreach (var entity in entities)
        {
            sb.AppendLine(GenerateController(entity, relationships, entities, options, projectName));
            sb.AppendLine();
        }

        sb.AppendLine($"}}");

        return sb.ToString();
    }

    // ============================================================
    // SINGLE-ENTITY CONTROLLER TEMPLATE (with Lookup endpoints)
    // ============================================================

    private static string GenerateController(
        DataObjects.EntityDefinition entity,
        List<DataObjects.RelationshipDefinition> relationships,
        List<DataObjects.EntityDefinition> allEntities,
        DataObjects.EntityWizardOptions options,
        string projectName)
    {
        var sb = new StringBuilder();
        var pkProp = entity.Properties.FirstOrDefault(p => p.IsPrimaryKey);
        var pkType = pkProp?.Type ?? "Guid";
        var pkName = pkProp?.Name ?? $"{entity.Name}Id";

        sb.AppendLine($"    // {entity.Name} API Endpoints");
        sb.AppendLine($"    #region {entity.Name}");
        sb.AppendLine();

        // Get filtered list
        sb.AppendLine($"    [HttpPost(\"api/Data/Get{entity.PluralName}\")]");
        sb.AppendLine($"    public async Task<ActionResult<DataObjects.{entity.Name}FilterResult>> Get{entity.PluralName}([FromBody] DataObjects.{entity.Name}Filter filter)");
        sb.AppendLine($"    {{");
        sb.AppendLine($"        return Ok(await da.Get{entity.PluralName}Async(filter));");
        sb.AppendLine($"    }}");
        sb.AppendLine();

        // Get by ID
        sb.AppendLine($"    [HttpPost(\"api/Data/Get{entity.Name}\")]");
        sb.AppendLine($"    public async Task<ActionResult<DataObjects.{entity.Name}?>> Get{entity.Name}([FromBody] {pkType} id)");
        sb.AppendLine($"    {{");
        sb.AppendLine($"        var item = await da.Get{entity.Name}Async(id);");
        sb.AppendLine($"        if (item == null) return NotFound();");
        sb.AppendLine($"        return Ok(item);");
        sb.AppendLine($"    }}");
        sb.AppendLine();

        // Get Lookups endpoint (for FK dropdowns)
        if (!entity.IsJoinTable)
        {
            sb.AppendLine($"    [HttpGet(\"api/Data/Get{entity.Name}Lookups\")]");
            sb.AppendLine($"    public async Task<ActionResult<List<DataObjects.{entity.Name}Lookup>>> Get{entity.Name}Lookups()");
            sb.AppendLine($"    {{");
            sb.AppendLine($"        return Ok(await da.Get{entity.Name}LookupsAsync());");
            sb.AppendLine($"    }}");
        }

        if (!options.IsReadOnly)
        {
            sb.AppendLine();
            // Save
            sb.AppendLine($"    [HttpPost(\"api/Data/Save{entity.Name}\")]");
            sb.AppendLine($"    public async Task<ActionResult<DataObjects.{entity.Name}?>> Save{entity.Name}([FromBody] DataObjects.{entity.Name} item)");
            sb.AppendLine($"    {{");
            sb.AppendLine($"        var result = await da.Save{entity.Name}Async(item);");
            sb.AppendLine($"        if (result == null) return BadRequest();");
            sb.AppendLine($"        return Ok(result);");
            sb.AppendLine($"    }}");
            sb.AppendLine();

            // Delete
            sb.AppendLine($"    [HttpPost(\"api/Data/Delete{entity.Name}\")]");
            sb.AppendLine($"    public async Task<ActionResult<bool>> Delete{entity.Name}([FromBody] {pkType} id)");
            sb.AppendLine($"    {{");
            sb.AppendLine($"        return Ok(await da.Delete{entity.Name}Async(id));");
            sb.AppendLine($"    }}");
        }

        sb.AppendLine();
        sb.AppendLine($"    #endregion");

        return sb.ToString();
    }

    // Backward compatibility overload
    private static string GenerateController(DataObjects.EntityDefinition entity, DataObjects.EntityWizardOptions options, string projectName)
    {
        return GenerateController(entity, new List<DataObjects.RelationshipDefinition>(), new List<DataObjects.EntityDefinition>(), options, projectName);
    }
}

#endregion
