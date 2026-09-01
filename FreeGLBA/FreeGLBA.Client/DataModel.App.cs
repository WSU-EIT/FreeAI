namespace FreeGLBA.Client;

public partial class BlazorDataModel
{
    private List<string> _MyValues = new List<string>();

    /// <summary>
    /// Raised when a GLBA SignalR update arrives (new access events, source-system
    /// changes). Pages subscribe to refresh their data in real time and must
    /// unsubscribe in Dispose.
    /// </summary>
    public event Action<DataObjects.SignalRUpdate>? OnGlbaUpdate;

    /// <summary>Notify subscribed pages that GLBA data changed.</summary>
    public void NotifyGlbaUpdate(DataObjects.SignalRUpdate update)
    {
        OnGlbaUpdate?.Invoke(update);
    }

    private bool HaveDeletedRecordsApp {
        get {
            bool output = false;

            // Check your app-specific deleted records here.
            //if (DeletedRecordCounts.MyValue > 0 ) {
            //    output = true;
            //}

            return output;
        }
    }

    public bool MyCustomDataModelMethod()
    {
        return true;
    }

    /// <summary>
    /// An example of implementing a custom property in your data model.
    /// </summary>
    public List<string> MyValues {
        get {
            return _MyValues;
        }

        set {
            if (!ObjectsAreEqual(_MyValues, value)) {
                _MyValues = value;
                _ModelUpdated = DateTime.UtcNow;
                NotifyDataChanged();
            }
        }
    }
}
