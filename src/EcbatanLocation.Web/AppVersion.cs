using System.Reflection;

namespace EcbatanLocation.Web;

/// <summary>
/// Exposes the application version (the release tag, injected at build time via
/// <c>-p:Version=</c>). Resolved once from the assembly's informational version.
/// </summary>
public static class AppVersion
{
    /// <summary>The current version string, e.g. "1.2.3". Falls back to "dev" when unset.</summary>
    public static string Current { get; } = Resolve();

    private static string Resolve()
    {
        var info = typeof(AppVersion).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        if (string.IsNullOrEmpty(info))
            return "dev";

        // SourceLink appends "+<git-commit>" to the informational version; drop it.
        var plus = info.IndexOf('+');
        return plus >= 0 ? info[..plus] : info;
    }
}
