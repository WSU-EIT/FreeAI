using Microsoft.EntityFrameworkCore;

namespace FreeGLBA.EFModels;

// NOTE: This class does NOT merge with the application's DbContext. The real
// context is FreeGLBA.EFModels.EFModels.EFDataModel (nested EFModels namespace),
// so this partial declares a separate, unused type and the ConfigureConventions
// override below has never applied. It is left untouched because "fixing" the
// namespace now would change Guid column mappings under existing MySQL/
// PostgreSQL/SQLite databases that were created without the conversion.
// App-level model configuration belongs in EFModels/FreeGLBA.App.EFDataModel.cs.
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