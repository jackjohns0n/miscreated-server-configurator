namespace MiscreatedServerConfigurator.Core.Catalog;

/// <summary>
/// Decides whether a value from the reference template is a real example the user can apply.
/// </summary>
public static class TemplateValuePolicy
{
    /// <summary>
    /// True when <paramref name="templateValue"/> is a value worth offering as "use reference value".
    /// Null, blank, and a quote pair with nothing inside (<c>""</c>) are not. The empty quotes on
    /// <c>sv_servername</c> only show that the name is written between quotes.
    /// </summary>
    /// <param name="templateValue">The value recorded from the template, or null when the template has no line.</param>
    /// <returns>True when the editor should show the value and the button.</returns>
    public static bool IsReference(string? templateValue)
    {
        if (string.IsNullOrWhiteSpace(templateValue))
        {
            return false;
        }

        var trimmed = templateValue.Trim();
        return !(trimmed.Length >= 2
            && trimmed[0] == '"'
            && trimmed[^1] == '"'
            && string.IsNullOrWhiteSpace(trimmed[1..^1]));
    }
}
