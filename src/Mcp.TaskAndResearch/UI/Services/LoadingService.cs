namespace Mcp.TaskAndResearch.UI.Services;

/// <summary>
/// Service to manage global loading state across components.
/// </summary>
public sealed class LoadingService
{
    private int _loadingCount;
    
    public bool IsLoading => _loadingCount > 0;
    public string? Message { get; private set; }
    
    public event Action? OnChange;

    public IDisposable Show(string? message = null)
    {
        Interlocked.Increment(ref _loadingCount);
        Message = message;
        NotifyStateChanged();
        return new LoadingScope(this);
    }

    private void Hide()
    {
        var count = Interlocked.Decrement(ref _loadingCount);
        if (count <= 0)
        {
            _loadingCount = 0;
            Message = null;
        }
        NotifyStateChanged();
    }

    private void NotifyStateChanged() => OnChange?.Invoke();

    private sealed class LoadingScope(LoadingService service) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            service.Hide();
        }
    }
}
