// ============================================================================
// SCAFFOLD: Awaiting migration from CRM.Client (split)
// Source: CRM.Client/Shared/Wizard/EntityWizard → FreeManager.App.EntityWizardStepProperties.App.razor
// Team: Team 4 (Client) - see docs/028_plan_team4-client.md
// Split: Property editor modal logic
// ============================================================================

namespace FreeManager.Client.Shared.Wizard;

/// <summary>
/// EntityWizardStepProperties editor partial - property add/edit modal functionality.
/// </summary>
public partial class EntityWizardStepProperties
{
    // ============================================================
    // PROPERTY MODAL STATE
    // ============================================================

    // TODO: Migration pending - modal state fields
    //   private bool _showPropertyModal = false;
    //   private PropertyDefinition? _editingProperty = null;
    //   private bool _showEnumModal = false;
    //   private EnumDefinition? _editingEnum = null;

    // ============================================================
    // PROPERTY CRUD METHODS
    // ============================================================

    // TODO: Migration pending - AddProperty()
    //   Source: CRM.Client/Shared/Wizard/EntityWizard → FreeManager.App.EntityWizardStepProperties.App.razor ~line 400
    //   Purpose: Open modal to add new property
    //   Creates: New PropertyDefinition with defaults
    //   Sets: _editingProperty, _showPropertyModal = true

    // TODO: Migration pending - EditProperty(PropertyDefinition prop)
    //   Source: CRM.Client/Shared/Wizard/EntityWizard → FreeManager.App.EntityWizardStepProperties.App.razor ~line 420
    //   Purpose: Open modal to edit existing property
    //   Clones: Property for editing (don't modify original until save)

    // TODO: Migration pending - SaveProperty()
    //   Source: CRM.Client/Shared/Wizard/EntityWizard → FreeManager.App.EntityWizardStepProperties.App.razor ~line 440
    //   Purpose: Save property from modal
    //   Validates: Property before saving
    //   Updates: Properties list

    // TODO: Migration pending - DeleteProperty(PropertyDefinition prop)
    //   Source: CRM.Client/Shared/Wizard/EntityWizard → FreeManager.App.EntityWizardStepProperties.App.razor ~line 460
    //   Purpose: Remove property from entity
    //   Prevents: Deleting primary key or system fields

    // ============================================================
    // PROPERTY ORDERING
    // ============================================================

    // TODO: Migration pending - MovePropertyUp(PropertyDefinition prop)
    //   Source: CRM.Client/Shared/Wizard/EntityWizard → FreeManager.App.EntityWizardStepProperties.App.razor ~line 480
    //   Purpose: Move property up in sort order

    // TODO: Migration pending - MovePropertyDown(PropertyDefinition prop)
    //   Source: CRM.Client/Shared/Wizard/EntityWizard → FreeManager.App.EntityWizardStepProperties.App.razor ~line 500
    //   Purpose: Move property down in sort order

    // ============================================================
    // ENUM EDITOR METHODS
    // ============================================================

    // TODO: Migration pending - AddEnumFromProperty()
    //   Source: CRM.Client/Shared/Wizard/EntityWizard → FreeManager.App.EntityWizardStepProperties.App.razor ~line 520
    //   Purpose: Create new enum for current property
    //   Opens: Enum editor modal

    // TODO: Migration pending - SaveEnum()
    //   Source: CRM.Client/Shared/Wizard/EntityWizard → FreeManager.App.EntityWizardStepProperties.App.razor ~line 540
    //   Purpose: Save enum definition
    //   Updates: Enums list and property type

    // ============================================================
    // MODAL EVENT HANDLERS
    // ============================================================

    // TODO: Migration pending - OnPropertyModalChanged(bool isOpen)
    //   Purpose: Handle modal open/close state changes

    // TODO: Migration pending - OnPropertyChanged(PropertyDefinition prop)
    //   Purpose: Handle property changes from modal
    //   Two-way binding callback

    // TODO: Migration pending - OnEnumModalChanged(bool isOpen)
    //   Purpose: Handle enum modal state changes

    // TODO: Migration pending - OnEnumDefChanged(EnumDefinition enumDef)
    //   Purpose: Handle enum changes from modal
}
