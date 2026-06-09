using Microsoft.JSInterop;

namespace PlanningLocation.Web.Services;

public class ThemeService
{
    private readonly IJSRuntime _js;

    public string CurrentTheme { get; private set; } = "ocean";
    public string CurrentMode { get; private set; } = "dark";

    public event Action? OnChange;

    public static readonly (string Id, string Label, string Preview)[] Themes =
    [
        ("ocean", "Océan", "#6ea8ff"),
        ("forest", "Forêt", "#34d399"),
        ("sunset", "Crépuscule", "#f97316"),
        ("amethyst", "Améthyste", "#a78bfa"),
        ("ruby", "Rubis", "#f43f5e"),
    ];

    public ThemeService(IJSRuntime js) => _js = js;

    public async Task InitializeAsync()
    {
        try
        {
            var result = await _js.InvokeAsync<ThemeState>("themeInterop.get");
            CurrentTheme = result.Theme ?? "ocean";
            CurrentMode = result.Mode ?? "dark";
        }
        catch
        {
            // SSR or prerendering — use defaults
        }
    }

    public async Task SetThemeAsync(string theme)
    {
        CurrentTheme = theme;
        await ApplyAsync();
    }

    public async Task SetModeAsync(string mode)
    {
        CurrentMode = mode;
        await ApplyAsync();
    }

    public async Task ToggleModeAsync()
    {
        CurrentMode = CurrentMode == "dark" ? "light" : "dark";
        await ApplyAsync();
    }

    private async Task ApplyAsync()
    {
        try
        {
            await _js.InvokeVoidAsync("themeInterop.apply", CurrentTheme, CurrentMode);
        }
        catch
        {
            // SSR fallback
        }
        OnChange?.Invoke();
    }

    private record ThemeState(string? Theme, string? Mode);
}
