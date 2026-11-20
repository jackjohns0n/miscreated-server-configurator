namespace MiscreatedServerConfigurator.Core.Setup;

/// <summary>Reads one registry value. The app supplies the Windows registry. Tests supply a dictionary.</summary>
public interface IRegistryView
{
    /// <summary>Reads a value under HKLM, or null when the key or value is absent.</summary>
    /// <param name="keyPath">Key path without the hive, using backslashes.</param>
    /// <param name="valueName">Value name.</param>
    /// <returns>The raw value, or null.</returns>
    object? GetValue(string keyPath, string valueName);
}

/// <summary>One Visual C++ x64 runtime the dedicated server is installed beside.</summary>
/// <param name="Id">Short id used in logs.</param>
/// <param name="DisplayName">Name shown to the user.</param>
/// <param name="InstallerUrl">Microsoft's official redistributable URL.</param>
/// <param name="SilentArguments">Arguments for a silent install. 2012 and 2013 do not accept <c>/install</c>.</param>
/// <param name="RegistryKeys">HKLM keys whose <c>Installed</c> DWORD is 1 when this runtime is present.</param>
public readonly record struct VcRuntime(
    string Id,
    string DisplayName,
    string InstallerUrl,
    string SilentArguments,
    IReadOnlyList<string> RegistryKeys);

/// <summary>
/// Visual C++ 2012, 2013, and 2015-or-later x64 runtimes.
/// Detection is the <c>VC\Runtimes\x64</c> Installed value. A runtime installed without that key is reported as missing.
/// winget is not used.
/// </summary>
public static class VcRuntimes
{
    /// <summary>The three x64 runtimes, in install order.</summary>
    public static IReadOnlyList<VcRuntime> Required { get; } = new VcRuntime[]
    {
        new(
            "2012-x64",
            "Visual C++ 2012 x64",
            "https://download.microsoft.com/download/1/6/B/16B06F60-3B20-4FF2-B699-5E9B7962F9AE/VSU_4/vcredist_x64.exe",
            "/quiet /norestart",
            new[]
            {
                @"SOFTWARE\Microsoft\VisualStudio\11.0\VC\Runtimes\x64",
                @"SOFTWARE\WOW6432Node\Microsoft\VisualStudio\11.0\VC\Runtimes\x64",
            }),
        new(
            "2013-x64",
            "Visual C++ 2013 x64",
            "https://download.microsoft.com/download/2/E/6/2E61CFA4-993B-4DD4-91DA-3737CD5CD6E3/vcredist_x64.exe",
            "/quiet /norestart",
            new[]
            {
                @"SOFTWARE\Microsoft\VisualStudio\12.0\VC\Runtimes\x64",
                @"SOFTWARE\WOW6432Node\Microsoft\VisualStudio\12.0\VC\Runtimes\x64",
            }),
        new(
            "2015-x64",
            "Visual C++ 2015 or later x64",
            "https://aka.ms/vs/17/release/vc_redist.x64.exe",
            "/install /quiet /norestart",
            new[]
            {
                @"SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\x64",
                @"SOFTWARE\WOW6432Node\Microsoft\VisualStudio\14.0\VC\Runtimes\x64",
            }),
    };

    /// <summary>Runtimes whose Installed value is not 1.</summary>
    /// <param name="registry">The registry to read.</param>
    /// <returns>The missing runtimes, in install order.</returns>
    public static IReadOnlyList<VcRuntime> Missing(IRegistryView registry)
    {
        ArgumentNullException.ThrowIfNull(registry);
        var missing = new List<VcRuntime>();
        foreach (var runtime in Required)
        {
            if (!IsInstalled(registry, runtime))
            {
                missing.Add(runtime);
            }
        }

        return missing;
    }

    /// <summary>True when any of the runtime's registry keys has Installed = 1.</summary>
    /// <param name="registry">The registry to read.</param>
    /// <param name="runtime">The runtime.</param>
    /// <returns><see langword="true"/> when the runtime is installed.</returns>
    public static bool IsInstalled(IRegistryView registry, VcRuntime runtime)
    {
        foreach (var key in runtime.RegistryKeys)
        {
            var value = registry.GetValue(key, "Installed");
            if (value is int number && number == 1)
            {
                return true;
            }

            if (value is long wide && wide == 1)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Explains which official installers will run, and why.</summary>
    /// <param name="missing">The runtimes that are not installed.</param>
    /// <returns>The notice. Empty when nothing is missing.</returns>
    public static string InstallNotice(IReadOnlyList<VcRuntime> missing)
    {
        if (missing.Count == 0)
        {
            return string.Empty;
        }

        var names = string.Join(", ", missing.Select(runtime => runtime.DisplayName));
        return "The dedicated server needs the " + names
            + " runtime. The app will download Microsoft's official installers and run them silently (/quiet /norestart; the 2015 or later installer also uses /install). "
            + "winget is not used. Continue?";
    }
}
