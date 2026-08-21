using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Plugins;

namespace FreeNavbarComponent.Server.Controllers;

public class AuthorizationController : ControllerBase
{
    private HttpContext? context;
    private IDataAccess da;
    private IPlugins plugins;
    private string _baseUrl = String.Empty;
    private string _requestedUrl = String.Empty;
    private string _fingerprint = String.Empty;
    private ICustomAuthentication? authenticationProviders;

    public AuthorizationController
    (
        IDataAccess daInjection,
        IHttpContextAccessor httpContextAccessor,
        IPlugins daPlugins,
        ICustomAuthentication auth
    )
    {
        authenticationProviders = auth;
        da = daInjection;
        plugins = daPlugins;

        if (authenticationProviders != null) {
            da.SetAuthenticationProviders(new DataObjects.AuthenticationProviders {
                UseApple = authenticationProviders.UseApple,
                UseFacebook = authenticationProviders.UseFacebook,
                UseGoogle = authenticationProviders.UseGoogle,
                UseMicrosoftAccount = authenticationProviders.UseMicrosoftAccount,
                UseOpenId = authenticationProviders.UseOpenId,
                OpenIdButtonText = authenticationProviders.OpenIdButtonText,
                OpenIdButtonClass = authenticationProviders.OpenIdButtonClass,
                OpenIdButtonIcon = authenticationProviders.OpenIdButtonIcon,
                OpenIdEmployeeIdClaim = authenticationProviders.OpenIdEmployeeIdClaim,
            });
        }

        if (httpContextAccessor != null && httpContextAccessor.HttpContext != null) {
            context = httpContextAccessor.HttpContext;
        }

        da.SetHttpContext(context);

        _fingerprint = da.Request("Fingerprint");

        _baseUrl = da.ApplicationURL;

        _requestedUrl = da.CookieRead("requested-url");
    }

    [HttpPost]
    [Route("~/Authorization/Custom")]
    public IActionResult CustomLogin()
    {
        string tenantId = da.Request("TenantId");
        string ssoToken = da.Request("sso-token");

        return Redirect(_baseUrl + "Authorization/Custom?TenantId=" + tenantId + "&sso-token=" + ssoToken + "&Fingerprint=" + _fingerprint);
    }

    [HttpPost]
    [Route("~/Authorization/Plugin")]
    public IActionResult PluginLogin()
    {
        string tenantId = da.Request("TenantId");
        string ssoToken = da.Request("sso-token");
        string pluginName = da.Request("Name");

        return Redirect(_baseUrl + "Authorization/Plugin?Name=" + pluginName + "&TenantId=" + tenantId + "&sso-token=" + ssoToken + "&Fingerprint=" + _fingerprint);
    }

    private void CookieWrite(string cookieName, string value)
    {
        if (context != null) {
            DateTime now = DateTime.Now;
            if (String.IsNullOrEmpty(cookieName)) { return; }

            Microsoft.AspNetCore.Http.CookieOptions option = new Microsoft.AspNetCore.Http.CookieOptions();
            option.Expires = now.AddYears(1);

            context.Response.Cookies.Append(da.CookiePrefix + cookieName, value, option);
        }
    }

    private string QueryStringValue(string valueName)
    {
        string output = String.Empty;

        if (context != null) {
            try {
                output += context.Request.Query[valueName].ToString();
            } catch { }
        }

        return output;
    }

    private string RequestValue(string parameter)
    {
        string output = String.Empty;

        if (context != null) {
            output = QueryStringValue(parameter);

            if (String.IsNullOrWhiteSpace(output)) {
                output += context.Request.Form[parameter].ToString();
            }
        }

        return output;
    }

    [Authorize(AuthenticationSchemes = "Apple")]
    [Route("~/Authorization/Apple/{id}")]
    public IActionResult Apple(Guid id)
    {
        return Redirect(da.ApplicationURL + "Authorization/AppleAuthorized/" + id.ToString() + "?Fingerprint=" + _fingerprint);
        //return RedirectToAction("AppleAuthorized", new { id = id.ToString() });
    }

    [Route("~/Authorization/AppleAuthorized/{id}")]
    public async Task<IActionResult> AppleAuthorized(Guid id)
    {
        var result = await ProcessClaims("Apple", id);
        if (result.Result) {
            if (!String.IsNullOrWhiteSpace(_requestedUrl)) {
                da.CookieWrite("requested-url", "");
                return Redirect(_requestedUrl);
            } else {
                return Redirect(_baseUrl);
            }
        } else {
            if (!String.IsNullOrWhiteSpace(result.Message)) {
                return Redirect(_baseUrl + "Authorization/" + result.Message);
            } else {
                return Redirect(_baseUrl + "Authorization/InvalidUser?AuthMethod=Apple");
            }
        }
    }

    [Authorize(AuthenticationSchemes = "Facebook")]
    [Route("~/Authorization/Facebook/{id}")]
    public IActionResult Facebook(Guid id)
    {
        return Redirect(da.ApplicationURL + "Authorization/FacebookAuthorized/" + id.ToString() + "?Fingerprint=" + _fingerprint);
        //return RedirectToAction("FacebookAuthorized", new { id = id.ToString() });
    }

    [Route("~/Authorization/FacebookAuthorized/{id}")]
    public async Task<IActionResult> FacebookAuthorized(Guid id)
    {
        var result = await ProcessClaims("Facebook", id);
        if (result.Result) {
            if (!String.IsNullOrWhiteSpace(_requestedUrl)) {
                da.CookieWrite("requested-url", "");
                return Redirect(_requestedUrl);
            } else {
                return Redirect(_baseUrl);
            }
        } else {
            if (!String.IsNullOrWhiteSpace(result.Message)) {
                return Redirect(_baseUrl + "Authorization/" + result.Message);
            } else {
                return Redirect(_baseUrl + "Authorization/InvalidUser?AuthMethod=Facebook");
            }
        }
    }

    [Authorize(AuthenticationSchemes = "Google")]
    [Route("~/Authorization/Google/{id}")]
    public IActionResult Google(Guid id)
    {
        return Redirect(da.ApplicationURL + "Authorization/GoogleAuthorized/" + id.ToString() + "?Fingerprint=" + _fingerprint);
        //return RedirectToAction("GoogleAuthorized", new { id = id.ToString() });
    }

    [Route("~/Authorization/GoogleAuthorized/{id}")]
    public async Task<IActionResult> GoogleAuthorized(Guid id)
    {
        var result = await ProcessClaims("Google", id);
        if (result.Result) {
            if (!String.IsNullOrWhiteSpace(_requestedUrl)) {
                da.CookieWrite("requested-url", "");
                return Redirect(_requestedUrl);
            } else {
                return Redirect(_baseUrl);
            }
        } else {
            if (!String.IsNullOrWhiteSpace(result.Message)) {
                return Redirect(_baseUrl + "Authorization/" + result.Message);
            } else {
                return Redirect(_baseUrl + "Authorization/InvalidUser?AuthMethod=Google");
            }
        }
    }

    [Authorize(AuthenticationSchemes = "MicrosoftAccount")]
    [Route("~/Authorization/MicrosoftAccount/{id}")]
    public IActionResult MicrosoftAccount(Guid id)
    {
        return Redirect(da.ApplicationURL + "Authorization/MicrosoftAccountAuthorized/" + id.ToString() + "?Fingerprint=" + _fingerprint);
        //return RedirectToAction("MicrosoftAccountAuthorized", new { id = id.ToString() });
    }

    [Route("~/Authorization/MicrosoftAccountAuthorized/{id}")]
    public async Task<IActionResult> MicrosoftAccountAuthorized(Guid id)
    {
        var result = await ProcessClaims("MicrosoftAccount", id);
        if (result.Result) {
            if (!String.IsNullOrWhiteSpace(_requestedUrl)) {
                da.CookieWrite("requested-url", "");
                return Redirect(_requestedUrl);
            } else {
                return Redirect(_baseUrl);
            }
        } else {
            if (!String.IsNullOrWhiteSpace(result.Message)) {
                return Redirect(_baseUrl + "Authorization/" + result.Message);
            } else {
                return Redirect(_baseUrl + "Authorization/InvalidUser?AuthMethod=MicrosoftAccount");
            }
        }
    }

    [Authorize(AuthenticationSchemes = "OpenId")]
    [Route("~/Authorization/OpenId/{id}")]
    public IActionResult OpenId(Guid id)
    {
        return Redirect(da.ApplicationURL + "Authorization/OpenIdAuthorized/" + id.ToString() + "?Fingerprint=" + _fingerprint);
        //return RedirectToAction("OpenIdAuthorized", new { id = id.ToString() });
    }

    [Route("~/Authorization/OpenIdAuthorized/{id}")]
    public async Task<IActionResult> OpenIdAuthorized(Guid id)
    {
        var result = await ProcessClaims("OpenId", id);
        if (result.Result) {
            if (!String.IsNullOrWhiteSpace(_requestedUrl)) {
                da.CookieWrite("requested-url", "");
                return Redirect(_requestedUrl);
            } else {
                return Redirect(_baseUrl);
            }
        } else {
            if (!String.IsNullOrWhiteSpace(result.Message)) {
                return Redirect(_baseUrl + "Authorization/" + result.Message);
            } else {
                return Redirect(_baseUrl + "Authorization/InvalidUser?AuthMethod=OpenId");
            }
        }
    }

    private async Task<DataObjects.SimpleResponse> ProcessClaims(string Source, Guid TenantId)
    {
        DataObjects.SimpleResponse output = new DataObjects.SimpleResponse();

        bool addedUser = false;
        bool validUser = false;
        bool noLocalAccount = false;

        DateTime now = DateTime.UtcNow;

        if (context != null) {
            if (context.User != null) {
                if (context.User.Identity != null) {
                    if (context.User.Identity.IsAuthenticated) {
                        validUser = true;

                        var claims = (System.Security.Claims.ClaimsIdentity)context.User.Identity;

                        if (claims != null && claims.Claims != null && claims.Claims.Any()) {
                            //Dictionary<string, string> allClaims = new Dictionary<string, string>();

                            string email = String.Empty;
                            string employeeId = String.Empty;
                            string givenName = String.Empty;
                            string familyName = String.Empty;
                            string preferredUsername = String.Empty;

                            var authProviders = da.GetAuthenticationProviders();
                            string openIdEmployeeIdClaim = da.StringValue(authProviders.OpenIdEmployeeIdClaim).ToLower();
                            if (String.IsNullOrWhiteSpace(openIdEmployeeIdClaim)) {
                                openIdEmployeeIdClaim = "employeeid";
                            }

                            var allClaims = claims.Claims.ToList();

                            foreach (var claim in allClaims) {
                                var claimType = GetClaimType(claim.Type).ToLower();

                                //allClaims.Add(claim.Type, claim.Value);

                                if (claimType.StartsWith("http:")) {
                                    // Ignore these
                                } else if (!String.IsNullOrWhiteSpace(openIdEmployeeIdClaim) && claimType.ToLower() == openIdEmployeeIdClaim) {
                                    if (String.IsNullOrWhiteSpace(employeeId)) {
                                        employeeId += claim.Value;
                                    }
                                } else {
                                    switch (claimType) {
                                        case "auth_time":
                                        case "jti":
                                        case "name":
                                            // Ignore these
                                            break;

                                        case "email":
                                        case "emailaddress":
                                        case "email_verified":
                                            if (String.IsNullOrWhiteSpace(email) && claim.Value.Contains("@")) {
                                                email += claim.Value;
                                            }
                                            break;

                                        case "preferred_username":
                                            if (String.IsNullOrWhiteSpace(preferredUsername)) {
                                                preferredUsername += claim.Value;
                                            }
                                            break;

                                        case "givenname":
                                        case "given_name":
                                            if (String.IsNullOrWhiteSpace(givenName)) {
                                                givenName += claim.Value;
                                            }
                                            break;

                                        case "surname":
                                        case "family_name":
                                            if (String.IsNullOrWhiteSpace(familyName)) {
                                                familyName += claim.Value;
                                            }
                                            break;

                                        default:
                                            if (claimType.Contains("email")) {
                                                if (String.IsNullOrWhiteSpace(email)) {
                                                    email += claim.Value;
                                                }
                                            } else if (claimType.Contains("username")) {
                                                if (String.IsNullOrWhiteSpace(preferredUsername)) {
                                                    preferredUsername += claim.Value;
                                                }
                                            } else if (claimType.Contains("employee")) {
                                                if (String.IsNullOrWhiteSpace(employeeId)) {
                                                    employeeId += claim.Value;
                                                }
                                            }

                                            break;
                                    }
                                }
                            }

                            // Now, check for any missing possible matches of claims
                            if (
                                String.IsNullOrWhiteSpace(email) || 
                                String.IsNullOrWhiteSpace(preferredUsername) ||
                                String.IsNullOrWhiteSpace(employeeId)
                            ){
                                foreach (var claim in allClaims) {
                                    var claimType = GetClaimType(claim.Type).ToLower();

                                    if (!claimType.StartsWith("http:")) {
                                        if (claimType.Contains("email")) {
                                            if (String.IsNullOrWhiteSpace(email)) {
                                                email += claim.Value;
                                            }
                                        } else if (claimType.Contains("username")) {
                                            if (String.IsNullOrWhiteSpace(preferredUsername)) {
                                                preferredUsername += claim.Value;
                                            }
                                        } else if (claimType.Contains("employee")) {
                                            if (String.IsNullOrWhiteSpace(employeeId)) {
                                                employeeId += claim.Value;
                                            }
                                        }
                                    }
                                }
                            }

                            if (String.IsNullOrWhiteSpace(preferredUsername) && !String.IsNullOrWhiteSpace(email)) {
                                preferredUsername = email;
                            }

                            if (String.IsNullOrWhiteSpace(email) && !String.IsNullOrWhiteSpace(preferredUsername)) {
                                preferredUsername = email;
                            }

                            if (!String.IsNullOrWhiteSpace(preferredUsername)) {
                                noLocalAccount = true;

                                DataObjects.User user = new DataObjects.User();

                                var tenant = da.GetTenant(TenantId);

                                user = await da.GetUserByUsernameOrEmail(TenantId, preferredUsername);
                                if (user == null || !user.ActionResponse.Result) {
                                    // See if this tenant allows for creating new accounts automatically.
                                    var settings = da.GetTenantSettings(TenantId);
                                    if (!settings.RequirePreExistingAccountToLogIn) {
                                        // Create the new account
                                        DataObjects.User addUser = new DataObjects.User {
                                            Added = now,
                                            AddedBy = Source,
                                            Admin = false,
                                            Deleted = false,
                                            Email = email,
                                            EmployeeId = employeeId,
                                            FirstName = givenName,
                                            Enabled = true,
                                            LastModified = now,
                                            LastModifiedBy = Source,
                                            LastName = familyName,
                                            ManageFiles = false,
                                            PreventPasswordChange = false,
                                            Source = Source,
                                            TenantId = TenantId,
                                            UserId = Guid.Empty,
                                            Username = preferredUsername,
                                        };

                                        user = await da.SaveUser(addUser);

                                        addedUser = true;
                                    }
                                }

                                if (user != null && user.ActionResponse.Result && user.Enabled) {
                                    output.Result = true;
                                    noLocalAccount = false;

                                    if (!addedUser) {
                                        // See if we need to make any updates based on data from the auth provider.
                                        bool updatesMade = false;

                                        if (user.Email != preferredUsername) {
                                            user.Email = preferredUsername;
                                            updatesMade = true;
                                        }

                                        if (user.FirstName != givenName) {
                                            user.FirstName = givenName;
                                            updatesMade = true;
                                        }

                                        if (user.LastName != familyName) {
                                            user.LastName = familyName;
                                            updatesMade = true;
                                        }

                                        if (user.EmployeeId != employeeId) {
                                            user.EmployeeId = employeeId;
                                            updatesMade = true;
                                        }

                                        if (updatesMade) {
                                            await da.SaveUser(user);
                                        }
                                    }

                                    await da.UpdateUserFromPlugins(user.UserId);

                                    if (String.IsNullOrWhiteSpace(user.AuthToken)) {
                                        user.AuthToken = da.GetUserToken(TenantId, user.UserId, _fingerprint, user.Sudo);
                                    }
                                    await CustomAuthorization.AddAuthetication(user, context, _fingerprint, Source);

                                    // Write out the user token
                                    CookieWrite("user-token", da.GetUserToken(TenantId, user.UserId, _fingerprint, user.Sudo));
                                    CookieWrite("Login-Method", Source);

                                    if (!user.Sudo) {
                                        await da.UpdateUserLastLoginTime(user.UserId, Source);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        if (validUser && noLocalAccount) {
            output.Message = "NoLocalAccount";
        }

        return output;
    }

    private string GetClaimType(string claimType)
    {
        string output = claimType;

        if (!String.IsNullOrWhiteSpace(claimType)) {
            if (claimType.Contains(@"\")) {
                claimType = claimType.Replace(@"\", "/");
            }

            if (claimType.Contains("/")) {
                int pos = claimType.LastIndexOf("/");
                output = claimType.Substring(pos + 1);
            }
        }

        return output;
    }
}