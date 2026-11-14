using System.Globalization;

namespace MiscreatedServerConfigurator.Core.Hosting;

/// <summary>
/// Text for the unexpected-error log. The log is the exception itself, never the open hosting.cfg.
/// </summary>
public static class ExceptionLog
{
    /// <summary>
    /// Builds one log entry.
    /// </summary>
    /// <param name="exception">The failure. The full <see cref="Exception.ToString"/> is included.</param>
    /// <param name="when">When the failure was recorded.</param>
    /// <returns>Text to append to the log file.</returns>
    public static string FormatEntry(Exception exception, DateTimeOffset when)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return when.ToString("o", CultureInfo.InvariantCulture)
            + Environment.NewLine
            + exception
            + Environment.NewLine
            + Environment.NewLine;
    }

    /// <summary>
    /// A short message for a dialog. Walks a <see cref="System.Reflection.TargetInvocationException"/>
    /// to the inner failure, which is what the user needs when a window command throws during startup.
    /// </summary>
    /// <param name="exception">The failure reported to the application.</param>
    /// <returns>One paragraph. Does not include the stack trace.</returns>
    public static string DisplayMessage(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        var current = exception;
        while (current.InnerException is not null && current is System.Reflection.TargetInvocationException)
        {
            current = current.InnerException;
        }

        return string.IsNullOrWhiteSpace(current.Message)
            ? "An unexpected error occurred."
            : current.Message;
    }
}
