using System.Windows.Input;

namespace MiscreatedServerConfigurator.App.ViewModels;

/// <summary>
/// An <see cref="ICommand"/> that runs a delegate.
/// </summary>
public sealed class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    /// <summary>
    /// Creates a command.
    /// </summary>
    /// <param name="execute">Action to run.</param>
    /// <param name="canExecute">Optional gate. Null means the command can always run.</param>
    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    /// <summary>Raised when WPF asks commands to refresh their enabled state.</summary>
    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    /// <summary>Returns whether the command may run.</summary>
    /// <param name="parameter">Ignored.</param>
    /// <returns><see langword="true"/> when the command may run.</returns>
    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

    /// <summary>Runs the command.</summary>
    /// <param name="parameter">Ignored.</param>
    public void Execute(object? parameter) => _execute();
}

/// <summary>
/// An <see cref="ICommand"/> that runs a delegate with a typed argument.
/// </summary>
/// <typeparam name="T">Argument type.</typeparam>
public sealed class RelayCommand<T> : ICommand
{
    private readonly Action<T?> _execute;

    /// <summary>
    /// Creates a command.
    /// </summary>
    /// <param name="execute">Action to run.</param>
    public RelayCommand(Action<T?> execute)
    {
        _execute = execute;
    }

    /// <summary>Raised when WPF asks commands to refresh their enabled state.</summary>
    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    /// <summary>Returns whether the command may run.</summary>
    /// <param name="parameter">Ignored.</param>
    /// <returns>Always <see langword="true"/>.</returns>
    public bool CanExecute(object? parameter) => true;

    /// <summary>Runs the command.</summary>
    /// <param name="parameter">The argument, cast to <typeparamref name="T"/>.</param>
    public void Execute(object? parameter) => _execute(parameter is T value ? value : default);
}
