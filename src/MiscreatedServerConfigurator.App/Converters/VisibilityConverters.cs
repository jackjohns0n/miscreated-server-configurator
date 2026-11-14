using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace MiscreatedServerConfigurator.App.Converters;

/// <summary>
/// Shows an element when the bound string has text, and collapses it otherwise.
/// </summary>
public sealed class StringPresenceToVisibilityConverter : IValueConverter
{
    /// <summary>Maps a string to <see cref="Visibility"/>.</summary>
    /// <param name="value">The bound value.</param>
    /// <param name="targetType">Unused.</param>
    /// <param name="parameter">Unused.</param>
    /// <param name="culture">Unused.</param>
    /// <returns><see cref="Visibility.Visible"/> when the string has text.</returns>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return string.IsNullOrWhiteSpace(value as string) ? Visibility.Collapsed : Visibility.Visible;
    }

    /// <summary>Not used.</summary>
    /// <param name="value">Unused.</param>
    /// <param name="targetType">Unused.</param>
    /// <param name="parameter">Unused.</param>
    /// <param name="culture">Unused.</param>
    /// <returns>Never returns.</returns>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
