// ============================================================================
// SCAFFOLD: Awaiting migration from CRM.Client (split)
// Source: CRM.Client/Shared/Wizard/EntityWizard → FreeManager.App.EntityWizardStepProperties.App.razor
// Team: Team 4 (Client) - see docs/028_plan_team4-client.md
// Split: Property validation logic
// ============================================================================

namespace FreeManager.Client.Shared.Wizard;

/// <summary>
/// EntityWizardStepProperties validation partial - property and enum validation.
/// </summary>
public partial class EntityWizardStepProperties
{
    // ============================================================
    // PROPERTY NAME VALIDATION
    // ============================================================

    // TODO: Migration pending - ValidatePropertyName(string name) -> List<string>
    //   Purpose: Validate property name is valid C# identifier
    //   Checks:
    //   - Not null or empty
    //   - Starts with letter or underscore
    //   - Contains only valid characters
    //   - Not a C# reserved keyword
    //   - Not duplicate within entity

    // TODO: Migration pending - IsReservedKeyword(string name) -> bool
    //   Purpose: Check if name is C# reserved keyword
    //   List: abstract, as, base, bool, break, byte, case, catch, char...

    // TODO: Migration pending - IsDuplicatePropertyName(string name, Guid? excludeId) -> bool
    //   Purpose: Check if property name already exists in entity
    //   Excludes: Current property when editing

    // ============================================================
    // PROPERTY TYPE VALIDATION
    // ============================================================

    // TODO: Migration pending - ValidatePropertyType(PropertyDefinition prop) -> List<string>
    //   Purpose: Validate property type configuration
    //   Checks:
    //   - Type is selected
    //   - MaxLength is valid for string types
    //   - Enum reference exists for enum types
    //   - Default value is valid for type

    // TODO: Migration pending - ValidateDefaultValue(string value, PropertyType type) -> bool
    //   Purpose: Validate default value matches property type
    //   Parses: Value as target type

    // ============================================================
    // ENUM VALIDATION
    // ============================================================

    // TODO: Migration pending - ValidateEnumDefinition(EnumDefinition enumDef) -> List<string>
    //   Purpose: Validate enum definition
    //   Checks:
    //   - Enum name is valid identifier
    //   - At least one value defined
    //   - All values are valid identifiers
    //   - No duplicate values
    //   - Numeric values (if specified) are valid

    // TODO: Migration pending - IsDuplicateEnumName(string name, Guid? excludeId) -> bool
    //   Purpose: Check if enum name already exists
    //   Excludes: Current enum when editing

    // ============================================================
    // AGGREGATE VALIDATION
    // ============================================================

    // TODO: Migration pending - GetAllValidationErrors() -> List<string>
    //   Purpose: Get all validation errors for current entity
    //   Aggregates: Property errors, enum errors, relationship errors

    // TODO: Migration pending - HasValidationErrors() -> bool
    //   Purpose: Quick check if any errors exist
    //   Used: To disable Next button
}
