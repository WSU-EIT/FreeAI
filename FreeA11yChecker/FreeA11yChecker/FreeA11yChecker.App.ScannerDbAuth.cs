using Microsoft.Playwright;
using FreeA11yChecker.Scanner;
using FreeA11yChecker.Scanner.Models;

namespace FreeA11yChecker;

/// <summary>
/// Thin wrapper that decrypts DB-stored credentials and delegates to the shared
/// Scanner library's AuthHandler for actual authentication logic.
/// </summary>
public static class ScannerDbAuth
{
    public static async Task<bool> AuthenticateAsync(IBrowserContext Context, DataObjects.Site Site,
        DataObjects.SiteCredential Credential, IDataAccess Da, IConfigurationHelper Config)
    {
        try {
            string plainPassword = Da.DecryptString(Credential.PasswordEncrypted, Config.ScanEncryptionKey);
            if (String.IsNullOrEmpty(plainPassword)) {
                return false;
            }

            var cred = new CredentialConfig {
                Username = Credential.Username,
                Password = plainPassword,
                AuthType = Credential.AuthType,
                TenantCode = Credential.TenantCode,
                LoginUrl = !String.IsNullOrWhiteSpace(Credential.LoginUrl)
                    ? Credential.LoginUrl
                    : Site.BaseUrl,
                UsernameSelector = Credential.UsernameSelector,
                PasswordSelector = Credential.PasswordSelector,
                SubmitSelector = Credential.SubmitSelector,
            };

            IPage page = Context.Pages.Count > 0 ? Context.Pages[0] : await Context.NewPageAsync();

            if (Credential.AuthType == "FreeCRM" || Site.IsFreeCRMApp) {
                return await AuthHandler.AuthenticateFreeCRM(page, cred);
            } else {
                return await AuthHandler.AuthenticateGeneric(page, cred);
            }
        } catch {
            return false;
        }
    }
}
