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
