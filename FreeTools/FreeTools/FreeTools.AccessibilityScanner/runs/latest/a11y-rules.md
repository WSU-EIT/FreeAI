# ♿ Accessibility Rules Reference

> A complete reference of all accessibility rules checked by this scanner.
> Rules detected in this scan run are marked with instance counts.
> Click any rule name to jump to its detailed explanation.

> **Generated:** 2026-02-19 02:46:46 UTC  
> **Rules in this scan:** 29  
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
| [aria-required-parent](#aria-required-parent) | 🔴 | — | ⚠️ 76 pg | 337 | Certain ARIA roles must be contained by particular parents |
| [aria-valid-attr](#aria-valid-attr) | 🔴 | — | ⚠️ 2 pg | 7 | ARIA attributes must conform to valid names |
| [meta-viewport](#meta-viewport) | 🔴 | 2.a.a | ⚠️ 1 pg | 1 | Zooming and scaling must not be disabled |
| [aria-allowed-attr](#aria-allowed-attr) | 🔴 | 4.1.2 | ⚠️ 492 pg | 492 | ARIA attributes must be allowed for the role |
| [aria-hidden-focus](#aria-hidden-focus) | 🟠 | — | ⚠️ 1 pg | 1 | ARIA hidden element must not be focusable or contain focu... |
| [aria-prohibited-attr](#aria-prohibited-attr) | 🟠 | — | ⚠️ 2 pg | 2 | Elements must only use permitted ARIA attributes |
| [aria-required-children](#aria-required-children) | 🔴 | 1.3.1 | ⚠️ 76 pg | 76 | ARIA roles must contain required children |
| [aria-valid-attr-value](#aria-valid-attr-value) | 🔴 | 4.1.2 | ⚠️ 1 pg | 1 | ARIA attributes must have valid values |
| [button-name](#button-name) | 🟠 | 4.1.2 | ⚠️ 100 pg | 145 | Buttons must have discernible text |
| [color-contrast](#color-contrast) | 🟠 | 2.a.a | ⚠️ 80 pg | 317 | Elements must have sufficient color contrast |
| [definition-list](#definition-list) | 🟠 | — | ⚠️ 1 pg | 1 | <dl> elements must only directly contain properly-ordered... |
| [document-title](#document-title) | 🟠 | 2.4.2 | ⚠️ 30 pg | 30 | Document must have a title |
| [frame-title](#frame-title) | 🟠 | — | ⚠️ 30 pg | 55 | Frames must have an accessible name |
| [html-has-lang](#html-has-lang) | 🟠 | 3.1.1 | ⚠️ 31 pg | 62 | HTML element must have a lang attribute |
| [image-alt](#image-alt) | 🟠 | 1.1.1 | ⚠️ 524 pg | 1,599 | Images must have alternate text |
| [label](#label) | 🟠 | 1.3.1 | ⚠️ 452 pg | 682 | Form elements must have labels |
| [link-in-text-block](#link-in-text-block) | 🟠 | — | ⚠️ 6 pg | 20 | Links must be distinguishable without relying on color |
| [link-name](#link-name) | 🟠 | 4.1.2 | ⚠️ 355 pg | 1,102 | Links must have discernible text |
| [list](#list) | 🟠 | — | ⚠️ 6 pg | 6 | <ul> and <ol> must only directly contain <li>, <script> o... |
| [listitem](#listitem) | 🟠 | — | ⚠️ 1 pg | 8 | <li> elements must be contained in a <ul> or <ol> |
| [scrollable-region-focusable](#scrollable-region-focusable) | 🟠 | — | ⚠️ 5 pg | 163 | Scrollable region must have keyboard access |
| [select-name](#select-name) | 🔴 | 1.3.1 | ⚠️ 8 pg | 15 | Select elements must have accessible names |
| [heading-order](#heading-order) | 🟡 | 1.3.1 | ⚠️ 169 pg | 208 | Heading levels should increase by one |
| [landmark-one-main](#landmark-one-main) | 🟡 | 1.3.1 | ⚠️ 67 pg | 67 | Page should have one main landmark |
| [page-has-heading-one](#page-has-heading-one) | 🟡 | 1.3.1 | ⚠️ 46 pg | 46 | Page should contain a level-one heading |
| [skip-link](#skip-link) | 🟡 | 2.4.1 | ⚠️ 89 pg | 89 | Page should have a skip navigation link |
| [tabindex](#tabindex) | 🟡 | 2.4.3 | ⚠️ 3 pg | 15 | Positive tabindex disrupts tab order |
| [td-has-header](#td-has-header) | 🟡 | 1.3.1 | ⚠️ 63 pg | 129 | Data table cells must have headers |
| [landmark-nav](#landmark-nav) | 🔵 | 1.3.1 | ⚠️ 48 pg | 48 | Page should have a navigation landmark |
| [blink](#blink) | 🟠 | 2.2.2 | — | — | Blinking content must not be used |
| [html-lang-valid](#html-lang-valid) | 🟠 | 3.1.1 | — | — | HTML lang attribute must be valid |
| [input-image-alt](#input-image-alt) | 🟠 | 1.1.1 | — | — | Image buttons must have alternate text |
| [marquee](#marquee) | 🟠 | 2.2.2 | — | — | Marquee elements must not be used |
| [color-contrast-enhanced](#color-contrast-enhanced) | 🟡 | 1.4.6 | — | — | Elements must have enhanced color contrast |
| [div-button](#div-button) | 🟡 | 4.1.2 | — | — | Interactive divs should be buttons |
| [fieldset](#fieldset) | 🟡 | 1.3.1 | — | — | Related form fields should be grouped with fieldset |
| [meta-refresh](#meta-refresh) | 🟡 | 2.2.1 | — | — | Page must not use meta refresh |
| [table-fake-caption](#table-fake-caption) | 🟡 | 1.3.1 | — | — | Tables should use caption instead of cells for titles |

---

## 📖 Rule Details

### 🔴 `aria-required-parent` {#aria-required-parent}

**Certain ARIA roles must be contained by particular parents**

| Field | Value |
|-------|-------|
| Severity | 🔴 **critical** |
| Instances in scan | **337** |
| Pages affected | 76 |
| Sites affected | 4 |
| Tools detecting | axe |
| Learn more | [https://dequeuniversity.com/rules/axe/4.10/aria-required-parent?application=axeAPI](https://dequeuniversity.com/rules/axe/4.10/aria-required-parent?application=axeAPI) |

**Example from scan:**

```html
<a role="menuitem" id="login-link" href="/login.action?os_destination=%2Fspaces%2FITSERVICES%2Fpages%2F297837408%2FMicrosoft%2B365%2BTeams" class="   ...
```

---

### 🔴 `aria-valid-attr` {#aria-valid-attr}

**ARIA attributes must conform to valid names**

| Field | Value |
|-------|-------|
| Severity | 🔴 **critical** |
| Instances in scan | **7** |
| Pages affected | 2 |
| Sites affected | 2 |
| Tools detecting | axe |
| Learn more | [https://dequeuniversity.com/rules/axe/4.10/aria-valid-attr?application=axeAPI](https://dequeuniversity.com/rules/axe/4.10/aria-valid-attr?application=axeAPI) |

**Example from scan:**

```html
<ul class="wsu-footer-site__offsite-menu" aria-lable="Offsite menu">
```

---

### 🔴 `meta-viewport` {#meta-viewport}

**Zooming and scaling must not be disabled**

| Field | Value |
|-------|-------|
| Severity | 🔴 **critical** |
| WCAG | [2.a.a](https://www.w3.org/WAI/WCAG21/Understanding/2aa) |
| Instances in scan | **1** |
| Pages affected | 1 |
| Sites affected | 1 |
| Tools detecting | axe |
| Learn more | [https://dequeuniversity.com/rules/axe/4.10/meta-viewport?application=axeAPI](https://dequeuniversity.com/rules/axe/4.10/meta-viewport?application=axeAPI) |

**Example from scan:**

```html
<meta name="viewport" content="width=device-width, initial-scale=1, maximum-scale=1">
```

---

### 🔴 `aria-allowed-attr` {#aria-allowed-attr}

**ARIA attributes must be allowed for the role**

| Field | Value |
|-------|-------|
| Severity | 🔴 **critical** |
| WCAG | [4.1.2](https://www.w3.org/WAI/WCAG21/Understanding/name-role-value) |
| Instances in scan | **492** |
| Pages affected | 492 |
| Sites affected | 42 |
| Tools detecting | axe |
| Learn more | [https://dequeuniversity.com/rules/axe/4.10/aria-allowed-attr?application=axeAPI](https://dequeuniversity.com/rules/axe/4.10/aria-allowed-attr?application=axeAPI) |

**What this means:**

> ARIA attributes used on an element must be valid for that element's role.

**How to fix:**

> Check the WAI-ARIA spec for which attributes are allowed on each role.

**Example from scan:**

```html
<div id="wsu-navigation-vertical" class="wsu-slide-in-panel  wsu-navigation-vertical wsu-slide-in-panel--position-left wsu-slide-in-panel--overlay-non...
```

---

### 🟠 `aria-hidden-focus` {#aria-hidden-focus}

**ARIA hidden element must not be focusable or contain focusable elements**

| Field | Value |
|-------|-------|
| Severity | 🟠 **serious** |
| Instances in scan | **1** |
| Pages affected | 1 |
| Sites affected | 1 |
| Tools detecting | axe |
| Learn more | [https://dequeuniversity.com/rules/axe/4.10/aria-hidden-focus?application=axeAPI](https://dequeuniversity.com/rules/axe/4.10/aria-hidden-focus?application=axeAPI) |

**Example from scan:**

```html
<button aria-hidden="true" id="hidden_skip_to_content" type="button">SKIP NAVIGATION</button>
```

---

### 🟠 `aria-prohibited-attr` {#aria-prohibited-attr}

**Elements must only use permitted ARIA attributes**

| Field | Value |
|-------|-------|
| Severity | 🟠 **serious** |
| Instances in scan | **2** |
| Pages affected | 2 |
| Sites affected | 1 |
| Tools detecting | axe |
| Learn more | [https://dequeuniversity.com/rules/axe/4.10/aria-prohibited-attr?application=axeAPI](https://dequeuniversity.com/rules/axe/4.10/aria-prohibited-attr?application=axeAPI) |

**Example from scan:**

```html
<span aria-label="4:00 to 5:00"><span aria-hidden="true">4:00-5:00</span></span>
```

---

### 🔴 `aria-required-children` {#aria-required-children}

**ARIA roles must contain required children**

| Field | Value |
|-------|-------|
| Severity | 🔴 **critical** |
| WCAG | [1.3.1](https://www.w3.org/WAI/WCAG21/Understanding/info-and-relationships) |
| Instances in scan | **76** |
| Pages affected | 76 |
| Sites affected | 4 |
| Tools detecting | axe |
| Learn more | [https://dequeuniversity.com/rules/axe/4.10/aria-required-children?application=axeAPI](https://dequeuniversity.com/rules/axe/4.10/aria-required-children?application=axeAPI) |

**What this means:**

> Certain ARIA roles require specific child roles (e.g., `role="list"` must contain `role="listitem"`).

**How to fix:**

> Add the required child elements/roles as specified by the ARIA spec.

**Example from scan:**

```html
<ul role="list" aria-busy="true" class="plugin_pagetree_children_list plugin_pagetree_children_list_noleftspace">
```

---

### 🔴 `aria-valid-attr-value` {#aria-valid-attr-value}

**ARIA attributes must have valid values**

| Field | Value |
|-------|-------|
| Severity | 🔴 **critical** |
| WCAG | [4.1.2](https://www.w3.org/WAI/WCAG21/Understanding/name-role-value) |
| Instances in scan | **1** |
| Pages affected | 1 |
| Sites affected | 1 |
| Tools detecting | axe |
| Learn more | [https://dequeuniversity.com/rules/axe/4.10/aria-valid-attr-value?application=axeAPI](https://dequeuniversity.com/rules/axe/4.10/aria-valid-attr-value?application=axeAPI) |

**What this means:**

> ARIA attribute values must conform to the spec (e.g., `aria-hidden` must be `true` or `false`).

**How to fix:**

> Correct the ARIA attribute value to match the specification.

**Example from scan:**

```html
<button class="slider-button slider-active" type="button" aria-label="Slide 1 of 5" aria-setsize="5" aria-posinset="1" role="tab" aria-controls="slide...
```

---

### 🟠 `button-name` {#button-name}

**Buttons must have discernible text**

| Field | Value |
|-------|-------|
| Severity | 🟠 **serious** |
| WCAG | [4.1.2](https://www.w3.org/WAI/WCAG21/Understanding/name-role-value) |
| Instances in scan | **145** |
| Pages affected | 100 |
| Sites affected | 25 |
| Tools detecting | axe, htmlcheck |
| Learn more | [https://dequeuniversity.com/rules/axe/4.10/button-name](https://dequeuniversity.com/rules/axe/4.10/button-name) |

**What this means:**

> Every `<button>` element must have text content, `aria-label`, or `aria-labelledby` so screen readers can announce it.

**How to fix:**

> Add text inside the button, or add `aria-label="Action description"`.

**Example from scan:**

```html
<button class="pswp__button pswp__button--close wp-dark-mode-ignore" title="Close [Esc]"></button>
```

---

### 🟠 `color-contrast` {#color-contrast}

**Elements must have sufficient color contrast**

| Field | Value |
|-------|-------|
| Severity | 🟠 **serious** |
| WCAG | [2.a.a](https://www.w3.org/WAI/WCAG21/Understanding/2aa) |
| Instances in scan | **317** |
| Pages affected | 80 |
| Sites affected | 22 |
| Tools detecting | axe |
| Learn more | [https://dequeuniversity.com/rules/axe/4.10/color-contrast?application=axeAPI](https://dequeuniversity.com/rules/axe/4.10/color-contrast?application=axeAPI) |

**What this means:**

> Text must have a contrast ratio of at least 4.5:1 for normal text and 3:1 for large text against its background.

**How to fix:**

> Increase the contrast ratio by darkening text or lightening the background (or vice versa). Use a contrast checker tool.

**Example from scan:**

```html
<a accesskey="I" href="/genindex.html" title="General Index">index</a>
```

---

### 🟠 `definition-list` {#definition-list}

**<dl> elements must only directly contain properly-ordered <dt> and <dd> groups, <script>, <template> or <div> elements**

| Field | Value |
|-------|-------|
| Severity | 🟠 **serious** |
| Instances in scan | **1** |
| Pages affected | 1 |
| Sites affected | 1 |
| Tools detecting | axe |
| Learn more | [https://dequeuniversity.com/rules/axe/4.10/definition-list?application=axeAPI](https://dequeuniversity.com/rules/axe/4.10/definition-list?application=axeAPI) |

**Example from scan:**

```html
<dl>
		
		<dd class="tribe-venue"> <a href="https://wsu.edu/digital-accessibility/venue/webinar/">Webinar</a> </dd>

		
		
		
			</dl>
```

---

### 🟠 `document-title` {#document-title}

**Document must have a title**

| Field | Value |
|-------|-------|
| Severity | 🟠 **serious** |
| WCAG | [2.4.2](https://www.w3.org/WAI/WCAG21/Understanding/page-titled) |
| Instances in scan | **30** |
| Pages affected | 30 |
| Sites affected | 11 |
| Tools detecting | axe |
| Learn more | [https://dequeuniversity.com/rules/axe/4.10/document-title?application=axeAPI](https://dequeuniversity.com/rules/axe/4.10/document-title?application=axeAPI) |

**What this means:**

> Every page must have a non-empty `<title>` element. The title is the first thing announced by screen readers.

**How to fix:**

> Add a descriptive `<title>` element inside `<head>`.

**Example from scan:**

```html
<html lang="en" style="--blazor-load-percentage: 97.22222222222221%; --blazor-load-percentage-text: &quot;97%&quot;;">
```

---

### 🟠 `frame-title` {#frame-title}

**Frames must have an accessible name**

| Field | Value |
|-------|-------|
| Severity | 🟠 **serious** |
| Instances in scan | **55** |
| Pages affected | 30 |
| Sites affected | 10 |
| Tools detecting | axe |
| Learn more | [https://dequeuniversity.com/rules/axe/4.10/frame-title?application=axeAPI](https://dequeuniversity.com/rules/axe/4.10/frame-title?application=axeAPI) |

**Example from scan:**

```html
<iframe width="300" height="1000" src="https://app.smartsheet.com/b/form/e1534e51af1c4c628689d4b6ad18c7ce"></iframe>
```

---

### 🟠 `html-has-lang` {#html-has-lang}

**HTML element must have a lang attribute**

| Field | Value |
|-------|-------|
| Severity | 🟠 **serious** |
| WCAG | [3.1.1](https://www.w3.org/WAI/WCAG21/Understanding/language-of-page) |
| Instances in scan | **62** |
| Pages affected | 31 |
| Sites affected | 10 |
| Tools detecting | axe, htmlcheck |
| Learn more | [https://dequeuniversity.com/rules/axe/4.10/html-has-lang?application=axeAPI](https://dequeuniversity.com/rules/axe/4.10/html-has-lang?application=axeAPI) |

**What this means:**

> The `<html>` element must have a `lang` attribute (e.g., `lang="en"`) so screen readers use the correct pronunciation.

**How to fix:**

> Add `lang="en"` (or appropriate language code) to the `<html>` element.

**Example from scan:**

```html
<html>
```

---

### 🟠 `image-alt` {#image-alt}

**Images must have alternate text**

| Field | Value |
|-------|-------|
| Severity | 🟠 **serious** |
| WCAG | [1.1.1](https://www.w3.org/WAI/WCAG21/Understanding/non-text-content) |
| Instances in scan | **1,599** |
| Pages affected | 524 |
| Sites affected | 31 |
| Tools detecting | axe, htmlcheck |
| Learn more | [https://dequeuniversity.com/rules/axe/4.10/image-alt](https://dequeuniversity.com/rules/axe/4.10/image-alt) |

**What this means:**

> Every `<img>` element must have an `alt` attribute that describes its content. Decorative images should use `alt=""`.

**How to fix:**

> Add a descriptive `alt` attribute. For decorative images, use `alt=""`. For complex images, consider `aria-describedby` linking to a longer description.

**Example from scan:**

```html
<img height="1" width="1" style="display:none" src="https://www.facebook.com/tr?id=352489839123111&amp;ev=PageView&amp;noscript=1">
```

---

### 🟠 `label` {#label}

**Form elements must have labels**

| Field | Value |
|-------|-------|
| Severity | 🟠 **serious** |
| WCAG | [1.3.1](https://www.w3.org/WAI/WCAG21/Understanding/info-and-relationships) |
| Instances in scan | **682** |
| Pages affected | 452 |
| Sites affected | 30 |
| Tools detecting | axe, htmlcheck |
| Learn more | [https://dequeuniversity.com/rules/axe/4.10/label](https://dequeuniversity.com/rules/axe/4.10/label) |

**What this means:**

> Every form input (`<input>`, `<select>`, `<textarea>`) must have a programmatically associated label via `<label for="id">`, `aria-label`, or `aria-labelledby`.

**How to fix:**

> Add a `<label for="inputId">` element, or add `aria-label="Description"` to the input.

**Example from scan:**

```html
<input type="search" class="search-field" placeholder="Search …" value="" name="s" tabindex="-1" style="">
```

---

### 🟠 `link-in-text-block` {#link-in-text-block}

**Links must be distinguishable without relying on color**

| Field | Value |
|-------|-------|
| Severity | 🟠 **serious** |
| Instances in scan | **20** |
| Pages affected | 6 |
| Sites affected | 5 |
| Tools detecting | axe |
| Learn more | [https://dequeuniversity.com/rules/axe/4.10/link-in-text-block?application=axeAPI](https://dequeuniversity.com/rules/axe/4.10/link-in-text-block?application=axeAPI) |

**Example from scan:**

```html
<a href="https://www.drs.wa.gov/plan/pers3/#plan-3-investment-withdrawals" data-type="link" data-id="https://www.drs.wa.gov/plan/pers3/#plan-3-investm...
```

---

### 🟠 `link-name` {#link-name}

**Links must have discernible text**

| Field | Value |
|-------|-------|
| Severity | 🟠 **serious** |
| WCAG | [4.1.2](https://www.w3.org/WAI/WCAG21/Understanding/name-role-value) |
| Instances in scan | **1,102** |
| Pages affected | 355 |
| Sites affected | 57 |
| Tools detecting | axe, htmlcheck |
| Learn more | [https://dequeuniversity.com/rules/axe/4.10/link-name](https://dequeuniversity.com/rules/axe/4.10/link-name) |

**What this means:**

> Every `<a>` element must have text content, an `aria-label`, or contain an `<img>` with alt text so screen readers can announce the link purpose.

**How to fix:**

> Add descriptive text inside the link, or add `aria-label="Description"`.

**Example from scan:**

```html
<a href="https://app.smartsheet.com/reports/rW5GmJvhc6HwG78J83WjJFG4rGvGRMXfmCRPJ641?view=grid" title="Chair &amp; Dean report" data-anchor="?view=gri...
```

---

### 🟠 `list` {#list}

**<ul> and <ol> must only directly contain <li>, <script> or <template> elements**

| Field | Value |
|-------|-------|
| Severity | 🟠 **serious** |
| Instances in scan | **6** |
| Pages affected | 6 |
| Sites affected | 5 |
| Tools detecting | axe |
| Learn more | [https://dequeuniversity.com/rules/axe/4.10/list?application=axeAPI](https://dequeuniversity.com/rules/axe/4.10/list?application=axeAPI) |

**Example from scan:**

```html
<ul>
```

---

### 🟠 `listitem` {#listitem}

**<li> elements must be contained in a <ul> or <ol>**

| Field | Value |
|-------|-------|
| Severity | 🟠 **serious** |
| Instances in scan | **8** |
| Pages affected | 1 |
| Sites affected | 1 |
| Tools detecting | axe |
| Learn more | [https://dequeuniversity.com/rules/axe/4.10/listitem?application=axeAPI](https://dequeuniversity.com/rules/axe/4.10/listitem?application=axeAPI) |

**Example from scan:**

```html
<li>
```

---

### 🟠 `scrollable-region-focusable` {#scrollable-region-focusable}

**Scrollable region must have keyboard access**

| Field | Value |
|-------|-------|
| Severity | 🟠 **serious** |
| Instances in scan | **163** |
| Pages affected | 5 |
| Sites affected | 1 |
| Tools detecting | axe |
| Learn more | [https://dequeuniversity.com/rules/axe/4.10/scrollable-region-focusable?application=axeAPI](https://dequeuniversity.com/rules/axe/4.10/scrollable-region-focusable?application=axeAPI) |

**Example from scan:**

```html
<tr class="row-1 odd">
	<th class="column-1">FY 26</th><th class="column-2">FY 27</th><th class="column-3">FY28</th>
</tr>
```

---

### 🔴 `select-name` {#select-name}

**Select elements must have accessible names**

| Field | Value |
|-------|-------|
| Severity | 🔴 **critical** |
| WCAG | [1.3.1](https://www.w3.org/WAI/WCAG21/Understanding/info-and-relationships) |
| Instances in scan | **15** |
| Pages affected | 8 |
| Sites affected | 6 |
| Tools detecting | axe |
| Learn more | [https://dequeuniversity.com/rules/axe/4.10/select-name?application=axeAPI](https://dequeuniversity.com/rules/axe/4.10/select-name?application=axeAPI) |

**What this means:**

> `<select>` elements must have an associated `<label>`, `aria-label`, or `aria-labelledby`.

**How to fix:**

> Add a `<label for="selectId">` or `aria-label` attribute.

**Example from scan:**

```html
<select id="wsuwp-scholarship-grade-level" class="wsu-scholarship-search__select" name="grade">
```

---

### 🟡 `heading-order` {#heading-order}

**Heading levels should increase by one**

| Field | Value |
|-------|-------|
| Severity | 🟡 **moderate** |
| WCAG | [1.3.1](https://www.w3.org/WAI/WCAG21/Understanding/info-and-relationships) |
| Instances in scan | **208** |
| Pages affected | 169 |
| Sites affected | 26 |
| Tools detecting | htmlcheck |
| Learn more | [https://dequeuniversity.com/rules/axe/4.10/heading-order](https://dequeuniversity.com/rules/axe/4.10/heading-order) |

**What this means:**

> Headings should not skip levels (e.g., `<h2>` to `<h4>`). Screen readers use heading hierarchy to understand page structure.

**How to fix:**

> Restructure headings so levels increase sequentially: h1 → h2 → h3, etc.

**Example from scan:**

```html
<h3>
```

---

### 🟡 `landmark-one-main` {#landmark-one-main}

**Page should have one main landmark**

| Field | Value |
|-------|-------|
| Severity | 🟡 **moderate** |
| WCAG | [1.3.1](https://www.w3.org/WAI/WCAG21/Understanding/info-and-relationships) |
| Instances in scan | **67** |
| Pages affected | 67 |
| Sites affected | 30 |
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
| Instances in scan | **46** |
| Pages affected | 46 |
| Sites affected | 19 |
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
| Instances in scan | **89** |
| Pages affected | 89 |
| Sites affected | 42 |
| Tools detecting | htmlcheck |
| Learn more | [https://dequeuniversity.com/rules/axe/4.10/skip-link](https://dequeuniversity.com/rules/axe/4.10/skip-link) |

**What this means:**

> A "Skip to main content" link at the top of the page allows keyboard users to bypass repetitive navigation.

**How to fix:**

> Add `<a href="#main-content" class="skip-link">Skip to main content</a>` as the first focusable element in the body.

---

### 🟡 `tabindex` {#tabindex}

**Positive tabindex disrupts tab order**

| Field | Value |
|-------|-------|
| Severity | 🟡 **moderate** |
| WCAG | [2.4.3](https://www.w3.org/WAI/WCAG21/Understanding/focus-order) |
| Instances in scan | **15** |
| Pages affected | 3 |
| Sites affected | 1 |
| Tools detecting | htmlcheck |
| Learn more | [https://dequeuniversity.com/rules/axe/4.10/tabindex](https://dequeuniversity.com/rules/axe/4.10/tabindex) |

**What this means:**

> `tabindex` values greater than 0 create a custom tab order that is confusing. Use `tabindex="0"` or `tabindex="-1"` instead.

**How to fix:**

> Remove positive tabindex values. Rearrange DOM order to match desired tab sequence.

**Example from scan:**

```html
tabindex="1"
```

---

### 🟡 `td-has-header` {#td-has-header}

**Data table cells must have headers**

| Field | Value |
|-------|-------|
| Severity | 🟡 **moderate** |
| WCAG | [1.3.1](https://www.w3.org/WAI/WCAG21/Understanding/info-and-relationships) |
| Instances in scan | **129** |
| Pages affected | 63 |
| Sites affected | 18 |
| Tools detecting | htmlcheck |
| Learn more | [https://dequeuniversity.com/rules/axe/4.10/td-has-header](https://dequeuniversity.com/rules/axe/4.10/td-has-header) |

**What this means:**

> Non-empty `<td>` elements in a data table must have an associated `<th>` header, either via row/column position or explicit `headers` attribute.

**How to fix:**

> Add `<th>` elements in the first row or column. For complex tables, use `headers` and `id` attributes.

**Example from scan:**

```html
<table><tbody><tr><td>
<h4>T – Threaten</h4>
</td><td>Employers cannot threaten employees with adverse action if they support or do not support a unio...
```

---

### 🔵 `landmark-nav` {#landmark-nav}

**Page should have a navigation landmark**

| Field | Value |
|-------|-------|
| Severity | 🔵 **minor** |
| WCAG | [1.3.1](https://www.w3.org/WAI/WCAG21/Understanding/info-and-relationships) |
| Instances in scan | **48** |
| Pages affected | 48 |
| Sites affected | 19 |
| Tools detecting | htmlcheck |
| Learn more | [https://dequeuniversity.com/rules/axe/4.10/region](https://dequeuniversity.com/rules/axe/4.10/region) |

**What this means:**

> Navigation sections should be wrapped in `<nav>` elements (or `role="navigation"`) so screen readers can identify them.

**How to fix:**

> Wrap navigation links in a `<nav>` element.

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
