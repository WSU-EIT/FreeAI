using Microsoft.EntityFrameworkCore;

// Namespace MUST match the other EFDataModel partial classes
// (FreeA11yChecker.EFModels.EFModels) so all three files merge into ONE class. The
// scaffold template in this codebase originally generated this file in the parent
// namespace, which silently created a second DbContext type and prevented EF Core
// migration tooling from finding the real (DbSet-bearing) context.
namespace FreeA11yChecker.EFModels.EFModels;

public partial class EFDataModel : DbContext
{
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // MySQL, PostgreSQL, and SQLite store uniqueidentifier fields (GUID) as a string in the EFCore provider.
        // So, if this instance is running one of those providers then override the converter for all Guid
        // values to use strings.
        var providerName = this.Database.ProviderName;
        if (!String.IsNullOrEmpty(providerName)) {
            switch (providerName.ToUpper()) {
                case "MICROSOFT.ENTITYFRAMEWORKCORE.SQLSERVER":
                case "MICROSOFT.ENTITYFRAMEWORKCORE.INMEMORY":
                    break;

                case "MYSQL.ENTITYFRAMEWORKCORE":
                case "NPGSQL.ENTITYFRAMEWORKCORE.POSTGRESQL":
                case "MICROSOFT.ENTITYFRAMEWORKCORE.SQLITE":
                    configurationBuilder
                        .Properties<Guid>()
                        .HaveConversion<Microsoft.EntityFrameworkCore.Storage.ValueConversion.GuidToStringConverter>();
                    break;
            }
        }
    }
}