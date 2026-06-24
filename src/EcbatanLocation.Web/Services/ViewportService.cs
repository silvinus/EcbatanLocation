using Microsoft.JSInterop;

namespace EcbatanLocation.Web.Services;

/// <summary>
/// Tracks the client form factor (mobile vs desktop) for the current circuit.
/// Backed by a CSS media query through JS interop so it reacts live to resize/rotation.
/// Scoped to the circuit; initialized once after the first interactive render.
/// </summary>
public sealed class ViewportService : IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private DotNetObjectReference<ViewportService>? _ref;
    private bool _initialized;

    public bool IsMobile { get; private set; }

    public event Action? OnChange;

    public ViewportService(IJSRuntime js) => _js = js;

    public async Task InitializeAsync()
    {
        if (_initialized) return;
        try
        {
            _ref = DotNetObjectReference.Create(this);
            IsMobile = await _js.InvokeAsync<bool>("viewportInterop.register", _ref);
            _initialized = true;
            OnChange?.Invoke();
        }
        catch
        {
            // SSR or prerendering — keep the desktop default until interop is available.
        }
    }

    [JSInvokable]
    public void OnViewportChanged(bool isMobile)
    {
        if (IsMobile == isMobile) return;
        IsMobile = isMobile;
        OnChange?.Invoke();
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_initialized)
                await _js.InvokeVoidAsync("viewportInterop.unregister");
        }
        catch
        {
            // Circuit already torn down — nothing to clean up.
        }
        _ref?.Dispose();
    }
}
