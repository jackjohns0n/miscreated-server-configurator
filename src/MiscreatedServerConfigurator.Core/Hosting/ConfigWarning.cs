namespace MiscreatedServerConfigurator.Core.Hosting;

/// <summary>
/// A non-blocking notice about the settings that will be written.
/// </summary>
public sealed class ConfigWarning
{
    /// <summary>
    /// Creates a warning.
    /// </summary>
    /// <param name="id">Stable id for tests. Not shown as the main message.</param>
    /// <param name="message">Text for the user. Must not contain a password or API key.</param>
    public ConfigWarning(string id, string message)
    {
        Id = id;
        Message = message;
    }

    /// <summary>Stable id for tests.</summary>
    public string Id { get; }

    /// <summary>Text for the user. Does not include secret values.</summary>
    public string Message { get; }
}

/// <summary>
/// Checks the two rules the reference template states in prose: set the RCON password and the server name,
/// and do not use the same secret for <c>http_password</c> and <c>sv_password</c>.
/// </summary>
public static class ConfigWarnings
{
    /// <summary>
    /// Evaluates <paramref name="session"/>.
    /// </summary>
    /// <param name="session">The file being edited.</param>
    /// <returns>Zero or more warnings. Saving is still allowed.</returns>
    public static IReadOnlyList<ConfigWarning> Evaluate(HostingConfigSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        var warnings = new List<ConfigWarning>();
        var http = session.GetRequired("http_password");
        var serverName = session.GetRequired("sv_servername");
        var joinPassword = session.GetRequired("sv_password");

        if (!http.Enabled || Unquote(http.Value).Length == 0)
        {
            warnings.Add(new ConfigWarning(
                "http_password.missing",
                "http_password is not set. The reference template says an RCON password is required for a uniquely named server."));
        }
        else if (string.Equals(Unquote(http.Value), "CHANGE_ME", StringComparison.Ordinal))
        {
            warnings.Add(new ConfigWarning(
                "http_password.placeholder",
                "http_password is still the placeholder CHANGE_ME. Replace it with a strong password before you start the server."));
        }

        if (!serverName.Enabled || Unquote(serverName.Value).Length == 0)
        {
            warnings.Add(new ConfigWarning(
                "sv_servername.missing",
                "sv_servername is empty. The reference template says a server name is required."));
        }

        if (http.Enabled && joinPassword.Enabled && SameSecret(http.Value, joinPassword.Value))
        {
            warnings.Add(new ConfigWarning(
                "passwords.match",
                "http_password and sv_password are the same. The reference template says not to set them equal."));
        }

        return warnings;
    }

    private static bool SameSecret(string left, string right)
    {
        if (left.Length == 0 || right.Length == 0)
        {
            return false;
        }

        if (string.Equals(left, right, StringComparison.Ordinal))
        {
            return Unquote(left).Length > 0;
        }

        var unquotedLeft = Unquote(left);
        var unquotedRight = Unquote(right);
        return unquotedLeft.Length > 0 && string.Equals(unquotedLeft, unquotedRight, StringComparison.Ordinal);
    }

    private static string Unquote(string value)
    {
        var trimmed = value.Trim();
        if (trimmed.Length >= 2 && trimmed[0] == '"' && trimmed[^1] == '"')
        {
            return trimmed[1..^1].Trim();
        }

        return trimmed;
    }
}
