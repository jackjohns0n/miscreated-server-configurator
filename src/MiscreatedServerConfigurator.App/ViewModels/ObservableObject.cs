using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MiscreatedServerConfigurator.App.ViewModels;

/// <summary>
/// Base type for view models. Raises <see cref="PropertyChanged"/> when a bound value changes.
/// </summary>
public abstract class ObservableObject : INotifyPropertyChanged
{
    /// <summary>Raised when a property value changes.</summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Sets <paramref name="field"/> and raises <see cref="PropertyChanged"/> when the value differs.
    /// </summary>
    /// <typeparam name="T">Property type.</typeparam>
    /// <param name="field">Backing field.</param>
    /// <param name="value">New value.</param>
    /// <param name="propertyName">Property name. Filled in by the compiler.</param>
    /// <returns><see langword="true"/> when the value changed.</returns>
    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        Raise(propertyName);
        return true;
    }

    /// <summary>
    /// Raises <see cref="PropertyChanged"/> for <paramref name="propertyName"/>.
    /// </summary>
    /// <param name="propertyName">Property name. Filled in by the compiler when omitted.</param>
    protected void Raise([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
