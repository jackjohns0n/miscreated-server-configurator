using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace MiscreatedServerConfigurator.Core.Setup;

/// <summary>One piece of SteamCMD console text after carriage returns are turned into updates.</summary>
/// <param name="ReplaceProgress">True when this should replace the current progress line instead of adding a new one.</param>
/// <param name="Text">The text to show.</param>
/// <param name="Percent">Download percentage when the line carries one, otherwise null.</param>
public readonly record struct SteamCmdPiece(bool ReplaceProgress, string Text, double? Percent);

/// <summary>Recognizes SteamCMD progress and install-state lines from the console or from content_log.txt.</summary>
public static partial class SteamCmdProgress
{
    /// <summary>Reads a percentage from <c>progress: 12.34</c> or <c>progress 12.34</c>.</summary>
    /// <param name="text">One console or content-log line.</param>
    /// <param name="percent">The percentage from 0 through 100 when the method returns true.</param>
    /// <returns><see langword="true"/> when a percentage was found.</returns>
    public static bool TryGetPercent(string? text, out double percent)
    {
        percent = 0;
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        var match = PercentPattern().Match(text);
        if (!match.Success)
        {
            return false;
        }

        if (!double.TryParse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out percent))
        {
            return false;
        }

        return percent is >= 0 and <= 100;
    }

    /// <summary>True for content-log lines that say the install changed state or failed.</summary>
    /// <param name="text">One content-log line.</param>
    /// <returns><see langword="true"/> when the line is worth showing even without a percentage.</returns>
    public static bool IsStateLine(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        return text.Contains("state changed", StringComparison.OrdinalIgnoreCase)
            || text.Contains("Fully Installed", StringComparison.OrdinalIgnoreCase)
            || text.Contains("Error!", StringComparison.OrdinalIgnoreCase);
    }

    [GeneratedRegex(@"\bprogress:?\s+(\d+(?:\.\d+)?)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex PercentPattern();
}

/// <summary>
/// Turns a raw SteamCMD stream into lines. A bare carriage return replaces the progress line.
/// Carriage return plus line feed is a normal new line.
/// </summary>
public sealed class SteamCmdConsoleReader
{
    private readonly StringBuilder _pending = new();
    private bool _carriageReturn;

    /// <summary>Feeds the next chunk from stdout or stderr.</summary>
    /// <param name="chunk">Characters just read. May end in the middle of a line.</param>
    /// <returns>The completed pieces, in order.</returns>
    public IReadOnlyList<SteamCmdPiece> Push(string? chunk)
    {
        var pieces = new List<SteamCmdPiece>();
        if (string.IsNullOrEmpty(chunk))
        {
            return pieces;
        }

        foreach (var ch in chunk)
        {
            Accept(ch, pieces);
        }

        return pieces;
    }

    /// <summary>Emits any text still held after the process exits.</summary>
    /// <returns>The final piece, or an empty list.</returns>
    public IReadOnlyList<SteamCmdPiece> Flush()
    {
        var pieces = new List<SteamCmdPiece>();
        if (_carriageReturn)
        {
            _carriageReturn = false;
            Emit(pieces, replace: true);
        }
        else if (_pending.Length > 0)
        {
            Emit(pieces, replace: false);
        }

        return pieces;
    }

    private void Accept(char ch, List<SteamCmdPiece> pieces)
    {
        if (_carriageReturn)
        {
            _carriageReturn = false;
            if (ch == '\n')
            {
                Emit(pieces, replace: false);
                return;
            }

            Emit(pieces, replace: true);
        }

        if (ch == '\r')
        {
            _carriageReturn = true;
            return;
        }

        if (ch == '\n')
        {
            Emit(pieces, replace: false);
            return;
        }

        _pending.Append(ch);
    }

    private void Emit(List<SteamCmdPiece> pieces, bool replace)
    {
        var text = _pending.ToString().Trim();
        _pending.Clear();
        if (text.Length == 0)
        {
            return;
        }

        double? percent = SteamCmdProgress.TryGetPercent(text, out var value) ? value : null;
        pieces.Add(new SteamCmdPiece(replace, text, percent));
    }
}
