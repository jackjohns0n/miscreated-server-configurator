using System.Reflection;

using MiscreatedServerConfigurator.Core.Hosting;

namespace MiscreatedServerConfigurator.Core.Tests;

public sealed class StartupFailureTests
{
    [Theory]
    [InlineData(false, false, false)]
    [InlineData(false, true, false)]
    [InlineData(true, false, false)]
    [InlineData(true, true, true)]
    public void Save_stays_disabled_until_the_editor_model_exists(bool modelIsReady, bool modelCanSave, bool expected)
    {
        Assert.Equal(expected, EditorCommandGate.CanSave(modelIsReady, modelCanSave));
    }

    [Fact]
    public void Startup_save_query_with_no_model_does_not_require_a_document()
    {
        // The window asks CanExecute while InitializeComponent is still running.
        // That query has no model yet. It must answer "no" without touching a document.
        Assert.False(EditorCommandGate.CanSave(modelIsReady: false, modelCanSave: true));
    }

    [Fact]
    public void Error_log_records_the_exception_and_not_a_stack_in_the_dialog_text()
    {
        var failure = new TargetInvocationException(
            "Exception has been thrown by the target of an invocation.",
            new NullReferenceException("editor model was not ready"));
        var when = new DateTimeOffset(2026, 11, 14, 3, 38, 0, TimeSpan.Zero);

        var entry = ExceptionLog.FormatEntry(failure, when);
        var dialog = ExceptionLog.DisplayMessage(failure);

        Assert.Contains("2026-11-14T03:38:00", entry, StringComparison.Ordinal);
        Assert.Contains("NullReferenceException", entry, StringComparison.Ordinal);
        Assert.Contains("editor model was not ready", entry, StringComparison.Ordinal);
        Assert.Equal("editor model was not ready", dialog);
        Assert.DoesNotContain("at ", dialog, StringComparison.Ordinal);
    }
}
