// Stub replacements for DataAccess.FileStorage.cs (Files module removed).
// Keep method signatures referenced by utilities so existing code compiles.
namespace FreeBlazorExample;

public partial class DataAccess
{
    public Task<DataObjects.FileStorage> GetFileStorage(Guid FileId, DataObjects.User? CurrentUser = null)
    {
        return Task.FromResult(new DataObjects.FileStorage());
    }

    public Task<DataObjects.BooleanResponse> DeleteFileStorage(Guid FileId, DataObjects.User? CurrentUser = null, bool DeleteImmediately = false)
    {
        return Task.FromResult(new DataObjects.BooleanResponse { Result = true });
    }
}
