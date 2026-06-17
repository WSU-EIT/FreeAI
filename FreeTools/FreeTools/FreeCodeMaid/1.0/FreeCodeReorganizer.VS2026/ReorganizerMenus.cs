// The single "FreeCodeReorganizer" menu and its nested submenu.
//
// The new VS 2026 extensibility model only lets extensions place menus on the Extensions (or Tools)
// menu — there is no main-menu-bar placement — so this named menu sits under Extensions (the same
// place CodeMaid's menu renders in current Visual Studio). The common "Reorganize Document" sits at
// the menu's top level; the heavier, repo-wide command is tucked one level deeper in a nested
// "Repository" submenu.
namespace FreeCodeReorganizer.VS2026;

using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Commands;

internal static class ReorganizerMenus
{
    /// <summary>Nested submenu holding the repo-wide command.</summary>
    [VisualStudioContribution]
    public static MenuConfiguration RepositorySubmenu => new("%FreeCodeReorganizer.Menu.Repository.DisplayName%")
    {
        Children = new[]
        {
            MenuChild.Command<ReorganizeRepositoryCommand>(),
        },
    };

    /// <summary>The top-level "FreeCodeReorganizer" menu (under Extensions).</summary>
    [VisualStudioContribution]
    public static MenuConfiguration MainMenu => new("%FreeCodeReorganizer.Menu.DisplayName%")
    {
        Placements = new CommandPlacement[]
        {
            CommandPlacement.KnownPlacements.ExtensionsMenu,
        },
        Children = new[]
        {
            MenuChild.Command<ReorganizeDocumentCommand>(),
            MenuChild.Separator,
            MenuChild.Menu(RepositorySubmenu),
        },
    };
}
