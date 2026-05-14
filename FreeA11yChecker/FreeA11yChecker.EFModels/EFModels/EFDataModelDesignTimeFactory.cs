using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FreeA11yChecker.EFModels.EFModels;

// Design-time DbContext factory for `dotnet ef migrations add` / `dotnet ef database update`.
// At runtime the app constructs EFDataModel via DI with options from appsettings.json — this
// factory is ONLY consulted by EF tooling. Connection string mirrors appsettings.json's
// "AppData" SQL Server trusted-connection entry.
public class EFDataModelDesignTimeFactory : IDesignTimeDbContextFactory<EFDataModel>
{
    public EFDataModel CreateDbContext(string[] args)
    {
        DbContextOptionsBuilder<EFDataModel> options = new DbContextOptionsBuilder<EFDataModel>();
        options.UseSqlServer(
            "Data Source=(local);Initial Catalog=FreeA11yChecker;Integrated Security=True;TrustServerCertificate=True;MultipleActiveResultSets=True;");
        return new EFDataModel(options.Options);
    }
}
