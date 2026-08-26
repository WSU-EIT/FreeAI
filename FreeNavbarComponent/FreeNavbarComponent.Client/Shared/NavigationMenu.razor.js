// Keyboard navigation for the application's own nested navigation menu.
//
// The menu markup is the same disclosure tree the NavbarMenu component renders
// - branch rows are .accordion-button elements carrying aria-expanded, leaves
// are .dropdown-item links - so the behaviour is shared rather than duplicated:
// this module is a thin entry point over AttachNestedMenuKeys, which lives with
// the component that first needed it.
//
// Nothing else in this menu uses JavaScript. Expanding and collapsing is still
// Bootstrap's collapse data API, and the search box is still filtered in C#.
import { AttachNestedMenuKeys } from "./NavbarMenu.razor.js";

/// <summary>
/// Attaches nested-menu key handling to the navigation bar by element id.
/// Safe to call again after a re-render; the previous listener is replaced.
/// </summary>
export function InitializeKeyboard(rootId) {
    var root = document.getElementById(rootId);
    if (!root) {
        return;
    }

    AttachNestedMenuKeys(root);
}
