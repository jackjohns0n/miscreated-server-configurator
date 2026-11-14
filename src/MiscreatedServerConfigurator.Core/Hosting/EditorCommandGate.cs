namespace MiscreatedServerConfigurator.Core.Hosting;

/// <summary>
/// Decides whether the editor's window commands may run.
/// </summary>
/// <remarks>
/// WPF queries <c>CanExecute</c> while the window XAML is still loading, before the window can finish
/// assigning its fields. A command that assumes those fields exist will take the process down before
/// the window is shown. A missing model means construction is still in progress: the command stays disabled.
/// </remarks>
public static class EditorCommandGate
{
    /// <summary>
    /// Save may run only when the editor model exists and that model has something to write.
    /// </summary>
    /// <param name="modelIsReady">True when the window has created its editor model.</param>
    /// <param name="modelCanSave">The model's own save flag. Ignored when <paramref name="modelIsReady"/> is false.</param>
    /// <returns>True when Save should be enabled.</returns>
    public static bool CanSave(bool modelIsReady, bool modelCanSave)
    {
        return modelIsReady && modelCanSave;
    }
}
