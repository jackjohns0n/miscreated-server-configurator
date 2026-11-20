using System.IO;
using System.Windows;
using System.Windows.Input;

using Microsoft.Win32;

using MiscreatedServerConfigurator.App.Setup;
using MiscreatedServerConfigurator.App.ViewModels;
using MiscreatedServerConfigurator.Core.Setup;

namespace MiscreatedServerConfigurator.App;

/// <summary>
/// Downloads and prepares a dedicated server. The server itself is started from the generated batch file.
/// </summary>
public partial class ServerSetupWindow : Window
{
    private readonly ServerSetupViewModel _model;

    /// <summary>Creates the window bound to the open editor.</summary>
    /// <param name="editor">The hosting.cfg editor.</param>
    public ServerSetupWindow(MainViewModel editor)
    {
        _model = new ServerSetupViewModel(
            editor,
            new ProcessRunner(),
            new HttpFileDownloader(),
            new WindowsRegistryView(),
            pickFolder: BrowseFolder);
        DataContext = _model;
        InitializeComponent();
        Loaded += (_, _) => _model.OfferLegacySteamCmd();
    }

    private string? BrowseFolder(string title)
    {
        var dialog = new OpenFolderDialog
        {
            Title = title,
            Multiselect = false,
        };
        return dialog.ShowDialog(this) == true ? dialog.FolderName : null;
    }

    private void OutputLog_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (sender is System.Windows.Controls.TextBox box)
        {
            box.ScrollToEnd();
        }
    }

    private void BrowseRoot_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Choose the server root",
            Multiselect = false,
        };
        if (dialog.ShowDialog(this) == true)
        {
            _model.RootPath = dialog.FolderName;
        }
    }

    private void BrowseSteamCmd_Click(object sender, RoutedEventArgs e)
    {
        var folder = BrowseFolder("Choose the SteamCMD folder");
        if (!string.IsNullOrWhiteSpace(folder))
        {
            _model.UsePickedSteamCmd(folder);
        }
    }

    private void LoadServer_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Choose an existing server folder",
            Multiselect = false,
        };
        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        _model.LoadServer(dialog.FolderName, ConfirmReplaceEditor);
    }

    private void KnownServer_DoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is not FrameworkElement { DataContext: SavedServer server })
        {
            return;
        }

        _model.ApplySaved(server);
    }

    private bool ConfirmReplaceEditor(string path)
    {
        if (!_model.EditorHasUnsavedChanges)
        {
            return true;
        }

        var answer = MessageBox.Show(
            this,
            "Replace the hosting.cfg open in the editor with " + Path.GetFileName(path) + "?",
            "Load server",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        return answer == MessageBoxResult.Yes;
    }
}
