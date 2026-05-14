using System.Text.Json;
using Microsoft.Playwright;

namespace FreeA11yChecker.Scanner;

/// <summary>
/// Audits form input elements for appropriate autocomplete attributes.
/// WCAG 1.3.5 (AA) — Identify Input Purpose: inputs collecting user data
/// should have autocomplete attributes matching a defined set of purposes.
/// </summary>
public static class AutocompleteAuditor
{
    /// <summary>
    /// Scans all input, select, and textarea elements for missing or inappropriate autocomplete values.
    /// Returns JSON array of findings.
    /// </summary>
    public static async Task<string> Run(IPage page)
    {
        try {
            string resultJson = await page.EvaluateAsync<string>(@"() => {
                // Map of input name/type patterns to expected autocomplete values.
                const patterns = [
                    { regex: /^(first.?name|fname|given.?name)/i, expected: 'given-name' },
                    { regex: /^(last.?name|lname|family.?name|surname)/i, expected: 'family-name' },
                    { regex: /^(full.?name|name)$/i, expected: 'name' },
                    { regex: /^(email|e.?mail)/i, expected: 'email' },
                    { regex: /^(phone|tel|mobile|cell)/i, expected: 'tel' },
                    { regex: /^(address|street|addr).?1/i, expected: 'address-line1' },
                    { regex: /^(address|street|addr).?2/i, expected: 'address-line2' },
                    { regex: /^(city|town|locality)/i, expected: 'address-level2' },
                    { regex: /^(state|province|region)/i, expected: 'address-level1' },
                    { regex: /^(zip|postal|postcode)/i, expected: 'postal-code' },
                    { regex: /^(country)/i, expected: 'country-name' },
                    { regex: /^(user.?name|login)/i, expected: 'username' },
                    { regex: /^(password|pass|pwd)$/i, expected: 'current-password' },
                    { regex: /^(new.?password|confirm.?password)/i, expected: 'new-password' },
                    { regex: /^(cc.?number|card.?number|credit.?card)/i, expected: 'cc-number' },
                    { regex: /^(cc.?name|card.?name|cardholder)/i, expected: 'cc-name' },
                    { regex: /^(cc.?exp|expir)/i, expected: 'cc-exp' },
                    { regex: /^(cc.?csc|cvv|cvc|security.?code)/i, expected: 'cc-csc' },
                    { regex: /^(org|company|organization)/i, expected: 'organization' },
                    { regex: /^(title|honorific)/i, expected: 'honorific-prefix' },
                    { regex: /^(bday|birthday|date.?of.?birth|dob)/i, expected: 'bday' },
                    { regex: /^(url|website|homepage)/i, expected: 'url' },
                ];

                const results = [];
                const inputs = document.querySelectorAll('input, select, textarea');

                inputs.forEach(el => {
                    // Skip hidden, submit, button, reset, image inputs.
                    const type = (el.type || '').toLowerCase();
                    if (['hidden', 'submit', 'button', 'reset', 'image', 'file', 'checkbox', 'radio'].includes(type)) return;

                    const name = el.name || el.id || '';
                    const actualAc = el.getAttribute('autocomplete') || '';
                    const label = el.getAttribute('aria-label') || '';
                    // Try to find associated label text.
                    let labelText = label;
                    if (!labelText && el.id) {
                        const lbl = document.querySelector('label[for=""' + el.id + '""]');
                        if (lbl) labelText = lbl.textContent.trim().substring(0, 100);
                    }
                    if (!labelText && el.labels && el.labels.length > 0) {
                        labelText = el.labels[0].textContent.trim().substring(0, 100);
                    }

                    let selector = el.tagName.toLowerCase();
                    if (el.id) selector += '#' + el.id;
                    else if (el.name) selector += '[name=""' + el.name + '""]';

                    // Match against patterns using name, id, or label.
                    const testStrings = [name, el.id || '', labelText].filter(Boolean);
                    let expected = '';
                    for (const p of patterns) {
                        for (const s of testStrings) {
                            if (p.regex.test(s)) { expected = p.expected; break; }
                        }
                        if (expected) break;
                    }

                    // Report if: has an expected autocomplete but doesn't match,
                    // or has no autocomplete and matches a known pattern.
                    if (expected && actualAc !== expected && actualAc !== 'off' && actualAc !== 'on') {
                        results.push({
                            selector,
                            name,
                            type: el.type || el.tagName.toLowerCase(),
                            label: labelText,
                            expectedAutocomplete: expected,
                            actualAutocomplete: actualAc || '(missing)'
                        });
                    }
                });

                return JSON.stringify(results);
            }") ?? "[]";

            return resultJson;
        } catch {
            return "[]";
        }
    }
}
