using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

using MiscreatedServerConfigurator.App.ViewModels;

namespace MiscreatedServerConfigurator.App.Views;

/// <summary>
/// Editor for one CVAR: a live-value checkbox, the description, and either a text field or the Workshop id list.
/// </summary>
public partial class CvarEditorRow : UserControl
{
    private CvarRowViewModel? _row;
    private bool _syncing;

    /// <summary>Creates the row.</summary>
    public CvarEditorRow()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (_row is not null)
        {
            _row.PropertyChanged -= OnRowPropertyChanged;
        }

        _row = DataContext as CvarRowViewModel;
        if (_row is not null)
        {
            _row.PropertyChanged += OnRowPropertyChanged;
            PushPassword(_row.Value);
        }
    }

    private void OnRowPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CvarRowViewModel.Value) && _row is not null)
        {
            PushPassword(_row.Value);
        }
    }

    private void PushPassword(string value)
    {
        if (_syncing || SecretBox.Password == value)
        {
            return;
        }

        _syncing = true;
        SecretBox.Password = value;
        _syncing = false;
    }

    private void Reveal_Click(object sender, RoutedEventArgs e)
    {
        if (_row is not null)
        {
            _row.RevealSecret = !_row.RevealSecret;
        }
    }

    private void SecretBox_OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (_syncing || _row is null || _row.Value == SecretBox.Password)
        {
            return;
        }

        _syncing = true;
        _row.Value = SecretBox.Password;
        _syncing = false;
    }
}
