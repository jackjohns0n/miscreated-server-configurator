using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

using MiscreatedServerConfigurator.App.Theming;
using MiscreatedServerConfigurator.Core.Hosting;

namespace MiscreatedServerConfigurator.App;

/// <summary>
/// WPF application entry point for the hosting.cfg editor.
/// Unexpected failures are logged and shown in a dialog instead of ending the process with no explanation.
/// </summary>
public partial class App : Application
{
    private int _reporting;

    /// <summary>Creates the application and registers the unhandled-exception handlers before any window opens.</summary>
    public App()
    {
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    }

    /// <summary>Paints the saved theme, then opens the editor. Follow system is the default.</summary>
    /// <param name="e">Startup arguments. Unused.</param>
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ThemeController.Initialize();
        var window = new MainWindow();
        MainWindow = window;
        window.Show();
    }

    /// <summary>Stops the Windows theme listener.</summary>
    /// <param name="e">Exit arguments.</param>
    protected override void OnExit(ExitEventArgs e)
    {
        ThemeController.Shutdown();
        base.OnExit(e);
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        e.Handled = true;
        Report(e.Exception);
        if (Windows.Count == 0)
        {
            Shutdown(1);
        }
    }

    private void OnDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception exception)
        {
            Report(exception);
        }
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        Report(e.Exception);
        e.SetObserved();
    }

    /// <summary>
    /// Writes the exception to the log and shows a short dialog. Re-entrant failures are ignored
    /// so a dialog problem cannot loop.
    /// </summary>
    private void Report(Exception exception)
    {
        if (Interlocked.Exchange(ref _reporting, 1) == 1)
        {
            return;
        }

        try
        {
            var path = TryAppendLog(exception);
            var message = ExceptionLog.DisplayMessage(exception);
            if (path is not null)
            {
                message += Environment.NewLine + Environment.NewLine + "Details were saved to:" + Environment.NewLine + path;
            }

            MessageBox.Show(
                message,
                "Miscreated Server Configurator",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        catch (Exception)
        {
            // The original failure is already in hand. A broken dialog or disk must not replace it.
        }
        finally
        {
            Interlocked.Exchange(ref _reporting, 0);
        }
    }

    /// <summary>
    /// Appends the exception text to the per-user error log. The open hosting.cfg is never written here.
    /// </summary>
    private static string? TryAppendLog(Exception exception)
    {
        try
        {
            var directory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MiscreatedServerConfigurator");
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, "error.log");
            File.AppendAllText(path, ExceptionLog.FormatEntry(exception, DateTimeOffset.Now));
            return path;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }
}
