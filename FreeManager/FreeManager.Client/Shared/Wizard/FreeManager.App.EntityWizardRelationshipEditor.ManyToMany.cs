// ============================================================================
// SCAFFOLD: Awaiting migration from CRM.Client (split)
// Source: CRM.Client/Shared/Wizard/EntityWizard → FreeManager.App.EntityWizardRelationshipEditor.App.razor
// Team: Team 4 (Client) - see docs/028_plan_team4-client.md
// Split: Many-to-Many (M:N) relationship configuration
// ============================================================================

namespace FreeManager.Client.Shared.Wizard;

/// <summary>
/// EntityWizardRelationshipEditor Many-to-Many partial - handles M:N relationship setup.
/// </summary>
public partial class EntityWizardRelationshipEditor
{
    // ============================================================
    // MANY-TO-MANY CONFIGURATION METHODS
    // ============================================================

    // TODO: Migration pending - GetSuggestedLinkTableName() -> string
    //   Source: CRM.Client/Shared/Wizard/EntityWizard → FreeManager.App.EntityWizardRelationshipEditor.App.razor ~line 501
    //   Purpose: Suggest join/link table name
    //   Pattern: {SourceEntity}{TargetEntity}Link
    //   Example: "StudentCourseLink" for Student <-> Course

    // TODO: Migration pending - AutoFillLinkTableName()
    //   Source: CRM.Client/Shared/Wizard/EntityWizard → FreeManager.App.EntityWizardRelationshipEditor.App.razor ~line 508
    //   Purpose: Auto-populate link table name field
    //   Called: When M:M type selected or entities changed

    // TODO: Migration pending - IsValidCSharpIdentifier(string name) -> bool
    //   Source: CRM.Client/Shared/Wizard/EntityWizard → FreeManager.App.EntityWizardRelationshipEditor.App.razor ~line 637
    //   Purpose: Validate link table name is valid C# class name
    //   Checks: Starts with letter, valid chars, not reserved keyword

    // ============================================================
    // MANY-TO-MANY VALIDATION
    // ============================================================

    // TODO: Migration pending - ValidateManyToMany(RelationshipDefinition rel) -> List<string>
    //   Purpose: Validate M:N relationship configuration
    //   Checks:
    //   - Both entities exist
    //   - Link table name is valid identifier
    //   - Link table name doesn't conflict with existing entities
    //   - Navigation properties on both sides

    // TODO: Migration pending - CanSaveRelationship() -> bool (partial)
    //   Source: CRM.Client/Shared/Wizard/EntityWizard → FreeManager.App.EntityWizardRelationshipEditor.App.razor ~line 618
    //   Purpose: Check if M:M can be saved
    //   Requires: Valid link table name for M:M type

    // ============================================================
    // MANY-TO-MANY UI HELPERS
    // ============================================================

    // TODO: Migration pending - GetManyToManyDiagramHtml(RelationshipDefinition rel) -> string
    //   Purpose: Generate visual diagram for M:N
    //   Format: [EntityA] --N-- [LinkTable] --N-- [EntityB]
    //   Shows: Both navigation properties, link table

    // TODO: Migration pending - GetManyToManyExplanation(RelationshipDefinition rel) -> string
    //   Source: CRM.Client/Shared/Wizard/EntityWizard → FreeManager.App.EntityWizardRelationshipEditor.App.razor ~line 491
    //   Purpose: Plain English explanation
    //   Example: "A Student can enroll in multiple Courses. A Course can have multiple Students."
}
