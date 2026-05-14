using Microsoft.Playwright;
using FreeA11yChecker.Scanner.Models;

namespace FreeA11yChecker.Scanner;

/// <summary>
/// Handles authentication for the scanner when scanning pages that require login.
/// Supports two flows: FreeA11yChecker-native (known login URL patterns, known selectors)
/// and generic form detection (15-selector cascade for username, 6 for password,
/// 5 for submit). Moved from FreeA11yChecker.App.ScannerAuth to the shared library
/// so both the web app and console CLI can use the same auth logic.
/// </summary>
public static class AuthHandler
{
    // ================================================================
    // FreeA11yChecker / Native Authentication
    // ================================================================

    /// <summary>
    /// Authenticates against a FreeA11yChecker application using known login page structure.
    /// FreeA11yChecker apps have a predictable login page with known selectors, making this
    /// faster and more reliable than generic detection. Builds the login URL from
    /// the base URL and optional tenant code.
    /// </summary>
    public static async Task<bool> AuthenticateFreeCRM(IPage Page, CredentialConfig Credentials)
    {
        try {
            // Build login URL based on tenant code or custom login URL.
            string loginUrl = BuildLoginUrl(Page, Credentials);

            // Navigate to login page.
            await Page.GotoAsync(loginUrl, new PageGotoOptions {
                WaitUntil = WaitUntilState.NetworkIdle,
                Timeout = 30000,
            });

            // If the login page shows provider selection (e.g., Local, Google, etc.),
            // click the local account button first to reveal the username/password fields.
            ILocator localLoginButton = Page.Locator("#login-button-local");
            if (await localLoginButton.CountAsync() > 0) {
                await localLoginButton.ClickAsync();
                await Page.WaitForTimeoutAsync(1000);
            }

            // Fill username using known standard selectors.
            string usernameSelector = !String.IsNullOrWhiteSpace(Credentials.UsernameSelector)
                ? Credentials.UsernameSelector : "input[name='username'], #login-email";
            ILocator usernameField = Page.Locator(usernameSelector).First;
            await usernameField.FillAsync(Credentials.Username);

            // Fill password using known selector.
            string passwordSelector = !String.IsNullOrWhiteSpace(Credentials.PasswordSelector)
                ? Credentials.PasswordSelector : "input[type='password']";
            ILocator passwordField = Page.Locator(passwordSelector).First;
            await passwordField.FillAsync(Credentials.Password);

            // Click submit button.
            string submitSelector = !String.IsNullOrWhiteSpace(Credentials.SubmitSelector)
                ? Credentials.SubmitSelector : "button[type='submit']";
            ILocator submitButton = Page.Locator(submitSelector).First;
            await submitButton.ClickAsync();

            // Wait for navigation after login.
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions {
                Timeout = 15000,
            });

            // Verify authentication succeeded.
            bool verified = await VerifyAuthenticated(Page);
            return verified;
        } catch (Exception) {
            return false;
        }
    }

    // ================================================================
    // Auth Screenshot Capture (for compliance evidence)
    // ================================================================

    /// <summary>
    /// Result of an auth attempt that also captured login-flow screenshots.
    /// Screenshots are provided as raw JPEG bytes plus a suggested filename,
    /// so the caller can add them to a PageScanResult.Screenshots list.
    /// The three screenshots prove the "locked out anonymous / unlocked authenticated"
    /// pair that every AA compliance report needs.
    /// </summary>
    public class AuthScreenshotResult
    {
        /// <summary>True if authentication verified successfully.</summary>
        public bool Succeeded { get; set; }

        /// <summary>Login form as it loaded, before any credentials were filled.</summary>
        public byte[]? AnonymousFormBytes { get; set; }

        /// <summary>Login form after username + password filled, before submit.</summary>
        public byte[]? CredentialsEnteredBytes { get; set; }

        /// <summary>Landed page after successful submit — or the error state if auth failed.</summary>
        public byte[]? PostAuthBytes { get; set; }

        /// <summary>True if PostAuthBytes depicts a failure (so callers can rename it).</summary>
        public bool PostAuthIsFailure { get; set; }
    }

    /// <summary>
    /// Performs authentication while capturing three named screenshots — anonymous form,
    /// credentials-entered pre-submit, and post-auth landed page (or failure state).
    /// Dispatches to FreeA11yChecker-native or generic flows internally. The caller receives raw byte
    /// arrays for each screenshot; an empty slot (null) means capture failed silently.
    /// </summary>
    public static async Task<AuthScreenshotResult> AuthenticateWithScreenshots(
        IPage Page, CredentialConfig Credentials, Action<string>? OnStep = null)
    {
        AuthScreenshotResult output = new AuthScreenshotResult();

        try {
            // ---------- Navigate to login URL ----------
            string loginUrl = BuildLoginUrl(Page, Credentials);
            OnStep?.Invoke($"auth: navigating to {loginUrl}");
            await Page.GotoAsync(loginUrl, new PageGotoOptions {
                WaitUntil = WaitUntilState.NetworkIdle,
                Timeout = 30000,
            });

            // Post-nav SPA hydration. NetworkIdle fires when the framework bundle has
            // downloaded but Blazor WASM may take several more seconds to instantiate
            // and render. Wait for ANY interactive element (input, button, anchor) to
            // appear before continuing. 15s cap; 99% of pages settle in <5s.
            OnStep?.Invoke("auth: waiting for any interactive element to mount (Blazor hydration signal, up to 15s)");
            try {
                await Page.WaitForSelectorAsync("input, button, a", new PageWaitForSelectorOptions {
                    State = WaitForSelectorState.Visible,
                    Timeout = 15000,
                });
            } catch { /* page rendered nothing visible — let downstream steps fail loudly */ }

            // ---------- TENANT SELECTION (multi-tenant app) ----------
            // FlexCRM /Login first shows a tenant chooser: a list of service desks each
            // with a "Select" button. The login form (#login-email / #login-password)
            // doesn't exist in DOM until you pick a tenant. Click the row matching the
            // configured TenantCode (e.g. "SFS" for Pullman Student Financial Services).
            // If TenantCode isn't set OR no chooser is present, this block silently no-ops
            // and we proceed to the standard login form.
            if (!String.IsNullOrWhiteSpace(Credentials.TenantCode)) {
                OnStep?.Invoke($"auth: checking for tenant chooser, looking for [{Credentials.TenantCode}] row (up to 15s)");
                try {
                    // Match a row containing the tenant code marker like "[SFS]". Walk up
                    // to the closest ancestor that contains a Select button, then click it.
                    // 15s wait — Blazor WASM hydration on Flex is variable (we've seen 1s
                    // and we've seen 12s on identical hardware). 5s caused intermittent
                    // failures where the chooser hadn't rendered yet but auth gave up.
                    string tag = "[" + Credentials.TenantCode + "]";
                    ILocator tenantRow = Page.Locator($"text={tag}").First;
                    int rowCount = 0;
                    try {
                        await tenantRow.WaitForAsync(new LocatorWaitForOptions {
                            State = WaitForSelectorState.Visible,
                            Timeout = 15000,
                        });
                        rowCount = 1;
                    } catch { /* no chooser visible — assume normal login form ahead */ }

                    if (rowCount > 0) {
                        OnStep?.Invoke($"  → tenant row '{tag}' found — clicking its Select button");
                        // Find the Select button that's a sibling/descendant within the
                        // same row container. Several layouts work: button right after
                        // the text node, or button inside the same tr/li/div.
                        ILocator selectBtn = tenantRow
                            .Locator("xpath=ancestor::*[descendant::button or descendant::a][1]//button[contains(translate(., 'SELCT', 'select'),'select')]")
                            .First;
                        if (await selectBtn.CountAsync() == 0) {
                            // Fallback: find any Select button in the same parent row.
                            selectBtn = tenantRow
                                .Locator("xpath=ancestor::*[descendant::button][1]//button[normalize-space()='Select']")
                                .First;
                        }
                        if (await selectBtn.CountAsync() == 0) {
                            // Final fallback: nearest Select button anywhere on page.
                            selectBtn = Page.Locator("button:has-text('Select')").First;
                        }
                        if (await selectBtn.CountAsync() > 0) {
                            await selectBtn.ClickAsync(new LocatorClickOptions { Timeout = 5000 });
                            OnStep?.Invoke("  → Select clicked — waiting up to 15s for login form to appear");
                            // After clicking the tenant Select button, FlexCRM transitions to
                            // a "Login Required" provider chooser (Local Account / Okta / Switch
                            // Tenants), NOT directly to the email/password form. Wait for either:
                            // the form input itself (if no provider chooser is shown) OR the
                            // local-account provider button (if it is). Whichever appears first
                            // means the tenant click worked. The next code block will handle
                            // clicking the local-account button if needed.
                            string formOrChooserSelector =
                                (!String.IsNullOrWhiteSpace(Credentials.UsernameSelector) ? Credentials.UsernameSelector : "#login-email")
                                + ", input[type='email'], input[type='password']"
                                + ", button:has-text('Log in with a Local Account'), #login-button-local";
                            try {
                                await Page.WaitForSelectorAsync(formOrChooserSelector, new PageWaitForSelectorOptions {
                                    State = WaitForSelectorState.Visible,
                                    Timeout = 15000,
                                });
                                OnStep?.Invoke($"  → tenant Select succeeded — next step (form or provider chooser) is now visible at {Page.Url}");
                            } catch {
                                OnStep?.Invoke("  → tenant Select clicked but neither login form nor provider chooser appeared in 15s; will keep trying anyway");
                            }
                        } else {
                            OnStep?.Invoke($"  → '{tag}' row found but no Select button could be located near it");
                        }
                    } else {
                        OnStep?.Invoke($"  → no tenant chooser row found for '{tag}' — assuming login form is direct");
                    }
                } catch (Exception ex) {
                    OnStep?.Invoke($"  → tenant selection error: {ex.GetType().Name}: {ex.Message.Split('\n')[0]}");
                }
            }

            // Brief hydration settle. We DON'T wait for #login-email here — on multi-tenant
            // FlexCRM the page first shows a tenant chooser, then a provider chooser,
            // and only the LAST step has the email input. Each subsequent block waits
            // for its own expected element.
            OnStep?.Invoke("auth: settling 3s for initial Blazor hydration");
            await Page.WaitForTimeoutAsync(3000);

            // ---------- 00a: anonymous login form ----------
            OnStep?.Invoke("auth: capturing anonymous form screenshot");
            output.AnonymousFormBytes = await SafeScreenshotAsync(Page);

            // ---------- Provider selection: click "Local Account" / local login button ----------
            // FlexCRM's post-tenant page is a "Login Required" screen with two buttons:
            //   - "Log in with a Local Account"  (admin / test users — what we want)
            //   - "Login with Okta Single Sign-On" (real users — too many redirects to test)
            // Older FreeA11yChecker/compatible templates use #login-button-local instead. We try multiple
            // selectors in order. SKIP entirely if an email/text input is already visible
            // (some sites have no provider chooser).
            OnStep?.Invoke($"auth: checking page state (URL={Page.Url})");
            int alreadyVisibleInputs = await Page.Locator("input[type='email'], input[type='password'], #login-email").CountAsync();
            if (alreadyVisibleInputs > 0) {
                OnStep?.Invoke("  → login form inputs already present, skipping provider-button hunt");
            } else {
                string[] localBtnSelectors = new[] {
                    "button:has-text('Log in with a Local Account')",  // FlexCRM Login Required screen
                    "a:has-text('Log in with a Local Account')",       // alt rendering
                    "button:has-text('Local Account')",                 // shorter variant
                    "#login-button-local",                              // standard FreeA11yChecker/local-account template
                    "button:has-text('Local')",                         // last-resort match (avoid matching Okta buttons)
                };
                bool clicked = false;
                foreach (string sel in localBtnSelectors) {
                    try {
                        ILocator btn = Page.Locator(sel).First;
                        await btn.WaitForAsync(new LocatorWaitForOptions {
                            State = WaitForSelectorState.Visible,
                            Timeout = 2000,
                        });
                        OnStep?.Invoke($"auth: clicking provider-selection button matched by '{sel}'");
                        await btn.ClickAsync(new LocatorClickOptions { Timeout = 5000 });
                        clicked = true;
                        break;
                    } catch { /* try next selector */ }
                }

                if (clicked) {
                    OnStep?.Invoke("  → waiting up to 15s for the email/password form to appear");
                    try {
                        await Page.WaitForSelectorAsync(
                            "input[type='email'], input[type='password'], #login-email",
                            new PageWaitForSelectorOptions {
                                State = WaitForSelectorState.Visible,
                                Timeout = 15000,
                            });
                        OnStep?.Invoke("  → login form appeared");
                    } catch {
                        OnStep?.Invoke("  → form inputs did not appear in 15s after provider-button click — will attempt fill anyway");
                    }
                } else {
                    OnStep?.Invoke("  → no Local Account button found AND no email/password inputs visible — page state unknown");
                }
            }
            OnStep?.Invoke($"auth: pre-fill state — URL={Page.Url}, email/password input count={await Page.Locator("input[type='email'], input[type='password'], #login-email").CountAsync()}");

            // ---------- Fill username ----------
            // Try the configured selector first; if it fails for any reason (not editable,
            // timeout, etc.), fall back to the FreeTools-style cascade. This is what makes
            // FreeTools.BrowserSnapshot succeed on the same page where our previous code
            // failed: when the configured selector misses, they walk a 17-deep fallback list
            // looking for ANY visible username-shaped input and fill that.
            OnStep?.Invoke($"auth: filling username '{Credentials.Username}' via selector '{Credentials.UsernameSelector}'");
            bool usernameFilled = await TryFillWithFallback(
                Page, Credentials.UsernameSelector, Credentials.Username,
                DefaultUsernameSelectors(Credentials.AuthType), "username", OnStep);

            // ---------- Fill password ----------
            OnStep?.Invoke($"auth: filling password via selector '{Credentials.PasswordSelector}'");
            bool passwordFilled = await TryFillWithFallback(
                Page, Credentials.PasswordSelector, Credentials.Password,
                DefaultPasswordSelectors(), "password", OnStep);

            // ---------- 00b: credentials-entered pre-submit ----------
            OnStep?.Invoke("auth: capturing credentials-entered screenshot");
            output.CredentialsEnteredBytes = await SafeScreenshotAsync(Page);

            // If fields never filled, short-circuit as a failure with current state as 00c.
            if (!usernameFilled || !passwordFilled) {
                OnStep?.Invoke($"auth: FAILED to fill credentials (user={usernameFilled}, pass={passwordFilled})");
                output.PostAuthBytes = await SafeScreenshotAsync(Page);
                output.PostAuthIsFailure = true;
                output.Succeeded = false;
                return output;
            }

            // ---------- Click submit ----------
            OnStep?.Invoke("auth: clicking submit");
            bool submitClicked = false;
            if (!String.IsNullOrWhiteSpace(Credentials.SubmitSelector)) {
                try {
                    await Page.Locator(Credentials.SubmitSelector).First.ClickAsync(
                        new LocatorClickOptions { Timeout = 10000 });
                    submitClicked = true;
                } catch { /* leave submitClicked=false */ }
            } else {
                submitClicked = await ClickFirstMatch(Page, DefaultSubmitSelectors());
            }

            if (!submitClicked) {
                OnStep?.Invoke("auth: FAILED to click submit button");
                output.PostAuthBytes = await SafeScreenshotAsync(Page);
                output.PostAuthIsFailure = true;
                output.Succeeded = false;
                return output;
            }

            // Wait for navigation / DOM settle after submit. Blazor SPAs may not
            // navigate (URL stays same, content swaps) so combine multiple signals.
            // First try a positive success signal: the login form disappearing.
            OnStep?.Invoke("auth: waiting for #login-email to become hidden (positive login-success signal, up to 15s)");
            bool loginFormGone = false;
            try {
                await Page.WaitForSelectorAsync("#login-email", new PageWaitForSelectorOptions {
                    State = WaitForSelectorState.Hidden,
                    Timeout = 15000,
                });
                loginFormGone = true;
                OnStep?.Invoke("auth: #login-email is hidden — login form went away, flagging success early");
            } catch {
                OnStep?.Invoke("auth: #login-email did not hide within 15s, falling through to NetworkIdle wait");
            }

            if (!loginFormGone) {
                OnStep?.Invoke("auth: waiting for post-submit network idle (up to 20s)");
                try {
                    await Page.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions {
                        Timeout = 20000,
                    });
                } catch { /* may already be idle */ }
            }
            // Extra settle for Blazor router redirect + UI swap.
            OnStep?.Invoke("auth: settling 3s for SPA router redirect");
            await Page.WaitForTimeoutAsync(3000);

            OnStep?.Invoke("auth: verifying authenticated state");
            bool verified = await VerifyAuthenticated(Page);

            // ---------- 00c: post-auth landed page (or failure) ----------
            OnStep?.Invoke($"auth: capturing post-auth screenshot ({(verified ? "SUCCESS" : "FAILED")})");
            output.PostAuthBytes = await SafeScreenshotAsync(Page);
            output.PostAuthIsFailure = !verified;
            output.Succeeded = verified;
            return output;
        } catch (Exception) {
            // On unexpected exception, still return whatever we captured so far,
            // marking the attempt as failed.
            if (output.PostAuthBytes == null) {
                output.PostAuthBytes = await SafeScreenshotAsync(Page);
            }
            output.PostAuthIsFailure = true;
            output.Succeeded = false;
            return output;
        }
    }

    /// <summary>
    /// Best-effort screenshot — returns null on failure instead of throwing so the
    /// auth flow continues even if one capture misses.
    /// </summary>
    private static async Task<byte[]?> SafeScreenshotAsync(IPage Page)
    {
        try {
            return await Page.ScreenshotAsync(new PageScreenshotOptions {
                FullPage = false,
                Timeout = 10000,
                Type = ScreenshotType.Jpeg,
                Quality = 90,
            });
        } catch {
            return null;
        }
    }

    /// <summary>
    /// Builds the login URL from credentials, reusing the same rules as the two
    /// existing authenticate methods (tenant URL pattern, explicit LoginUrl,
    /// or default /Login path on the current origin).
    /// </summary>
    private static string BuildLoginUrl(IPage Page, CredentialConfig Credentials)
    {
        if (!String.IsNullOrWhiteSpace(Credentials.LoginUrl)) {
            return Credentials.LoginUrl;
        }
        // Prefer Credentials.BaseUrl when set — preserves virtual-app sub-paths like
        // "https://host/Touchpoints" that GetBaseFromPage would strip down to just
        // "https://host". Fall back to live page URL if BaseUrl wasn't injected.
        string root = !String.IsNullOrWhiteSpace(Credentials.BaseUrl)
            ? Credentials.BaseUrl.TrimEnd('/')
            : GetBaseFromPage(Page).TrimEnd('/');
        if (Credentials.AuthType == "FreeCRM" && !String.IsNullOrWhiteSpace(Credentials.TenantCode)) {
            return root + "/" + Credentials.TenantCode + "/Login";
        }
        return root + "/Login";
    }

    private static string[] DefaultUsernameSelectors(string AuthType)
    {
        // 17-selector cascade — copied verbatim from
        // Examples/FreeTools/FreeTools.BrowserSnapshot/Program.cs:TryFillLoginFormAsync.
        // Same list whether AuthType is FreeCRM or Generic — the list already
        // covers standard #login-email, Blazor Identity's Input.Email, plain HTML email
        // inputs, plus webauthn variants. Adding our own quirks on top is just future-proofing.
        return new string[] {
            "input[id='login-email']",                      // FreeA11yChecker/FreeExamples local login
            "input[name='username']",
            "input[name='Username']",
            "input[name='email']",
            "input[name='Email']",
            "input[name='Input.Email']",                     // Blazor Identity (name attribute)
            "input[name='Input.Username']",                  // Blazor Identity (name attribute)
            "input[id='username']",
            "input[id='Username']",
            "input[id='email']",
            "input[id='Email']",
            "input[id='Input.Email']",                       // Blazor Identity (id with dot)
            "input[id='Input.Username']",                    // Blazor Identity (id with dot)
            "input[type='email']",
            "input[autocomplete='username']",
            "input[autocomplete='username webauthn']",        // Blazor Identity with passkey support
            "input[placeholder*='user' i]",
            "input[placeholder*='email' i]",
            // Local additions beyond FreeTools list:
            "input[id*='user' i]",
            "input[name='login']",
            "input[name='user']",
        };
    }

    private static string[] DefaultPasswordSelectors()
    {
        // 7-selector cascade — verbatim from FreeTools.BrowserSnapshot.
        return new string[] {
            "input[id='login-password']",                    // FreeA11yChecker/FreeExamples local login
            "input[name='password']",
            "input[name='Password']",
            "input[name='Input.Password']",                  // Blazor Identity (name attribute)
            "input[id='password']",
            "input[id='Password']",
            "input[id='Input.Password']",                    // Blazor Identity (id with dot)
            "input[type='password']",
            "input[autocomplete='current-password']",
        };
    }

    private static string[] DefaultSubmitSelectors()
    {
        return new string[] {
            "button[type='submit']", "input[type='submit']",
            "button:has-text('Log in')", "button:has-text('Login')",
            "button:has-text('Sign in')",
        };
    }

    // ================================================================
    // Generic Authentication
    // ================================================================

    /// <summary>
    /// Authenticates against a generic login form using a cascade of common selectors.
    /// Tries 15 username selectors, 6 password selectors, and 5 submit selectors in
    /// priority order. The first matching visible selector wins.
    /// </summary>
    public static async Task<bool> AuthenticateGeneric(IPage Page, CredentialConfig Credentials)
    {
        try {
            // Navigate to login URL or fall back to current page.
            string loginUrl = !String.IsNullOrWhiteSpace(Credentials.LoginUrl)
                ? Credentials.LoginUrl : GetBaseFromPage(Page).TrimEnd('/') + "/Login";

            await Page.GotoAsync(loginUrl, new PageGotoOptions {
                WaitUntil = WaitUntilState.NetworkIdle,
                Timeout = 30000,
            });

            // Wait for the login form to render.
            await Page.WaitForTimeoutAsync(2000);

            // Username selectors in priority order — 15 common patterns.
            string[] usernameSelectors = new string[] {
                "#login-email",
                "#Input_Email",
                "input[name='username']",
                "input[name='Username']",
                "input[name='Email']",
                "input[name='email']",
                "input[name='Input.Email']",
                "input[type='email']",
                "input[autocomplete='username']",
                "input[autocomplete='email']",
                "input[placeholder*='email' i]",
                "input[placeholder*='user' i]",
                "input[name='login']",
                "input[name='user']",
                "input[id*='user' i]",
            };

            // Find and fill username.
            if (!String.IsNullOrWhiteSpace(Credentials.UsernameSelector)) {
                await Page.Locator(Credentials.UsernameSelector).First.FillAsync(Credentials.Username);
            } else {
                bool filled = await FillFirstMatch(Page, usernameSelectors, Credentials.Username);
                if (!filled) {
                    return false;
                }
            }

            // Password selectors in priority order — 6 common patterns.
            string[] passwordSelectors = new string[] {
                "input[type='password']",
                "#login-password",
                "input[name='password']",
                "input[name='Password']",
                "input[name='Input.Password']",
                "input[autocomplete='current-password']",
            };

            // Find and fill password.
            if (!String.IsNullOrWhiteSpace(Credentials.PasswordSelector)) {
                await Page.Locator(Credentials.PasswordSelector).First.FillAsync(Credentials.Password);
            } else {
                bool filled = await FillFirstMatch(Page, passwordSelectors, Credentials.Password);
                if (!filled) {
                    return false;
                }
            }

            // Submit selectors in priority order — 5 common patterns.
            string[] submitSelectors = new string[] {
                "button[type='submit']",
                "input[type='submit']",
                "button:has-text('Log in')",
                "button:has-text('Login')",
                "button:has-text('Sign in')",
            };

            // Find and click submit.
            if (!String.IsNullOrWhiteSpace(Credentials.SubmitSelector)) {
                await Page.Locator(Credentials.SubmitSelector).First.ClickAsync();
            } else {
                await ClickFirstMatch(Page, submitSelectors);
            }

            // Wait for navigation after login.
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions {
                Timeout = 15000,
            });

            // Verify authentication succeeded.
            bool verified = await VerifyAuthenticated(Page);
            return verified;
        } catch (Exception) {
            return false;
        }
    }

    // ================================================================
    // Helper Methods
    // ================================================================

    /// <summary>
    /// Try the configured selector first. If it doesn't exist, isn't visible, isn't editable,
    /// or FillAsync throws, walk the cascade as a fallback (this is the FreeTools pattern —
    /// the configured selector is a HINT, not a hard requirement). Returns true if any
    /// matching field was successfully filled. Logs each step via OnStep.
    /// </summary>
    private static async Task<bool> TryFillWithFallback(
        IPage Page, string? configuredSelector, string value,
        string[] cascadeSelectors, string fieldKind, Action<string>? OnStep)
    {
        // 1. Try the configured selector with editability re-check.
        if (!String.IsNullOrWhiteSpace(configuredSelector)) {
            try {
                ILocator loc = Page.Locator(configuredSelector).First;
                int count = await loc.CountAsync();
                if (count > 0) {
                    bool editable = false;
                    try { editable = await loc.IsEditableAsync(); } catch { }
                    if (!editable) {
                        OnStep?.Invoke($"  → {fieldKind}: configured selector found but not editable, waiting 2s...");
                        await Page.WaitForTimeoutAsync(2000);
                    }
                    await loc.FillAsync(value, new LocatorFillOptions { Timeout = 15000 });
                    return true;
                }
                OnStep?.Invoke($"  → {fieldKind}: configured selector '{configuredSelector}' not in DOM, falling back to cascade");
            } catch (Exception ex) {
                OnStep?.Invoke($"  → {fieldKind}: configured selector failed ({ex.GetType().Name}: {ex.Message.Split('\n')[0]}), falling back to cascade");
            }
        }

        // 2. Cascade fallback — try each candidate selector until one fills.
        foreach (string selector in cascadeSelectors) {
            try {
                ILocator loc = Page.Locator(selector).First;
                int count = await loc.CountAsync();
                if (count > 0 && await loc.IsVisibleAsync()) {
                    await loc.FillAsync(value, new LocatorFillOptions { Timeout = 5000 });
                    OnStep?.Invoke($"  → {fieldKind}: filled via fallback selector '{selector}'");
                    return true;
                }
            } catch { /* try next */ }
        }

        OnStep?.Invoke($"  → {fieldKind}: NO selector matched (configured + {cascadeSelectors.Length} fallbacks all failed)");
        return false;
    }

    /// <summary>
    /// Tries each selector in order, fills the first one that is visible on the page.
    /// Returns true if a field was found and filled, false if no match was found.
    /// </summary>
    private static async Task<bool> FillFirstMatch(IPage Page, string[] Selectors, string Value)
    {
        foreach (string selector in Selectors) {
            try {
                ILocator locator = Page.Locator(selector).First;
                int count = await locator.CountAsync();
                if (count > 0 && await locator.IsVisibleAsync()) {
                    await locator.FillAsync(Value);
                    return true;
                }
            } catch {
                // Selector not found or not interactable — try next.
            }
        }
        return false;
    }

    /// <summary>
    /// Tries each selector in order, clicks the first one that is visible on the page.
    /// Returns true if a button was found and clicked, false if no match was found.
    /// </summary>
    private static async Task<bool> ClickFirstMatch(IPage Page, string[] Selectors)
    {
        foreach (string selector in Selectors) {
            try {
                ILocator locator = Page.Locator(selector).First;
                int count = await locator.CountAsync();
                if (count > 0 && await locator.IsVisibleAsync()) {
                    await locator.ClickAsync();
                    return true;
                }
            } catch {
                // Selector not found or not interactable — try next.
            }
        }
        return false;
    }

    /// <summary>
    /// Verifies authentication succeeded by checking the current URL.
    /// If the page redirected back to a login page, auth failed.
    /// </summary>
    private static async Task<bool> VerifyAuthenticated(IPage Page)
    {
        try {
            // Wait a moment for any final redirects.
            await Page.WaitForTimeoutAsync(1000);

            string currentUrl = Page.Url.ToLower();

            // If we're still on a login page, auth failed.
            bool onLoginPage = currentUrl.Contains("/login") ||
                currentUrl.Contains("/signin") ||
                currentUrl.Contains("/sign-in") ||
                currentUrl.Contains("/account/login");

            if (onLoginPage) {
                return false;
            }

            // Also check document.title — a page still titled "Login" / "Sign in"
            // indicates the login form is still being shown even if URL changed.
            try {
                string title = await Page.TitleAsync();
                string titleLower = (title ?? String.Empty).ToLower();
                if (titleLower.Contains("login") || titleLower.Contains("sign in")) {
                    return false;
                }
            } catch {
                // Title lookup failed — don't fail verification on that alone.
            }

            return true;
        } catch {
            return false;
        }
    }

    /// <summary>
    /// Extracts the base URL (scheme + host) from the current page URL.
    /// </summary>
    private static string GetBaseFromPage(IPage Page)
    {
        try {
            Uri uri = new Uri(Page.Url);
            return uri.GetLeftPart(UriPartial.Authority);
        } catch {
            return Page.Url;
        }
    }
}
