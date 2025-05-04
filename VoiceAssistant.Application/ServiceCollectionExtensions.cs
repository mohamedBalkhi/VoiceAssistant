using Microsoft.Extensions.DependencyInjection;
using VoiceAssistant.Application.Commands;
using VoiceAssistant.Domain;
using VoiceAssistant.Infrastructure;

namespace VoiceAssistant.Application;

/// <summary>
/// Extension methods for registering application services with the DI container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers core application services.
    /// </summary>
    public static IServiceCollection AddVoiceAssistantApplication(this IServiceCollection services)
    {
        // Register command handlers
        services.AddTransient<RecognizeSpeechCommandHandler>();
        services.AddTransient<ParseIntentCommandHandler>();
        services.AddTransient<ExecuteCommandCommandHandler>();
        services.AddTransient<SpeakTextCommandHandler>();
        
        // Register the main service
        services.AddSingleton<VoiceAssistantService>();
        
        return services;
    }

    /// <summary>
    /// Registers the handlers for different commands.
    /// This is separate from AddVoiceAssistantApplication to allow for registering
    /// only those handlers that make sense for a particular platform.
    /// </summary>
    public static IServiceCollection AddCommandHandlers(this IServiceCollection services)
    {
        // Register command handlers
        services.AddTransient<ICommandHandler, TakeScreenshotCommandHandler>();
        services.AddTransient<ICommandHandler, OpenFolderCommandHandler>();
        services.AddTransient<ICommandHandler, GetTimeCommandHandler>();
        
        return services;
    }
} 