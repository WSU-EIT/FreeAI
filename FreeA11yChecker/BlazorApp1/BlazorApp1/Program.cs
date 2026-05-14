using BlazorApp1.Client.Pages;
using BlazorApp1.Components;
using BlazorApp1.Components.Account;
using BlazorApp1.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BlazorApp1
{
    public class Program
    {
        // Seeded test credentials — used by FreeA11yChecker scanner
        public const string SeedEmail = "admin@example.com";
        public const string SeedPassword = "Admin1234!";

        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddRazorComponents()
                .AddInteractiveWebAssemblyComponents()
                .AddAuthenticationStateSerialization();

            builder.Services.AddCascadingAuthenticationState();
            builder.Services.AddScoped<IdentityRedirectManager>();

            builder.Services.AddAuthentication(options => {
                options.DefaultScheme = IdentityConstants.ApplicationScheme;
                options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
            })
                .AddIdentityCookies();
            builder.Services.AddAuthorization();

            // Use InMemory database so no SQL Server setup is needed for scanning/testing.
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase("BlazorApp1-Test"));
            builder.Services.AddDatabaseDeveloperPageExceptionFilter();

            builder.Services.AddIdentityCore<ApplicationUser>(options => {
                // Disable email confirmation so the seeded user can log in immediately.
                options.SignIn.RequireConfirmedAccount = false;
                options.SignIn.RequireConfirmedEmail = false;
                options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
            })
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddSignInManager()
                .AddDefaultTokenProviders();

            builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

            var app = builder.Build();

            // Seed the admin test user on startup.
            await SeedTestUserAsync(app.Services);

            // Configure the HTTP request pipeline.
            if(app.Environment.IsDevelopment()) {
                app.UseWebAssemblyDebugging();
                app.UseMigrationsEndPoint();
            } else {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
            app.UseHttpsRedirection();

            app.UseAntiforgery();

            app.MapStaticAssets();
            app.MapRazorComponents<App>()
                .AddInteractiveWebAssemblyRenderMode()
                .AddAdditionalAssemblies(typeof(Client._Imports).Assembly);

            // Add additional endpoints required by the Identity /Account Razor components.
            app.MapAdditionalIdentityEndpoints();

            app.Run();
        }

        /// <summary>
        /// Seeds a single admin user so the FreeA11yChecker scanner can authenticate
        /// against the InMemory database without any manual registration step.
        /// Email: admin@example.com  /  Password: Admin1234!
        /// </summary>
        private static async Task SeedTestUserAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            if (await userManager.FindByEmailAsync(SeedEmail) == null) {
                var user = new ApplicationUser {
                    UserName = SeedEmail,
                    Email = SeedEmail,
                    EmailConfirmed = true,
                };
                await userManager.CreateAsync(user, SeedPassword);
            }
        }
    }
}
