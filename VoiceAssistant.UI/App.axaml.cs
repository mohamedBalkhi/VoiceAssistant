using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using System;
using VoiceAssistant.Application;
using VoiceAssistant.UI.ViewModels;

namespace VoiceAssistant.UI;

public partial class App : Avalonia.Application
{
    public static IServiceProvider? ServiceProvider { get; set; }
    private VoiceAssistantService? _voiceAssistantService;
    
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Create main window with injected view model
            if (ServiceProvider != null)
            {
                var mainWindow = new MainWindow();
                var viewModel = ServiceProvider.GetRequiredService<MainWindowViewModel>();
                
                // Store the voice assistant service for proper disposal
                _voiceAssistantService = ServiceProvider.GetRequiredService<VoiceAssistantService>();
                
                mainWindow.DataContext = viewModel;
                desktop.MainWindow = mainWindow;
                
                // Handle application exit
                desktop.Exit += OnApplicationExit;
            }
            else
            {
                // Fallback if service provider is not available
                desktop.MainWindow = new MainWindow();
            }
        }

        base.OnFrameworkInitializationCompleted();
    }
    
    private void OnApplicationExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        // Clean up voice assistant resources if initialized
        if (_voiceAssistantService != null)
        {
            _voiceAssistantService.StopListening();
        }
        
        // Dispose the service provider if needed
        if (ServiceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}