using System.IO;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Input;

using Microsoft.Win32;

using MiscreatedServerConfigurator.App.ViewModels;
using MiscreatedServerConfigurator.Core.Hosting;

namespace MiscreatedServerConfigurator.App;

/// <summary>
/// The hosting.cfg editor window.
/// </summary>
public partial class MainWindow : Window
{
    private readonly MainViewModel? _model;

    /// <summary>Creates the window and loads the reference template.</summary>
    public MainWindow()
    {
        // The Save button queries CanExecute while InitializeComponent is loading the XAML.
        // The model has to exist before that, or the query dereferences null and the process
        // ends before the window is shown.
        _model = new MainViewModel();
        DataContext = _model;
        InitializeComponent();
    }

    private void New_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = true;
    }

    private void New_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        if (_model is null || !ConfirmDiscard())
        {
            return;
        }

        _model.LoadTemplate();
    }

    private void Open_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = true;
    }

    private void Open_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        if (_model is null || !ConfirmDiscard())
        {
            return;
        }

        var dialog = new OpenFileDialog
        {
            Filter = "hosting.cfg (*.cfg)|*.cfg|All files (*.*)|*.*",
            Title = "Open hosting.cfg",
        };
        if (!string.IsNullOrEmpty(_model.FilePath))
        {
            var directory = Path.GetDirectoryName(_model.FilePath);
            if (!string.IsNullOrEmpty(directory))
            {
                dialog.InitialDirectory = directory;
            }
        }

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        try
        {
            _model.LoadFile(HostingConfigFile.Read(dialog.FileName), dialog.FileName);
        }
        catch (DecoderFallbackException)
        {
            MessageBox.Show(
                this,
                "That file is not valid UTF-8, so it was not opened and was not changed.",
                "Open hosting.cfg",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(
                this,
                "Could not read that file.\n\n" + ex.Message,
                "Open hosting.cfg",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private void Save_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = EditorCommandGate.CanSave(_model is not null, _model?.CanSave ?? false);
    }

    private void Save_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        if (_model is null)
        {
            return;
        }

        Save();
    }

    private void SaveAs_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = true;
    }

    private void SaveAs_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        if (_model is null)
        {
            return;
        }

        SaveAs();
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void About_Click(object sender, RoutedEventArgs e)
    {
        var version = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion ?? "0.1.0-dev";
        MessageBox.Show(
            this,
            "Miscreated Server Configurator\n"
            + "Version " + version + "\n\n"
            + "Phase 1 edits a dedicated-server hosting.cfg.\n"
            + "Passwords and API keys are written only into the file you save.\n"
            + "This build does not connect to Steam.\n\n"
            + "The hosting.cfg guide is docs/hosting-cfg.md in the repository.",
            "About",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    /// <summary>
    /// Asks before throwing away edits. Saving is attempted when the user says yes.
    /// </summary>
    /// <returns><see langword="true"/> when it is safe to discard the current document.</returns>
    private bool ConfirmDiscard()
    {
        if (_model is null || !_model.IsDirty)
        {
            return true;
        }

        var choice = MessageBox.Show(
            this,
            "Save changes to this hosting.cfg?",
            "Miscreated Server Configurator",
            MessageBoxButton.YesNoCancel,
            MessageBoxImage.Warning);
        if (choice == MessageBoxResult.Cancel)
        {
            return false;
        }

        if (choice == MessageBoxResult.No)
        {
            return true;
        }

        return Save();
    }

    private bool Save()
    {
        if (_model is null)
        {
            return false;
        }

        if (string.IsNullOrEmpty(_model.FilePath))
        {
            return SaveAs();
        }

        return Write(_model.FilePath);
    }

    private bool SaveAs()
    {
        if (_model is null)
        {
            return false;
        }

        var dialog = new SaveFileDialog
        {
            Filter = "hosting.cfg (*.cfg)|*.cfg|All files (*.*)|*.*",
            FileName = string.IsNullOrEmpty(_model.FilePath) ? "hosting.cfg" : Path.GetFileName(_model.FilePath),
            Title = "Save hosting.cfg",
            OverwritePrompt = true,
        };
        if (dialog.ShowDialog(this) != true)
        {
            return false;
        }

        return Write(dialog.FileName);
    }

    private bool Write(string path)
    {
        if (_model is null)
        {
            return false;
        }

        var text = _model.BuildText();
        try
        {
            HostingConfigFile.Write(path, text);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(
                this,
                "Could not save the file.\n\n" + ex.Message,
                "Save hosting.cfg",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return false;
        }

        _model.NoteSaved(path, text);
        return true;
    }

    private void OnClosing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_model is not null && !ConfirmDiscard())
        {
            e.Cancel = true;
        }
    }
}
