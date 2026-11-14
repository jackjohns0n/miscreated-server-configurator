using System.Reflection;

namespace MiscreatedServerConfigurator.Core.Hosting;

/// <summary>
/// The reference hosting.cfg embedded in this library.
/// </summary>
/// <remarks>
/// The file is the 13 November 2026 template (27,142 bytes). Its only live settings are
/// <c>http_password=CHANGE_ME</c> and an empty <c>sv_servername</c>. <c>CHANGE_ME</c> is a placeholder,
/// not a credential, and the library does not contain any other password or account name.
/// </remarks>
public static class ServerTemplate
{
    /// <summary>Manifest name of the embedded template.</summary>
    public const string ResourceName = "MiscreatedServerConfigurator.Core.Resources.hosting.cfg";

    /// <summary>
    /// Reads the embedded reference template.
    /// </summary>
    /// <returns>The template text, without a trailing newline, matching the file on disk.</returns>
    /// <exception cref="InvalidOperationException">The template resource is missing from the assembly.</exception>
    public static string LoadText()
    {
        var assembly = typeof(ServerTemplate).Assembly;
        using var stream = assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{ResourceName}' was not found.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
