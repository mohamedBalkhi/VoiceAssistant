using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Styling;
using Avalonia.Threading;
using Microsoft.Extensions.Logging;
using VoiceAssistant.Application;
using VoiceAssistant.Domain;
using VoiceAssistant.UI.Commands;

namespace VoiceAssistant.UI.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private readonly VoiceAssistantService _voiceAssistantService;
    private readonly ILogger<MainWindowViewModel> _logger;
    private bool _isListening;
    private string _textCommand = string.Empty;
    private string _statusMessage = "Ready";
    private bool _isBusy;
    private bool _useWakeWord = false;
    private bool _isDarkMode = false;

    public event EventHandler<bool>? MinimizeToTrayRequested;

    public MainWindowViewModel(VoiceAssistantService voiceAssistantService, ILogger<MainWindowViewModel> logger)
    {
        _voiceAssistantService = voiceAssistantService;
        _logger = logger;
        
        // Initialize collections
        ActivityLog = new ObservableCollection<string>();
        
        // Sync initial state with service
        _isListening = _voiceAssistantService.IsListening;
        
        // Initialize commands - IMPORTANT: Change condition to not check IsBusy for StopListeningCommand
        StartListeningCommand = new RelayCommand(async _ => await StartListeningAsync(), _ => !IsListening && !IsBusy);
        StopListeningCommand = new RelayCommand(_ => 
        {
            StopListening();
            // Force command state update
            (StartListeningCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (StopListeningCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }, _ => IsListening);  // Remove the IsBusy check here - allow stop even when busy
        SendTextCommandCommand = new RelayCommand(async _ => await SendTextCommandAsync(), _ => !string.IsNullOrWhiteSpace(TextCommand) && !IsBusy);
        MinimizeToTrayCommand = new RelayCommand(_ => MinimizeToTray());
        
        // Add timer to check synchronization with service every second
        var timer = new System.Timers.Timer(1000);
        timer.Elapsed += (sender, args) => 
        {
            Dispatcher.UIThread.Post(() => 
            {
                // Check if view model state matches service state
                var serviceListening = _voiceAssistantService.IsListening;
                if (_isListening != serviceListening)
                {
                    _logger.LogWarning($"State mismatch detected: ViewModel.IsListening={_isListening}, Service.IsListening={serviceListening}. Correcting...");
                    IsListening = serviceListening;
                    
                    // Force command state update
                    (StartListeningCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    (StopListeningCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            });
        };
        timer.Start();
        
        // Subscribe to service events
        _voiceAssistantService.ActivityLogged += OnActivityLogged;
        _voiceAssistantService.ListeningStateChanged += OnListeningStateChanged;
        _voiceAssistantService.IntentRecognized += OnIntentRecognized;
        _voiceAssistantService.CommandExecuted += OnCommandExecuted;
        
        // Configure wake word settings
        _voiceAssistantService.UseWakeWord = _useWakeWord;
        
        // Log initial state
        _logger.LogInformation($"ViewModel initialized. IsListening={_isListening}");
    }

    // Properties
    public bool IsListening
    {
        get => _isListening;
        private set
        {
            if (_isListening != value)
            {
                _isListening = value;
                OnPropertyChanged();
                
                // Explicitly raise CanExecuteChanged for both commands
                (StartListeningCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (StopListeningCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }
    }
    
    public string TextCommand
    {
        get => _textCommand;
        set
        {
            if (_textCommand != value)
            {
                _textCommand = value;
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }
    
    public string StatusMessage
    {
        get => _statusMessage;
        private set
        {
            if (_statusMessage != value)
            {
                _statusMessage = value;
                OnPropertyChanged();
            }
        }
    }
    
    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (_isBusy != value)
            {
                _isBusy = value;
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }
    
    public bool UseWakeWord
    {
        get => _useWakeWord;
        set
        {
            if (_useWakeWord != value)
            {
                _useWakeWord = value;
                OnPropertyChanged();
                
                // Update service configuration
                _voiceAssistantService.UseWakeWord = value;
                
                // Log the change
                string message = value ? "Wake word mode enabled. Using 'Hey Voicy' as trigger." : "Wake word mode disabled.";
                StatusMessage = message;
                _logger.LogInformation(message);
            }
        }
    }

    public bool IsDarkMode
    {
        get => _isDarkMode;
        set
        {
            if (_isDarkMode != value)
            {
                _isDarkMode = value;
                OnPropertyChanged();

                // Update application theme
                Avalonia.Application.Current!.RequestedThemeVariant = value ? ThemeVariant.Dark : ThemeVariant.Light;
            }
        }
    }
    
    public ObservableCollection<string> ActivityLog { get; }
    
    // Commands
    public ICommand StartListeningCommand { get; }
    public ICommand StopListeningCommand { get; }
    public ICommand SendTextCommandCommand { get; }
    public ICommand MinimizeToTrayCommand { get; }
    
    // Methods
    private async Task StartListeningAsync()
    {
        try
        {
            IsBusy = true;
            StatusMessage = "Starting voice recognition...";
            
            _logger.LogInformation("StartListeningAsync: Beginning to start listening");
            
            // Run the listening on a background thread to not block the UI
            await Task.Run(async () => 
            {
                try 
                {
                    await _voiceAssistantService.StartListeningAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in background task starting listening");
                    throw;
                }
            });
            
            _logger.LogInformation("StartListeningAsync: Finished background task");
            
            // IMPORTANT: Set IsBusy to false before updating UI state
            IsBusy = false;
            
            // Manually check and update state after starting
            Dispatcher.UIThread.Post(() =>
            {
                try
                {
                    // Make sure UI state matches service state
                    _logger.LogInformation($"StartListeningAsync UI update - Service.IsListening={_voiceAssistantService.IsListening}, ViewModel.IsListening={IsListening}, IsBusy={IsBusy}");
                    
                    IsListening = _voiceAssistantService.IsListening;
                    
                    _logger.LogInformation($"After update: IsListening={IsListening}, IsBusy={IsBusy}");
                    
                    // Check command states
                    var stopCommand = StopListeningCommand as RelayCommand;
                    _logger.LogInformation($"StopCommand CanExecute before update: {stopCommand?.CanExecute(null)}");
                    
                    // Force command state update
                    (StartListeningCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    stopCommand?.RaiseCanExecuteChanged();
                    
                    _logger.LogInformation($"StopCommand CanExecute after update: {stopCommand?.CanExecute(null)}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating UI state after start listening");
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting listening");
            StatusMessage = $"Error: {ex.Message}";
            IsBusy = false;
        }
       
    }
    
    private void StopListening()
    {
        try
        {
            StatusMessage = "Stopping voice recognition...";
            _voiceAssistantService.StopListening();
            
            // Make sure the UI updates even if the service doesn't trigger the event
            IsListening = _voiceAssistantService.IsListening;
            StatusMessage = IsListening ? "Error stopping listening" : "Ready";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping listening");
            StatusMessage = $"Error: {ex.Message}";
        }
    }
    
    private async Task SendTextCommandAsync()
    {
        if (string.IsNullOrWhiteSpace(TextCommand))
            return;
            
        try
        {
            IsBusy = true;
            StatusMessage = "Processing command...";
            
            string command = TextCommand;
            TextCommand = string.Empty; // Clear the input field
            
            await _voiceAssistantService.ProcessTextCommandAsync(command);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending text command");
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    private void MinimizeToTray()
    {
        StatusMessage = "Application minimized to system tray";
        MinimizeToTrayRequested?.Invoke(this, true);
    }
    
    // Event handlers
    private void OnActivityLogged(object? sender, string message)
    {
        // Since this event could be raised from a background thread,
        // we need to dispatch to the UI thread
        Dispatcher.UIThread.Post(() =>
        {
            ActivityLog.Insert(0, $"[{DateTime.Now:HH:mm:ss}] {message}");
            
            // Keep log size manageable
            while (ActivityLog.Count > 100)
            {
                ActivityLog.RemoveAt(ActivityLog.Count - 1);
            }
        });
    }
    
    private void OnListeningStateChanged(object? sender, bool isListening)
    {
        _logger.LogInformation($"OnListeningStateChanged received: {isListening}");
        
        // Ensure this runs on the UI thread
        Dispatcher.UIThread.Post(() =>
        {
            try
            {
                // Log before state change
                _logger.LogInformation($"Before state change - Current IsListening: {_isListening}, Changing to: {isListening}");
                
                // Update properties
                IsListening = isListening;
                
                // Log after state change
                _logger.LogInformation($"After state change - IsListening now: {_isListening}");
                
                // Update status message
                StatusMessage = isListening ? 
                    UseWakeWord ? "Listening for 'Hey Voicy'..." : "Listening..." 
                    : "Ready";
                    
                // Force command evaluation
                var stopCommand = StopListeningCommand as RelayCommand;
                var startCommand = StartListeningCommand as RelayCommand;
                
                // Log command state
                _logger.LogInformation($"StopCommand CanExecute: {stopCommand?.CanExecute(null)}, " +
                                      $"StartCommand CanExecute: {startCommand?.CanExecute(null)}");
                
                // Force update
                stopCommand?.RaiseCanExecuteChanged();
                startCommand?.RaiseCanExecuteChanged();
                
                // Log command state again after update
                _logger.LogInformation($"After raise - StopCommand CanExecute: {stopCommand?.CanExecute(null)}, " +
                                      $"StartCommand CanExecute: {startCommand?.CanExecute(null)}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OnListeningStateChanged");
            }
        });
    }
    
    private void OnIntentRecognized(object? sender, IntentResult intent)
    {
        Dispatcher.UIThread.Post(() =>
        {
            StatusMessage = $"Recognized intent: {intent.IntentName}";
        });
    }
    
    private void OnCommandExecuted(object? sender, CommandResult result)
    {
        Dispatcher.UIThread.Post(() =>
        {
            StatusMessage = result.Success 
                ? "Command executed successfully" 
                : $"Command failed: {result.FailureReason}";
        });
    }
} 