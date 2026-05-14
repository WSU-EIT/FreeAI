using System;
using System.Collections.Generic;

namespace FreeA11yChecker.Console.SourceAnalysis;

/// <summary>
/// Provides short, one-sentence actionable "how to fix" hints for common
/// axe-core / IBM Equal Access accessibility rule IDs. Hints target WCAG 2.1
/// AA compliance scope and are intended to be surfaced next to violations
/// in reports or remediation UI.
/// </summary>
public static class QuickFixHints
{
    private static readonly Dictionary<string, string> Hints = new(StringComparer.OrdinalIgnoreCase)
    {
        // Images / media
        ["image-alt"] = "Add a meaningful `alt` attribute. Use `alt=\"\"` for decorative images.",
        ["image-redundant-alt"] = "Remove redundant words like 'image of' or 'picture of' from the `alt` text.",
        ["input-image-alt"] = "Add an `alt` attribute to the `<input type=\"image\">` describing its action.",
        ["area-alt"] = "Add an `alt` attribute to each `<area>` inside the image map.",
        ["object-alt"] = "Add accessible text inside the `<object>` element or via `aria-label`.",
        ["svg-img-alt"] = "Add a `<title>` child or `aria-label` to the `<svg role=\"img\">`.",
        ["audio-caption"] = "Provide a `<track kind=\"captions\">` for the `<audio>` element.",
        ["video-caption"] = "Provide a `<track kind=\"captions\">` for the `<video>` element.",
        ["video-description"] = "Provide an audio description track for the `<video>` element.",

        // Forms / labels
        ["label"] = "Wrap the input in a `<label>` or add `for=\"...\"` matching the input id.",
        ["label-title-only"] = "Provide a visible `<label>` — `title` alone is not a sufficient accessible name.",
        ["form-field-multiple-labels"] = "Associate the form control with exactly one `<label>`.",
        ["select-name"] = "Give the `<select>` an accessible name via `<label>` or `aria-label`.",
        ["input-button-name"] = "Add a `value` attribute or visible text to the button input.",
        ["autocomplete-valid"] = "Use a valid HTML autocomplete token (e.g., `name`, `email`, `tel`).",

        // Links / buttons
        ["link-name"] = "Add visible text or `aria-label` to the link.",
        ["button-name"] = "Add visible text or `aria-label` to the button.",
        ["link-in-text-block"] = "Distinguish the link from surrounding text with more than color alone (e.g., underline).",
        ["identical-links-same-purpose"] = "Links with the same accessible name must go to the same destination.",

        // Color / contrast
        ["color-contrast"] = "Increase color contrast. Need 4.5:1 for body text, 3:1 for large text.",
        ["color-contrast-enhanced"] = "Increase color contrast to 7:1 for body text, 4.5:1 for large text (AAA).",
        ["link-in-text-block-style"] = "Links in text blocks need 3:1 contrast against surrounding text plus a non-color indicator.",

        // Document / structure
        ["html-has-lang"] = "Add `lang=\"en\"` (or appropriate language) to the `<html>` element.",
        ["html-lang-valid"] = "Set a valid BCP-47 language value on the `<html>` element.",
        ["html-xml-lang-mismatch"] = "`lang` and `xml:lang` on `<html>` must specify the same language.",
        ["valid-lang"] = "Use a valid BCP-47 language code (e.g., `en`, `en-US`).",
        ["document-title"] = "Add a descriptive, non-empty `<title>` in the `<head>`.",
        ["meta-viewport"] = "Don't disable user zoom. Remove `user-scalable=no` and don't cap `maximum-scale`.",
        ["meta-refresh"] = "Avoid `<meta http-equiv=\"refresh\">` for auto-redirect or timed refresh.",

        // Landmarks / headings
        ["landmark-one-main"] = "Wrap the main page content in a single `<main>` element.",
        ["landmark-no-duplicate-main"] = "There should be only one `<main>` landmark on the page.",
        ["landmark-no-duplicate-banner"] = "There should be only one top-level `<header>` (banner) landmark.",
        ["landmark-no-duplicate-contentinfo"] = "There should be only one top-level `<footer>` (contentinfo) landmark.",
        ["landmark-unique"] = "Give landmarks of the same type unique `aria-label` values.",
        ["landmark-complementary-is-top-level"] = "`<aside>` (complementary) should be a top-level landmark, not nested.",
        ["region"] = "Add a landmark role (header/main/nav/aside/footer) around top-level content.",
        ["heading-order"] = "Don't skip heading levels. After `<h1>` use `<h2>`, then `<h3>`, etc.",
        ["page-has-heading-one"] = "Add an `<h1>` describing the page topic.",
        ["empty-heading"] = "Provide visible text inside the heading element.",

        // Skip links / keyboard
        ["bypass"] = "Add a 'Skip to main content' link as the first focusable element.",
        ["skip-link"] = "Ensure the skip link's target exists and is focusable.",
        ["tabindex"] = "Avoid positive tabindex. Use `0` or rely on natural source order.",
        ["focus-order-semantics"] = "Give focusable non-interactive elements a proper role (e.g., `button`, `link`).",
        ["accesskeys"] = "`accesskey` values must be unique across the page.",

        // ARIA
        ["aria-hidden-focus"] = "Don't use `aria-hidden=\"true\"` on focusable elements — they stay in tab order but are invisible to AT.",
        ["aria-hidden-body"] = "Never apply `aria-hidden=\"true\"` to the `<body>` element.",
        ["aria-required-attr"] = "Add the required ARIA attribute for this role.",
        ["aria-required-children"] = "This role requires specific child roles inside it.",
        ["aria-required-parent"] = "This role requires a specific parent role wrapping it.",
        ["aria-roles"] = "Use a valid ARIA role from the WAI-ARIA specification.",
        ["aria-valid-attr"] = "Remove or correct the misspelled `aria-*` attribute.",
        ["aria-valid-attr-value"] = "Set the `aria-*` attribute to a value allowed by the spec.",
        ["aria-allowed-attr"] = "Remove the ARIA attribute that isn't allowed on this role.",
        ["aria-allowed-role"] = "Use a role that's allowed on this element, or remove the `role` attribute.",
        ["aria-input-field-name"] = "Give the ARIA input an accessible name via `aria-label` or `aria-labelledby`.",
        ["aria-toggle-field-name"] = "Give the ARIA toggle (checkbox/switch/radio) an accessible name.",
        ["aria-command-name"] = "Give the ARIA command (button/link/menuitem) an accessible name.",
        ["aria-dialog-name"] = "Give the dialog an accessible name via `aria-label` or `aria-labelledby`.",
        ["aria-meter-name"] = "Give the `role=\"meter\"` element an accessible name.",
        ["aria-progressbar-name"] = "Give the `role=\"progressbar\"` element an accessible name.",
        ["aria-tooltip-name"] = "Give the `role=\"tooltip\"` element an accessible name.",
        ["aria-treeitem-name"] = "Give each `role=\"treeitem\"` an accessible name.",
        ["presentation-role-conflict"] = "Remove `role=\"presentation\"`/`role=\"none\"` from elements with global ARIA or focus.",

        // IDs / duplicates
        ["duplicate-id"] = "Make every `id` and `aria-labelledby` target unique on the page.",
        ["duplicate-id-aria"] = "Make every `id` and `aria-labelledby` target unique on the page.",
        ["duplicate-id-active"] = "Every active (focusable) element's `id` must be unique.",

        // Frames
        ["frame-title"] = "Add a `title` attribute to the `<iframe>`.",
        ["frame-title-unique"] = "Each `<iframe>` on the page must have a unique `title`.",
        ["frame-tested"] = "Ensure the iframe content is testable (axe must be able to run inside it).",
        ["frame-focusable-content"] = "Don't apply `tabindex=\"-1\"` to an iframe that has focusable content.",

        // Lists
        ["list"] = "List items must be inside `<ul>`, `<ol>`, or `<menu>`.",
        ["listitem"] = "An `<li>` must be a direct child of `<ul>`, `<ol>`, or `<menu>`.",
        ["definition-list"] = "A `<dl>` must contain only properly grouped `<dt>` and `<dd>` children.",
        ["dlitem"] = "`<dt>` and `<dd>` elements must be contained in a `<dl>`.",

        // Tables
        ["td-headers-attr"] = "Each `<td>`'s `headers` attribute must reference an existing `<th>` id.",
        ["th-has-data-cells"] = "A `<th>` with `scope=\"col\"` must have data cells beneath it.",
        ["scope-attr-valid"] = "Valid scope values are `row`, `col`, `rowgroup`, `colgroup`.",
        ["table-fake-caption"] = "Use a real `<caption>` element instead of a fake caption row.",
        ["td-has-header"] = "Each data cell in a layout table must reference a header cell.",
        ["empty-table-header"] = "`<th>` elements must contain visible text.",

        // Parsing / misc
        ["nested-interactive"] = "Don't nest interactive elements (e.g., a button inside a link).",
        ["no-autoplay-audio"] = "Don't autoplay audio longer than 3 seconds without a pause/stop control.",
        ["blink"] = "Remove `<blink>` — it's obsolete and causes accessibility issues.",
        ["marquee"] = "Remove `<marquee>` — it's obsolete and causes accessibility issues.",
        ["server-side-image-map"] = "Use a client-side image map (`<map>`) instead of a server-side one.",
        ["scrollable-region-focusable"] = "Scrollable regions must be keyboard focusable (e.g., `tabindex=\"0\"`).",
        ["css-orientation-lock"] = "Don't lock content to a single orientation unless it's essential.",
        ["target-size"] = "Interactive targets should be at least 24x24 CSS pixels (WCAG 2.2 AA)."
    };

    /// <summary>
    /// Returns a one-sentence actionable remediation hint for the given rule id,
    /// or <c>null</c> when the rule id isn't recognized. Matching is case-insensitive.
    /// </summary>
    /// <param name="ruleId">The axe-core / IBM Equal Access rule identifier.</param>
    /// <param name="snippet">Optional HTML snippet (reserved for future context-aware hints).</param>
    public static string? GetHint(string ruleId, string? snippet = null)
    {
        if (string.IsNullOrWhiteSpace(ruleId))
        {
            return null;
        }

        return Hints.TryGetValue(ruleId.Trim(), out var hint) ? hint : null;
    }

    /// <summary>Gets the number of rule hints currently registered.</summary>
    public static int Count => Hints.Count;
}
