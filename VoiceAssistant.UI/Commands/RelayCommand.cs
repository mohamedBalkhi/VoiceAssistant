using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Threading;

namespace VoiceAssistant.UI.Commands;

public class RelayCommand : ICommand
{
    private readonly Func<object?, Task>? _asyncExecute;
    private readonly Action<object?>? _execute;
    private readonly Predicate<object?>? _canExecute;
    private bool _isExecuting;

    public event EventHandler? CanExecuteChanged;

    public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public RelayCommand(Func<object?, Task> asyncExecute, Predicate<object?>? canExecute = null)
    {
        _asyncExecute = asyncExecute ?? throw new ArgumentNullException(nameof(asyncExecute));
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter)
    {
        try
        {
            bool canExecute = !_isExecuting && (_canExecute == null || _canExecute(parameter));
            // Add some debugging
            Console.WriteLine($"RelayCommand.CanExecute returning: {canExecute}, isExecuting: {_isExecuting}");
            return canExecute;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in CanExecute: {ex.Message}");
            return false;
        }
    }

    public void Execute(object? parameter)
    {
        if (!CanExecute(parameter))
            return;

        _isExecuting = true;
        RaiseCanExecuteChanged();

        try
        {
            if (_asyncExecute != null)
            {
                // Async execution
                _asyncExecute(parameter).ContinueWith(task =>
                {
                    _isExecuting = false;
                    RaiseCanExecuteChanged();
                    
                    // Propagate exceptions from the task if any
                    if (task.IsFaulted && task.Exception != null)
                    {
                        // Log or handle the exception
                        Console.WriteLine($"Command execution error: {task.Exception.InnerException?.Message}");
                    }
                });
            }
            else
            {
                // Synchronous execution
                _execute!(parameter);
                _isExecuting = false;
                RaiseCanExecuteChanged();
            }
        }
        catch (Exception ex)
        {
            // Log the error
            Console.WriteLine($"Command execution error: {ex.Message}");
            
            _isExecuting = false;
            RaiseCanExecuteChanged();
            throw;
        }
    }

    public void RaiseCanExecuteChanged()
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            Dispatcher.UIThread.Post(() => CanExecuteChanged?.Invoke(this, EventArgs.Empty));
        }
    }
}

public static class CommandManager
{
    public static void InvalidateRequerySuggested()
    {
        // This is a stub for WPF's CommandManager
        // In Avalonia, commands need to raise CanExecuteChanged themselves
    }
} 