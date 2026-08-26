var dotNetHelper;

export function SetDotNetHelper(helper) {
    dotNetHelper = helper;
}

// Bootstrap's collapse/dropdown data API drives the menu itself, exactly as in the
// source design: every branch opens without any JavaScript from us. The bundle is
// already loaded globally by App.razor, so there are no resources to load here.
//
// This module only supplies the search filter over the rendered tree, and it adds
// no CSS: visibility is Bootstrap's own .d-none utility.

function slice(nodes) {
    return Array.prototype.slice.call(nodes);
}

/// <summary>
/// Wires the search box to the tree. Safe to call again after a re-render;
/// the previous listener is removed so filtering never gets double-applied.
/// </summary>
export function InitializeFilter(searchId, treeId, countId, levelCount) {
    var box = document.getElementById(searchId);
    var tree = document.getElementById(treeId);

    if (!box || !tree) {
        return;
    }

    // Re-registering on a re-render would stack listeners, so drop the old one first.
    if (box._navbarFilterHandler) {
        box.removeEventListener("input", box._navbarFilterHandler);
    }

    var handler = function () {
        ApplyFilter(box.value, treeId, countId, levelCount);
    };

    box._navbarFilterHandler = handler;
    box.addEventListener("input", handler);

    // Reflect the starting state (usually the full tree, collapsed).
    ApplyFilter(box.value, treeId, countId, levelCount);
}

export function ApplyFilter(query, treeId, countId, levelCount) {
    var tree = document.getElementById(treeId);
    if (!tree) {
        return 0;
    }

    var count = countId ? document.getElementById(countId) : null;
    var q = (query || "").trim().toLowerCase();
    var items = slice(tree.querySelectorAll(".accordion-item"));
    var leaves = slice(tree.querySelectorAll(".dropdown-item"));

    if (!q) {
        // Empty query: restore the whole tree, all branches closed.
        slice(tree.querySelectorAll(".d-none")).forEach(function (el) {
            el.classList.remove("d-none");
        });

        items.forEach(function (item) {
            var body = item.querySelector(".accordion-collapse");
            var btn = item.querySelector(".accordion-button");
            if (body) { body.classList.remove("show"); }
            if (btn) {
                btn.classList.add("collapsed");
                btn.setAttribute("aria-expanded", "false");
            }
        });

        SetCount(count, leaves.length, levelCount, true);
        NotifyFilter(query, leaves.length);
        return leaves.length;
    }

    // 1. a leaf survives on its own label
    leaves.forEach(function (leaf) {
        leaf.classList.toggle("d-none", leaf.textContent.toLowerCase().indexOf(q) < 0);
    });

    // 2. a branch whose own label matches reveals its whole subtree
    items.forEach(function (item) {
        var btn = item.querySelector(".accordion-button");
        var label = btn ? btn.textContent.toLowerCase() : "";
        item.hasOwnHit = label.indexOf(q) > -1;
        if (item.hasOwnHit) {
            slice(item.querySelectorAll(".d-none")).forEach(function (el) {
                el.classList.remove("d-none");
            });
        }
    });

    // 3. innermost branch first, so a deep hit keeps its ancestors alive
    items.slice().reverse().forEach(function (item) {
        var keep = item.hasOwnHit
            || item.querySelector(".dropdown-item:not(.d-none)")
            || item.querySelector(".accordion-item:not(.d-none)");

        item.classList.toggle("d-none", !keep);

        var btn = item.querySelector(".accordion-button");
        var body = item.querySelector(".accordion-collapse");
        if (body) { body.classList.add("show"); }
        if (btn) {
            btn.classList.remove("collapsed");
            btn.setAttribute("aria-expanded", "true");
        }
    });

    var shown = tree.querySelectorAll(".dropdown-item:not(.d-none)").length;
    SetCount(count, shown, levelCount, false);
    NotifyFilter(query, shown);
    return shown;
}

function SetCount(element, shown, levelCount, isEmptyQuery) {
    if (!element) {
        return;
    }

    if (isEmptyQuery) {
        element.textContent = shown + (shown === 1 ? " product" : " products")
            + " across " + levelCount + (levelCount === 1 ? " level" : " levels");
    } else {
        element.textContent = shown === 1
            ? "1 product matches"
            : shown + " products match";
    }
}

function NotifyFilter(query, shown) {
    if (dotNetHelper) {
        dotNetHelper.invokeMethodAsync("OnFilterChanged", query || "", shown);
    }
}

/// <summary>
/// Collapses every branch and clears the search box.
/// </summary>
export function ResetMenu(searchId, treeId, countId, levelCount) {
    var box = document.getElementById(searchId);
    if (box) {
        box.value = "";
    }
    ApplyFilter("", treeId, countId, levelCount);
}

/// <summary>
/// Keeps the dropdown panel on screen automatically. Bootstrap positions navbar
/// dropdowns statically (Popper never runs), so a panel anchored near a screen
/// edge can hang off it. On each open this measures the painted panel and flips
/// its alignment (.dropdown-menu-end) whichever way leaves it fully visible.
/// Safe to call again after a re-render; the previous listener is replaced.
/// </summary>
export function InitializeEdgeGuard(navId, enabled) {
    var nav = document.getElementById(navId);
    if (!nav) {
        return;
    }

    if (nav._edgeGuardHandler) {
        nav.removeEventListener("shown.bs.dropdown", nav._edgeGuardHandler);
        nav._edgeGuardHandler = null;
    }

    if (!enabled) {
        return;
    }

    var handler = function (e) {
        var host = e.target && e.target.closest ? e.target.closest(".dropdown, .dropup") : null;
        var menu = host ? host.querySelector(".dropdown-menu") : null;
        if (!menu) {
            return;
        }

        var vw = window.innerWidth;
        var r = menu.getBoundingClientRect();

        // Full-width phone mode positions the panel with CSS insets; nothing to do.
        if (r.width >= vw - 32) {
            return;
        }

        if (r.right > vw && !menu.classList.contains("dropdown-menu-end")) {
            // Hanging off the right: anchor to the toggle's right instead.
            menu.classList.add("dropdown-menu-end");
            menu._edgeGuardApplied = true;

            // If the flip pushed it off the left instead, prefer the original side.
            if (menu.getBoundingClientRect().left < 0) {
                menu.classList.remove("dropdown-menu-end");
                menu._edgeGuardApplied = false;
            }
        } else if (menu._edgeGuardApplied && r.left < 0) {
            // A guard-applied flip is now overflowing left (window grew or the
            // toggle moved): return to the default anchoring.
            menu.classList.remove("dropdown-menu-end");
            menu._edgeGuardApplied = false;
        }
    };

    nav.addEventListener("shown.bs.dropdown", handler);
    nav._edgeGuardHandler = handler;
}

// ---------------------------------------------------------------------------
// Keyboard navigation for the nested menu.
//
// The menu is a disclosure tree: branch rows are buttons carrying aria-expanded
// and leaves are links, all of them in the tab order. That alone satisfies
// WCAG 2.1.1, and Tab still walks the whole panel exactly as before. What this
// adds is the movement a sighted keyboard user expects from a menu, so a deep
// tree does not have to be crossed one Tab at a time:
//
//   Down / Up      previous or next visible row, wrapping at the ends
//   Home / End     first or last visible row
//   Right          expand a collapsed branch; on an open branch, enter it
//   Left           collapse an open branch; otherwise go up to the parent row
//   a-z, 0-9       jump to the next visible row starting with that character
//   Escape         left to Bootstrap, which closes the panel and restores focus
//
// Rows inside a collapsed branch are display:none, and rows filtered out by the
// search box carry .d-none, so both are skipped simply by ignoring anything
// with no client rects. Nothing here changes roles or ARIA: it only moves
// focus, so screen-reader behaviour is untouched (in browse mode the reader
// keeps its own arrow keys, which is the expected behaviour for this pattern).
// ---------------------------------------------------------------------------

/// <summary>
/// Attaches nested-menu key handling to a container by element id.
/// Safe to call again after a re-render; the previous listener is replaced.
/// </summary>
export function InitializeKeyboard(rootId) {
    var root = document.getElementById(rootId);
    if (!root) {
        return;
    }

    AttachNestedMenuKeys(root);
}

/// <summary>
/// Attaches nested-menu key handling to an element. Exported so other menus
/// built from the same markup can reuse it.
/// </summary>
export function AttachNestedMenuKeys(root) {
    if (!root) {
        return;
    }

    if (root._nestedMenuKeysHandler) {
        window.removeEventListener("keydown", root._nestedMenuKeysHandler, true);
        root._nestedMenuKeysHandler = null;
    }

    // This has to be a capturing listener on window, not a plain one on the
    // menu, because of how Bootstrap delegates: its dropdown key handling is
    // registered on document in the *capture* phase, and for ArrowUp and
    // ArrowDown it calls stopPropagation(). A listener anywhere inside the
    // document therefore never sees those two keys at all - the symptom is
    // every key working except the two that matter, with focus jumping to
    // whichever .dropdown-item Bootstrap picked. Capturing at window puts this
    // ahead of document, so the tree rows win and Bootstrap's flat-list
    // movement never runs.
    //
    // The listener is scoped by containment rather than by attachment, and it
    // unregisters itself once the menu leaves the DOM, so navigating around a
    // single-page app does not accumulate handlers.
    var handler = function (e) {
        if (!root.isConnected) {
            window.removeEventListener("keydown", handler, true);
            root._nestedMenuKeysHandler = null;
            return;
        }

        if (!root.contains(e.target)) {
            return;
        }

        HandleMenuKey(e, root);
    };

    window.addEventListener("keydown", handler, true);
    root._nestedMenuKeysHandler = handler;
}

// Every row a user can land on, in visual order, skipping collapsed branches,
// search-filtered rows and disabled links.
function MenuRows(panel) {
    var nodes = panel.querySelectorAll(".accordion-button, a.dropdown-item, input");
    var output = [];

    for (var i = 0; i < nodes.length; i++) {
        var el = nodes[i];

        if (el.disabled || el.classList.contains("disabled") || el.getAttribute("aria-disabled") === "true") {
            continue;
        }

        // No client rects covers both a collapsed branch (display:none) and a
        // row the search filter hid with .d-none.
        if (!el.getClientRects().length) {
            continue;
        }

        output.push(el);
    }

    return output;
}

function FocusRow(rows, index) {
    if (!rows.length) {
        return;
    }

    var i = index;
    if (i < 0) { i = rows.length - 1; }
    if (i >= rows.length) { i = 0; }

    rows[i].focus();
}

// The branch button that owns the subtree a row sits in, or null at the top.
function ParentBranch(panel, el) {
    var body = el.closest(".accordion-collapse");
    if (!body || !body.id) {
        return null;
    }

    return panel.querySelector('[aria-controls="' + body.id + '"]');
}

function IsBranch(el) {
    return el.classList && el.classList.contains("accordion-button");
}

function IsExpanded(el) {
    return el.getAttribute("aria-expanded") === "true";
}

function FirstRowInside(el) {
    var id = el.getAttribute("aria-controls");
    var body = id ? document.getElementById(id) : null;
    if (!body) {
        return null;
    }

    var rows = MenuRows(body);
    return rows.length ? rows[0] : null;
}

function NextMatch(rows, from, character) {
    var target = character.toLowerCase();

    for (var step = 1; step <= rows.length; step++) {
        var row = rows[(from + step + rows.length) % rows.length];
        var text = (row.textContent || "").trim().toLowerCase();
        if (text.charAt(0) === target) {
            return row;
        }
    }

    return null;
}

function HandleMenuKey(e, root) {
    var target = e.target;
    if (!target || !target.closest) {
        return;
    }

    var panel = target.closest(".dropdown-menu");
    if (!panel || !root.contains(panel)) {
        return;
    }

    // Bootstrap has its own arrow handling for .dropdown-item rows, bound at
    // the document. Anything handled here is stopped so focus does not move
    // twice; Escape is deliberately left alone so Bootstrap still closes the
    // panel and restores focus to the toggle.
    var handled = true;
    var rows = MenuRows(panel);
    var index = rows.indexOf(target);
    var typing = target.tagName === "INPUT";

    switch (e.key) {
        case "ArrowDown":
            FocusRow(rows, index + 1);
            break;

        case "ArrowUp":
            // Up and down work in the search box too: it is a single-line
            // field, so there is no line to move a caret to, and a filter box
            // that traps the arrow keys would strand anyone who typed into it.
            FocusRow(rows, index - 1);
            break;

        case "Home":
            if (typing) { return; }
            FocusRow(rows, 0);
            break;

        case "End":
            if (typing) { return; }
            FocusRow(rows, rows.length - 1);
            break;

        case "ArrowRight":
            if (typing) { return; }
            if (IsBranch(target)) {
                if (IsExpanded(target)) {
                    var first = FirstRowInside(target);
                    if (first) { first.focus(); }
                } else {
                    target.click();
                }
            } else {
                handled = false;
            }
            break;

        case "ArrowLeft":
            if (typing) { return; }
            if (IsBranch(target) && IsExpanded(target)) {
                target.click();
            } else {
                var parent = ParentBranch(panel, target);
                if (parent) { parent.focus(); } else { handled = false; }
            }
            break;

        default:
            if (typing || e.ctrlKey || e.altKey || e.metaKey || e.key.length !== 1 || !/[a-z0-9]/i.test(e.key)) {
                return;
            }

            var match = NextMatch(rows, index, e.key);
            if (match) { match.focus(); } else { handled = false; }
            break;
    }

    if (handled) {
        e.preventDefault();
        e.stopPropagation();
    }
}
