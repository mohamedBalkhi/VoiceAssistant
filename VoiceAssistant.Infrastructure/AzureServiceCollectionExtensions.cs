using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VoiceAssistant.Domain;
using VoiceAssistant.Infrastructure.Platform.MacOS;
#if WINDOWS
using VoiceAssistant.Infrastructure.Platform.Windows;
#endif

namespace VoiceAssistant.Infrastructure;

/// <summary>
/// Extensions for registering Azure services with the DI container.
/// </summary>
public static class AzureServiceCollectionExtensions
{
    /// <summary>
    /// Registers Azure-specific services.
    /// </summary>
    public static IServiceCollection AddAzureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Add configurations
        var azureSection = configuration.GetSection("Azure");
        
        // Log the configuration values
        var logger = services.BuildServiceProvider().GetService<ILogger<AzureOptions>>();
        logger?.LogInformation("Language Endpoint: {Endpoint}", azureSection["Language:Endpoint"]);
        logger?.LogInformation("Language API Key: {ApiKey}", 
            azureSection["Language:ApiKey"]?.Substring(0, 5) + "..." ?? "not set");
        logger?.LogInformation("Speech Region: {Region}", azureSection["Speech:Region"]);
        
        services.Configure<AzureOptions>(options => {
            options.Language.Endpoint = azureSection["Language:Endpoint"] ?? "";
            options.Language.ApiKey = azureSection["Language:ApiKey"] ?? "";
            options.Language.ProjectName = azureSection["Language:ProjectName"] ?? "";
            options.Language.DeploymentName = azureSection["Language:DeploymentName"] ?? "";
            options.Speech.ApiKey = azureSection["Speech:ApiKey"] ?? "";
            options.Speech.Region = azureSection["Speech:Region"] ?? "";
        });
        
        // Register intent parsers in order of preference
        services.AddSingleton<RuleBasedIntentParser>();  // Keep original for compatibility
        services.AddSingleton<EnhancedRuleBasedIntentParser>();
        
        // Register Azure parser with fallback to enhanced rule-based parser
        services.AddSingleton<IIntentParser>(provider => 
        {
            var azureParser = new AzureLanguageIntentParser(
                provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<AzureOptions>>(),
                provider.GetRequiredService<EnhancedRuleBasedIntentParser>(),
                provider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<AzureLanguageIntentParser>>());
                
            return azureParser;
        });
        
        return services;
    }
    
    /// <summary>
    /// Registers Azure Speech services for the voice assistant.
    /// </summary>
    public static IServiceCollection AddAzureSpeechServices(
        this IServiceCollection services, 
        string speechKey, 
        string speechRegion)
    {
        // Register the Azure speech recognizer
        services.AddSingleton<IRecognizer>(provider => 
            new AzureSpeechRecognizer(speechKey, speechRegion));
        
        // Register the Azure text-to-speech service
        services.AddSingleton<ITextToSpeechSynthesizer>(provider => 
            new AzureTextToSpeechSynthesizer(speechKey, speechRegion));
        
        return services;
    }
    
    /// <summary>
    /// Registers macOS-specific platform services.
    /// </summary>
    public static IServiceCollection AddMacOSPlatformServices(this IServiceCollection services)
    {
        // Register macOS-specific services
        services.AddSingleton<Domain.Platform.IScreenshotProvider, MacOSScreenshotProvider>();
        services.AddSingleton<Domain.Platform.IFolderOpener, MacOSFolderOpener>();

        return services;
    }

    /// <summary>
    /// Registers Windows-specific platform services.
    /// </summary>
#if WINDOWS
    public static IServiceCollection AddWindowsPlatformServices(this IServiceCollection services)
    {
        services.AddSingleton<Domain.Platform.IScreenshotProvider, WindowsScreenshotProvider>();
        services.AddSingleton<Domain.Platform.IFolderOpener, WindowsFolderOpener>();

        return services;
    }
#else
    public static IServiceCollection AddWindowsPlatformServices(this IServiceCollection services) => services;
#endif
}


