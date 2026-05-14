# ♿ Accessibility Rules Reference

> A complete reference of all accessibility rules checked by this scanner.
> Rules detected in this scan run are marked with instance counts.
> Click any rule name to jump to its detailed explanation.

> **Generated:** 2026-03-04 21:16:01 UTC  
> **Rules in this scan:** 8  
> **Total rules documented:** 27  

---

## 🎨 Severity Levels

| Icon | Level | Meaning |
|:----:|-------|---------|
| 🔴 | **Critical** | Blocks access entirely for some users. Must fix immediately. |
| 🟠 | **Serious** | Causes significant difficulty. Should fix as high priority. |
| 🟡 | **Moderate** | Causes some difficulty. Fix as part of regular maintenance. |
| 🔵 | **Minor** | Annoying but doesn't block access. Fix when possible. |

## 🔧 Tools

| Tool | Description |
|------|-------------|
| **axe** | [axe-core](https://github.com/dequelabs/axe-core) by Deque — industry-standard automated engine injected via Playwright |
| **htmlcheck** | Built-in HTML pattern scanner — regex-based structural checks (no browser needed) |
| **wave** | [WAVE API](https://wave.webaim.org/api/) by WebAIM — remote accessibility evaluation service |

## 📊 Confidence Scoring

When multiple tools check the same rule, confidence increases:

| Icon | Confidence | Meaning |
|:----:|:----------:|---------|
| 🟢 | **High** | 2+ tools agree this is a real issue (≥80% of capable tools) |
| 🟡 | **Medium** | Some tools found it, others didn't (50-79%) |
| 🔵 | **Low** | Only one tool flagged this — may be a false positive (<50%) |

---

## 📋 Quick Reference

| Rule | Sev | WCAG | Found | Instances | Description |
|------|:---:|:----:|:-----:|:---------:|-------------|
| [color-contrast](#color-contrast) | 🟠 | 2.a.a | ⚠️ 121 pg | 121 | Elements must have sufficient color contrast |
| [document-title](#document-title) | 🟠 | 2.4.2 | ⚠️ 3 pg | 3 | Document must have a title |
| [image-alt](#image-alt) | 🔴 | 1.1.1 | ⚠️ 6 pg | 12 | Images must have alternate text |
| [link-name](#link-name) | 🟠 | 2.4.4 | ⚠️ 122 pg | 490 | Links must have discernible text |
| [landmark-one-main](#landmark-one-main) | 🟡 | 1.3.1 | ⚠️ 124 pg | 124 | Page should have one main landmark |
| [page-has-heading-one](#page-has-heading-one) | 🟡 | 1.3.1 | ⚠️ 3 pg | 3 | Page should contain a level-one heading |
| [skip-link](#skip-link) | 🟡 | 2.4.1 | ⚠️ 124 pg | 124 | Page should have a skip navigation link |
| [landmark-nav](#landmark-nav) | 🔵 | 1.3.1 | ⚠️ 2 pg | 2 | Page should have a navigation landmark |
| [aria-allowed-attr](#aria-allowed-attr) | 🟠 | 4.1.2 | — | — | ARIA attributes must be allowed for the role |
| [aria-required-children](#aria-required-children) | 🟠 | 1.3.1 | — | — | ARIA roles must contain required children |
| [aria-valid-attr-value](#aria-valid-attr-value) | 🟠 | 4.1.2 | — | — | ARIA attributes must have valid values |
| [blink](#blink) | 🟠 | 2.2.2 | — | — | Blinking content must not be used |
| [button-name](#button-name) | 🟠 | 4.1.2 | — | — | Buttons must have discernible text |
| [html-has-lang](#html-has-lang) | 🟠 | 3.1.1 | — | — | HTML element must have a lang attribute |
| [html-lang-valid](#html-lang-valid) | 🟠 | 3.1.1 | — | — | HTML lang attribute must be valid |
| [input-image-alt](#input-image-alt) | 🟠 | 1.1.1 | — | — | Image buttons must have alternate text |
| [label](#label) | 🟠 | 1.3.1 | — | — | Form elements must have labels |
| [marquee](#marquee) | 🟠 | 2.2.2 | — | — | Marquee elements must not be used |
| [select-name](#select-name) | 🟠 | 1.3.1 | — | — | Select elements must have accessible names |
| [color-contrast-enhanced](#color-contrast-enhanced) | 🟡 | 1.4.6 | — | — | Elements must have enhanced color contrast |
| [div-button](#div-button) | 🟡 | 4.1.2 | — | — | Interactive divs should be buttons |
| [fieldset](#fieldset) | 🟡 | 1.3.1 | — | — | Related form fields should be grouped with fieldset |
| [heading-order](#heading-order) | 🟡 | 1.3.1 | — | — | Heading levels should increase by one |
| [meta-refresh](#meta-refresh) | 🟡 | 2.2.1 | — | — | Page must not use meta refresh |
| [tabindex](#tabindex) | 🟡 | 2.4.3 | — | — | Positive tabindex disrupts tab order |
| [table-fake-caption](#table-fake-caption) | 🟡 | 1.3.1 | — | — | Tables should use caption instead of cells for titles |
| [td-has-header](#td-has-header) | 🟡 | 1.3.1 | — | — | Data table cells must have headers |

---

## 📖 Rule Details

### 🟠 `color-contrast` {#color-contrast}

**Elements must have sufficient color contrast**

| Field | Value |
|-------|-------|
| Severity | 🟠 **serious** |
| WCAG | [2.a.a](https://www.w3.org/WAI/WCAG21/Understanding/2aa) |
| Instances in scan | **121** |
| Pages affected | 121 |
| Sites affected | 1 |
| Tools detecting | axe |
| Learn more | [https://dequeuniversity.com/rules/axe/4.10/color-contrast?application=axeAPI](https://dequeuniversity.com/rules/axe/4.10/color-contrast?application=axeAPI) |

**What this means:**

> Text must have a contrast ratio of at least 4.5:1 for normal text and 3:1 for large text against its background.

**How to fix:**

> Increase the contrast ratio by darkening text or lightening the background (or vice versa). Use a contrast checker tool.

**Example from scan:**

```html
<span class="icon-text"><!--!-->Theme</span>
```

---

### 🟠 `document-title` {#document-title}

**Document must have a title**

| Field | Value |
|-------|-------|
| Severity | 🟠 **serious** |
| WCAG | [2.4.2](https://www.w3.org/WAI/WCAG21/Understanding/page-titled) |
| Instances in scan | **3** |
| Pages affected | 3 |
| Sites affected | 1 |
| Tools detecting | axe |
| Learn more | [https://dequeuniversity.com/rules/axe/4.10/document-title?application=axeAPI](https://dequeuniversity.com/rules/axe/4.10/document-title?application=axeAPI) |

**What this means:**

> Every page must have a non-empty `<title>` element. The title is the first thing announced by screen readers.

**How to fix:**

> Add a descriptive `<title>` element inside `<head>`.

**Example from scan:**

```html
<html lang="en" style="--blazor-load-percentage: 21.96078431372549%; --blazor-load-percentage-text: &quot;21%&quot;;">
```

---

### 🔴 `image-alt` {#image-alt}

**Images must have alternate text**

| Field | Value |
|-------|-------|
| Severity | 🔴 **critical** |
| WCAG | [1.1.1](https://www.w3.org/WAI/WCAG21/Understanding/non-text-content) |
| Instances in scan | **12** |
| Pages affected | 6 |
| Sites affected | 1 |
| Tools detecting | axe, htmlcheck |
| Learn more | [https://dequeuniversity.com/rules/axe/4.10/image-alt?application=axeAPI](https://dequeuniversity.com/rules/axe/4.10/image-alt?application=axeAPI) |

**What this means:**

> Every `<img>` element must have an `alt` attribute that describes its content. Decorative images should use `alt=""`.

**How to fix:**

> Add a descriptive `alt` attribute. For decorative images, use `alt=""`. For complex images, consider `aria-describedby` linking to a longer description.

**Example from scan:**

```html
<img class="user-menu-icon" src="https://localhost:7271/File/View/39efbd88-a08d-496d-88ea-2a2459072e55">
```

---

### 🟠 `link-name` {#link-name}

**Links must have discernible text**

| Field | Value |
|-------|-------|
| Severity | 🟠 **serious** |
| WCAG | [2.4.4](https://www.w3.org/WAI/WCAG21/Understanding/link-purpose-in-context) |
| Instances in scan | **490** |
| Pages affected | 122 |
| Sites affected | 1 |
| Tools detecting | axe, htmlcheck |
| Learn more | [https://dequeuniversity.com/rules/axe/4.10/link-name?application=axeAPI](https://dequeuniversity.com/rules/axe/4.10/link-name?application=axeAPI) |

**What this means:**

> Every `<a>` element must have text content, an `aria-label`, or contain an `<img>` with alt text so screen readers can announce the link purpose.

**How to fix:**

> Add descriptive text inside the link, or add `aria-label="Description"`.

**Example from scan:**

```html
<a class="nav-link dropdown-toggle show" href="#" id="themeDropdown" role="button" data-bs-toggle="dropdown" aria-expanded="true"><!--!--><i class="ic...
```

---

### 🟡 `landmark-one-main` {#landmark-one-main}

**Page should have one main landmark**

| Field | Value |
|-------|-------|
| Severity | 🟡 **moderate** |
| WCAG | [1.3.1](https://www.w3.org/WAI/WCAG21/Understanding/info-and-relationships) |
| Instances in scan | **124** |
| Pages affected | 124 |
| Sites affected | 1 |
| Tools detecting | htmlcheck |
| Learn more | [https://dequeuniversity.com/rules/axe/4.10/landmark-one-main](https://dequeuniversity.com/rules/axe/4.10/landmark-one-main) |

**What this means:**

> Pages should have exactly one `<main>` landmark (or `role="main"`) so screen reader users can quickly jump to the primary content.

**How to fix:**

> Wrap your primary content in a `<main>` element.

---

### 🟡 `page-has-heading-one` {#page-has-heading-one}

**Page should contain a level-one heading**

| Field | Value |
|-------|-------|
| Severity | 🟡 **moderate** |
| WCAG | [1.3.1](https://www.w3.org/WAI/WCAG21/Understanding/info-and-relationships) |
| Instances in scan | **3** |
| Pages affected | 3 |
| Sites affected | 1 |
| Tools detecting | htmlcheck |
| Learn more | [https://dequeuniversity.com/rules/axe/4.10/page-has-heading-one](https://dequeuniversity.com/rules/axe/4.10/page-has-heading-one) |

**What this means:**

> Pages should have at least one `<h1>` element. The h1 typically matches the page title and helps screen reader users orient.

**How to fix:**

> Add a single `<h1>` element that describes the main content of the page.

---

### 🟡 `skip-link` {#skip-link}

**Page should have a skip navigation link**

| Field | Value |
|-------|-------|
| Severity | 🟡 **moderate** |
| WCAG | [2.4.1](https://www.w3.org/WAI/WCAG21/Understanding/bypass-blocks) |
| Instances in scan | **124** |
| Pages affected | 124 |
| Sites affected | 1 |
| Tools detecting | htmlcheck |
| Learn more | [https://dequeuniversity.com/rules/axe/4.10/skip-link](https://dequeuniversity.com/rules/axe/4.10/skip-link) |

**What this means:**

> A "Skip to main content" link at the top of the page allows keyboard users to bypass repetitive navigation.

**How to fix:**

> Add `<a href="#main-content" class="skip-link">Skip to main content</a>` as the first focusable element in the body.

---

### 🔵 `landmark-nav` {#landmark-nav}

**Page should have a navigation landmark**

| Field | Value |
|-------|-------|
| Severity | 🔵 **minor** |
| WCAG | [1.3.1](https://www.w3.org/WAI/WCAG21/Understanding/info-and-relationships) |
| Instances in scan | **2** |
| Pages affected | 2 |
| Sites affected | 1 |
| Tools detecting | htmlcheck |
| Learn more | [https://dequeuniversity.com/rules/axe/4.10/region](https://dequeuniversity.com/rules/axe/4.10/region) |

**What this means:**

> Navigation sections should be wrapped in `<nav>` elements (or `role="navigation"`) so screen readers can identify them.

**How to fix:**

> Wrap navigation links in a `<nav>` element.

---

### 🟠 `aria-allowed-attr` {#aria-allowed-attr}

**ARIA attributes must be allowed for the role**

| Field | Value |
|-------|-------|
| Severity | 🟠 **serious** |
| WCAG | [4.1.2](https://www.w3.org/WAI/WCAG21/Understanding/name-role-value) |
| Status in scan | ✅ Not detected |
| Learn more | [https://dequeuniversity.com/rules/axe/4.10/aria-allowed-attr](https://dequeuniversity.com/rules/axe/4.10/aria-allowed-attr) |

**What this means:**

> ARIA attributes used on an element must be valid for that element's role.

**How to fix:**

> Check the WAI-ARIA spec for which attributes are allowed on each role.

---

### 🟠 `aria-required-children` {#aria-required-children}

**ARIA roles must contain required children**

| Field | Value |
|-------|-------|
| Severity | 🟠 **serious** |
| WCAG | [1.3.1](https://www.w3.org/WAI/WCAG21/Understanding/info-and-relationships) |
| Status in scan | ✅ Not detected |
| Learn more | [https://dequeuniversity.com/rules/axe/4.10/aria-required-children](https://dequeuniversity.com/rules/axe/4.10/aria-required-children) |

**What this means:**

> Certain ARIA roles require specific child roles (e.g., `role="list"` must contain `role="listitem"`).

**How to fix:**

> Add the required child elements/roles as specified by the ARIA spec.

---

### 🟠 `aria-valid-attr-value` {#aria-valid-attr-value}

**ARIA attributes must have valid values**

| Field | Value |
|-------|-------|
| Severity | 🟠 **serious** |
| WCAG | [4.1.2](https://www.w3.org/WAI/WCAG21/Understanding/name-role-value) |
| Status in scan | ✅ Not detected |
| Learn more | [https://dequeuniversity.com/rules/axe/4.10/aria-valid-attr-value](https://dequeuniversity.com/rules/axe/4.10/aria-valid-attr-value) |

**What this means:**

> ARIA attribute values must conform to the spec (e.g., `aria-hidden` must be `true` or `false`).

**How to fix:**

> Correct the ARIA attribute value to match the specification.

---

### 🟠 `blink` {#blink}

**Blinking content must not be used**

| Field | Value |
|-------|-------|
| Severity | 🟠 **serious** |
| WCAG | [2.2.2](https://www.w3.org/WAI/WCAG21/Understanding/pause-stop-hide) |
| Status in scan | ✅ Not detected |
| Learn more | [https://dequeuniversity.com/rules/axe/4.10/blink](https://dequeuniversity.com/rules/axe/4.10/blink) |

**What this means:**

> The `<blink>` element causes content to flash, which can trigger seizures and is inaccessible.

**How to fix:**

> Remove all `<blink>` elements. Use CSS animations with `prefers-reduced-motion` support if animation is needed.

---

### 🟠 `button-name` {#button-name}

**Buttons must have discernible text**

| Field | Value |
|-------|-------|
| Severity | 🟠 **serious** |
| WCAG | [4.1.2](https://www.w3.org/WAI/WCAG21/Understanding/name-role-value) |
| Status in scan | ✅ Not detected |
| Learn more | [https://dequeuniversity.com/rules/axe/4.10/button-name](https://dequeuniversity.com/rules/axe/4.10/button-name) |

**What this means:**

> Every `<button>` element must have text content, `aria-label`, or `aria-labelledby` so screen readers can announce it.

**How to fix:**

> Add text inside the button, or add `aria-label="Action description"`.

---

### 🟠 `html-has-lang` {#html-has-lang}

**HTML element must have a lang attribute**

| Field | Value |
|-------|-------|
| Severity | 🟠 **serious** |
| WCAG | [3.1.1](https://www.w3.org/WAI/WCAG21/Understanding/language-of-page) |
| Status in scan | ✅ Not detected |
| Learn more | [https://dequeuniversity.com/rules/axe/4.10/html-has-lang](https://dequeuniversity.com/rules/axe/4.10/html-has-lang) |

**What this means:**

> The `<html>` element must have a `lang` attribute (e.g., `lang="en"`) so screen readers use the correct pronunciation.

**How to fix:**

> Add `lang="en"` (or appropriate language code) to the `<html>` element.

---

### 🟠 `html-lang-valid` {#html-lang-valid}

**HTML lang attribute must be valid**

| Field | Value |
|-------|-------|
| Severity | 🟠 **serious** |
| WCAG | [3.1.1](https://www.w3.org/WAI/WCAG21/Understanding/language-of-page) |
| Status in scan | ✅ Not detected |
| Learn more | [https://dequeuniversity.com/rules/axe/4.10/html-lang-valid](https://dequeuniversity.com/rules/axe/4.10/html-lang-valid) |

**What this means:**

> The `lang` attribute value must be a valid BCP 47 language tag (e.g., `en`, `en-US`, `fr`).

**How to fix:**

> Set `lang` to a valid BCP 47 code like `en` or `en-US`.

---

### 🟠 `input-image-alt` {#input-image-alt}

**Image buttons must have alternate text**

| Field | Value |
|-------|-------|
| Severity | 🟠 **serious** |
| WCAG | [1.1.1](https://www.w3.org/WAI/WCAG21/Understanding/non-text-content) |
| Status in scan | ✅ Not detected |
| Learn more | [https://dequeuniversity.com/rules/axe/4.10/input-image-alt](https://dequeuniversity.com/rules/axe/4.10/input-image-alt) |

**What this means:**

> `<input type="image">` elements must have an `alt` attribute describing the button action.

**How to fix:**

> Add `alt="Submit"` or similar action description to the image input.

---

### 🟠 `label` {#label}

**Form elements must have labels**

| Field | Value |
|-------|-------|
| Severity | 🟠 **serious** |
| WCAG | [1.3.1](https://www.w3.org/WAI/WCAG21/Understanding/info-and-relationships) |
| Status in scan | ✅ Not detected |
| Learn more | [https://dequeuniversity.com/rules/axe/4.10/label](https://dequeuniversity.com/rules/axe/4.10/label) |

**What this means:**

> Every form input (`<input>`, `<select>`, `<textarea>`) must have a programmatically associated label via `<label for="id">`, `aria-label`, or `aria-labelledby`.

**How to fix:**

> Add a `<label for="inputId">` element, or add `aria-label="Description"` to the input.

---

### 🟠 `marquee` {#marquee}

**Marquee elements must not be used**

| Field | Value |
|-------|-------|
| Severity | 🟠 **serious** |
| WCAG | [2.2.2](https://www.w3.org/WAI/WCAG21/Understanding/pause-stop-hide) |
| Status in scan | ✅ Not detected |
| Learn more | [https://dequeuniversity.com/rules/axe/4.10/marquee](https://dequeuniversity.com/rules/axe/4.10/marquee) |

**What this means:**

> The `<marquee>` element causes content to scroll automatically, which is disorienting and inaccessible.

**How to fix:**

> Remove `<marquee>` elements. Use CSS animations with pause controls if scrolling content is needed.

---

### 🟠 `select-name` {#select-name}

**Select elements must have accessible names**

| Field | Value |
|-------|-------|
| Severity | 🟠 **serious** |
| WCAG | [1.3.1](https://www.w3.org/WAI/WCAG21/Understanding/info-and-relationships) |
| Status in scan | ✅ Not detected |
| Learn more | [https://dequeuniversity.com/rules/axe/4.10/select-name](https://dequeuniversity.com/rules/axe/4.10/select-name) |

**What this means:**

> `<select>` elements must have an associated `<label>`, `aria-label`, or `aria-labelledby`.

**How to fix:**

> Add a `<label for="selectId">` or `aria-label` attribute.

---

### 🟡 `color-contrast-enhanced` {#color-contrast-enhanced}

**Elements must have enhanced color contrast**

| Field | Value |
|-------|-------|
| Severity | 🟡 **moderate** |
| WCAG | [1.4.6](https://www.w3.org/WAI/WCAG21/Understanding/contrast-enhanced) |
| Status in scan | ✅ Not detected |
| Learn more | [https://dequeuniversity.com/rules/axe/4.10/color-contrast-enhanced](https://dequeuniversity.com/rules/axe/4.10/color-contrast-enhanced) |

**What this means:**

> For WCAG AAA, text must have a contrast ratio of at least 7:1 for normal text and 4.5:1 for large text.

**How to fix:**

> Same as color-contrast but with stricter thresholds.

---

### 🟡 `div-button` {#div-button}

**Interactive divs should be buttons**

| Field | Value |
|-------|-------|
| Severity | 🟡 **moderate** |
| WCAG | [4.1.2](https://www.w3.org/WAI/WCAG21/Understanding/name-role-value) |
| Status in scan | ✅ Not detected |
| Learn more | [https://dequeuniversity.com/rules/axe/4.10/button-name](https://dequeuniversity.com/rules/axe/4.10/button-name) |

**What this means:**

> `<div>` elements with `onclick` handlers but no ARIA role are not keyboard-accessible. Use `<button>` instead.

**How to fix:**

> Replace `<div onclick="...">` with `<button>`. If you must use a div, add `role="button"`, `tabindex="0"`, and keyboard event handlers.

---

### 🟡 `fieldset` {#fieldset}

**Related form fields should be grouped with fieldset**

| Field | Value |
|-------|-------|
| Severity | 🟡 **moderate** |
| WCAG | [1.3.1](https://www.w3.org/WAI/WCAG21/Understanding/info-and-relationships) |
| Status in scan | ✅ Not detected |
| Learn more | [https://dequeuniversity.com/rules/axe/4.10/fieldset](https://dequeuniversity.com/rules/axe/4.10/fieldset) |

**What this means:**

> Groups of related checkboxes or radio buttons should be wrapped in `<fieldset>` with a `<legend>`.

**How to fix:**

> Wrap related inputs in `<fieldset>` and add a `<legend>` describing the group.

---

### 🟡 `heading-order` {#heading-order}

**Heading levels should increase by one**

| Field | Value |
|-------|-------|
| Severity | 🟡 **moderate** |
| WCAG | [1.3.1](https://www.w3.org/WAI/WCAG21/Understanding/info-and-relationships) |
| Status in scan | ✅ Not detected |
| Learn more | [https://dequeuniversity.com/rules/axe/4.10/heading-order](https://dequeuniversity.com/rules/axe/4.10/heading-order) |

**What this means:**

> Headings should not skip levels (e.g., `<h2>` to `<h4>`). Screen readers use heading hierarchy to understand page structure.

**How to fix:**

> Restructure headings so levels increase sequentially: h1 → h2 → h3, etc.

---

### 🟡 `meta-refresh` {#meta-refresh}

**Page must not use meta refresh**

| Field | Value |
|-------|-------|
| Severity | 🟡 **moderate** |
| WCAG | [2.2.1](https://www.w3.org/WAI/WCAG21/Understanding/timing-adjustable) |
| Status in scan | ✅ Not detected |
| Learn more | [https://dequeuniversity.com/rules/axe/4.10/meta-refresh](https://dequeuniversity.com/rules/axe/4.10/meta-refresh) |

**What this means:**

> `<meta http-equiv="refresh">` can disorient users, especially those using screen readers. Use server-side redirects instead.

**How to fix:**

> Remove the meta refresh tag and use HTTP 301/302 redirects on the server.

---

### 🟡 `tabindex` {#tabindex}

**Positive tabindex disrupts tab order**

| Field | Value |
|-------|-------|
| Severity | 🟡 **moderate** |
| WCAG | [2.4.3](https://www.w3.org/WAI/WCAG21/Understanding/focus-order) |
| Status in scan | ✅ Not detected |
| Learn more | [https://dequeuniversity.com/rules/axe/4.10/tabindex](https://dequeuniversity.com/rules/axe/4.10/tabindex) |

**What this means:**

> `tabindex` values greater than 0 create a custom tab order that is confusing. Use `tabindex="0"` or `tabindex="-1"` instead.

**How to fix:**

> Remove positive tabindex values. Rearrange DOM order to match desired tab sequence.

---

### 🟡 `table-fake-caption` {#table-fake-caption}

**Tables should use caption instead of cells for titles**

| Field | Value |
|-------|-------|
| Severity | 🟡 **moderate** |
| WCAG | [1.3.1](https://www.w3.org/WAI/WCAG21/Understanding/info-and-relationships) |
| Status in scan | ✅ Not detected |
| Learn more | [https://dequeuniversity.com/rules/axe/4.10/table-fake-caption](https://dequeuniversity.com/rules/axe/4.10/table-fake-caption) |

**What this means:**

> Data tables should use `<caption>` for the table title rather than a merged row of cells.

**How to fix:**

> Replace title rows with a `<caption>` element inside the `<table>`.

---

### 🟡 `td-has-header` {#td-has-header}

**Data table cells must have headers**

| Field | Value |
|-------|-------|
| Severity | 🟡 **moderate** |
| WCAG | [1.3.1](https://www.w3.org/WAI/WCAG21/Understanding/info-and-relationships) |
| Status in scan | ✅ Not detected |
| Learn more | [https://dequeuniversity.com/rules/axe/4.10/td-has-header](https://dequeuniversity.com/rules/axe/4.10/td-has-header) |

**What this means:**

> Non-empty `<td>` elements in a data table must have an associated `<th>` header, either via row/column position or explicit `headers` attribute.

**How to fix:**

> Add `<th>` elements in the first row or column. For complex tables, use `headers` and `id` attributes.

---

## 📚 WCAG Quick Reference

The rules above map to [WCAG 2.1 Level AA](https://www.w3.org/TR/WCAG21/) success criteria:

| WCAG | Principle | Guideline |
|:----:|-----------|-----------|
| 1.1.1 | Perceivable | Non-text Content — provide text alternatives |
| 1.3.1 | Perceivable | Info and Relationships — structure and relationships conveyed programmatically |
| 1.4.3 | Perceivable | Contrast (Minimum) — at least 4.5:1 ratio |
| 1.4.6 | Perceivable | Contrast (Enhanced) — at least 7:1 ratio (AAA) |
| 2.2.1 | Operable | Timing Adjustable — users can control time limits |
| 2.2.2 | Operable | Pause, Stop, Hide — moving content can be controlled |
| 2.4.1 | Operable | Bypass Blocks — skip repetitive content |
| 2.4.2 | Operable | Page Titled — descriptive page titles |
| 2.4.3 | Operable | Focus Order — logical tab sequence |
| 2.4.4 | Operable | Link Purpose — link text describes destination |
| 3.1.1 | Understandable | Language of Page — lang attribute on html |
| 4.1.2 | Robust | Name, Role, Value — UI components have accessible names |

---

*Generated by AccessibilityScanner (FreeTools) v1.0*
