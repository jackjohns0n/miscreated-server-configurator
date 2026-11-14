using System.IO;
using System.Text;

namespace MiscreatedServerConfigurator.App;

/// <summary>
/// Reads and writes hosting.cfg as UTF-8 without a BOM.
/// A file that is not valid UTF-8 is rejected instead of being silently rewritten.
/// </summary>
internal static class HostingConfigFile
{
    private static readonly Encoding StrictUtf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    public static string Read(string path)
    {
        var bytes = File.ReadAllBytes(path);
        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
        {
            bytes = bytes[3..];
        }

        return StrictUtf8.GetString(bytes);
    }

    public static void Write(string path, string text)
    {
        var encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        File.WriteAllText(path, text, encoding);
    }
}
