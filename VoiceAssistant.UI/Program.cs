using Avalonia;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using VoiceAssistant.Application;
using VoiceAssistant.Domain;
using VoiceAssistant.Infrastructure;
using VoiceAssistant.UI.ViewModels;
using DotNetEnv;

namespace VoiceAssistant.UI;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    // Avalonia configuration, don't remove; also used by visual designer.
    private static AppBuilder BuildAvaloniaApp()
    {
        Console.WriteLine("Starting application configuration...");
        // Set up configuration
        var currentDirectory = Directory.GetCurrentDirectory();
        Console.WriteLine($"Current Directory: {currentDirectory}");
        
        // Load .env file
        Console.WriteLine("Loading environment variables from .env file...");
        bool envLoaded = false;
        if (File.Exists(Path.Combine(currentDirectory, ".env")))
        {
            Env.Load(options: LoadOptions.TraversePath());
            envLoaded = true;
            Console.WriteLine(".env file loaded successfully");
        }
        else
        {
            Console.WriteLine("WARNING: .env file not found");
        }
        
        
        // Print environment variables loaded (safely) // DEBUG
        #if DEBUG
        if (envLoaded)
        {
            Console.WriteLine("Environment variables from .env:");
            var envVars = Environment.GetEnvironmentVariables();
            var azureKeys = new[] {
                "AZURE_LANGUAGE_ENDPOINT", 
                "AZURE_LANGUAGE_PROJECT_NAME", 
                "AZURE_LANGUAGE_DEPLOYMENT_NAME",
                "AZURE_SPEECH_REGION"
            };
            
            foreach (var key in azureKeys)
            {
                if (envVars.Contains(key))
                {
                    Console.WriteLine($"  {key}: {envVars[key]}");
                }
            }
            
            // Check API keys exist (without printing them)
            var apiKeys = new[] {"AZURE_LANGUAGE_API_KEY", "AZURE_SPEECH_API_KEY"};
            foreach (var key in apiKeys)
            {
                var value = Environment.GetEnvironmentVariable(key);
                Console.WriteLine($"  {key} exists: {!string.IsNullOrEmpty(value)}");
            }
        }
        #endif
        
        // Set up dependency injection
        var services = new ServiceCollection();
        
        // Add logging
        services.AddLogging(configure => configure.AddConsole());
        
        // Add application services
        services.AddVoiceAssistantApplication();
        services.AddCommandHandlers();
        
        // Add platform-specific services
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            services.AddWindowsPlatformServices();
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            services.AddMacOSPlatformServices();
        }
        
        // Check if we should use real Azure services or mock services
        bool useMockServices = false; // Set to true to use mock services until valid API keys are configured
        
        // Validate Azure API keys if we want to use real services
        if (!useMockServices)
        {
            if (envLoaded)
            {
                useMockServices = !HasValidAzureEnvConfiguration();
            }
            else
            {
                // If .env file is not loaded, use mock services
                useMockServices = true;
                Console.WriteLine("WARNING: .env file not found. Using mock services.");
            }
            
            if (useMockServices)
            {
                Console.WriteLine("WARNING: Azure configuration is invalid or incomplete. Using mock services instead.");
            }
        }
        
        if (useMockServices)
        {
            // Register mock services for development
            services.AddSingleton<IRecognizer>(new MockRecognizer());
            services.AddSingleton<IIntentParser>(new MockIntentParser());
            services.AddSingleton<ITextToSpeechSynthesizer>(new MockTextToSpeechSynthesizer());
        }
        else if (envLoaded)
        {
            // Add Azure services using environment variables
            string speechKey = Environment.GetEnvironmentVariable("AZURE_SPEECH_API_KEY") ?? "";
            string speechRegion = Environment.GetEnvironmentVariable("AZURE_SPEECH_REGION") ?? "westeurope";
            
            services.AddAzureSpeechServices(speechKey, speechRegion);
            
            // Add Azure language services
            string languageEndpoint = Environment.GetEnvironmentVariable("AZURE_LANGUAGE_ENDPOINT") ?? "";
            string languageKey = Environment.GetEnvironmentVariable("AZURE_LANGUAGE_API_KEY") ?? "";
            string projectName = Environment.GetEnvironmentVariable("AZURE_LANGUAGE_PROJECT_NAME") ?? "";
            string deploymentName = Environment.GetEnvironmentVariable("AZURE_LANGUAGE_DEPLOYMENT_NAME") ?? "";
            
            services.AddSingleton<RuleBasedIntentParser>();  // Keep original for compatibility
            services.AddSingleton<EnhancedRuleBasedIntentParser>();
            
            // Register Azure parser with fallback to enhanced rule-based parser
            services.AddSingleton<IIntentParser>(provider => 
            {
                var options = new AzureOptions
                {
                    Language = new AzureLanguageOptions
                    {
                        Endpoint = languageEndpoint,
                        ApiKey = languageKey,
                        ProjectName = projectName,
                        DeploymentName = deploymentName
                    }
                };
                
                var azureParser = new AzureLanguageIntentParser(
                    Microsoft.Extensions.Options.Options.Create(options),
                    provider.GetRequiredService<EnhancedRuleBasedIntentParser>(),
                    provider.GetRequiredService<ILogger<AzureLanguageIntentParser>>());
                    
                return azureParser;
            });
        }
        
        // Add view models
        services.AddTransient<MainWindowViewModel>();
        
        // Build the service provider
        var serviceProvider = services.BuildServiceProvider();
        
        // Store the service provider for use in the application
        App.ServiceProvider = serviceProvider;
        
        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
    }
    
    /// <summary>
    /// Validates that Azure configuration is complete and valid from environment variables.
    /// </summary>
    private static bool HasValidAzureEnvConfiguration()
    {
        // Check Speech configuration
        var speechKey = Environment.GetEnvironmentVariable("AZURE_SPEECH_API_KEY");
        Console.WriteLine($"Speech Key (from env): {(speechKey != null ? "[EXISTS]" : "[MISSING]")}");
        var speechRegion = Environment.GetEnvironmentVariable("AZURE_SPEECH_REGION");
        Console.WriteLine($"Speech Region (from env): {speechRegion}");
        bool validSpeech = !string.IsNullOrWhiteSpace(speechKey) && 
                          !string.IsNullOrWhiteSpace(speechRegion) &&
                          speechKey.Length > 10;
        Console.WriteLine($"Valid Speech (from env): {validSpeech}");  
                          
        // Check Language configuration
        var languageEndpoint = Environment.GetEnvironmentVariable("AZURE_LANGUAGE_ENDPOINT");
        Console.WriteLine($"Language Endpoint (from env): {languageEndpoint}");
        var languageKey = Environment.GetEnvironmentVariable("AZURE_LANGUAGE_API_KEY");
        Console.WriteLine($"Language Key (from env): {(languageKey != null ? "[EXISTS]" : "[MISSING]")}");
        var projectName = Environment.GetEnvironmentVariable("AZURE_LANGUAGE_PROJECT_NAME");
        Console.WriteLine($"Project Name (from env): {projectName}");
        var deploymentName = Environment.GetEnvironmentVariable("AZURE_LANGUAGE_DEPLOYMENT_NAME");
        Console.WriteLine($"Deployment Name (from env): {deploymentName}");
        bool validLanguage = !string.IsNullOrWhiteSpace(languageEndpoint) &&
                            !string.IsNullOrWhiteSpace(languageKey) &&
                            !string.IsNullOrWhiteSpace(projectName) &&
                            !string.IsNullOrWhiteSpace(deploymentName) &&
                            languageKey.Length > 10;
        Console.WriteLine($"Valid Language (from env): {validLanguage}");

        return validSpeech && validLanguage;
    }
}

// Mock implementations for development
public class MockRecognizer : IRecognizer
{
    private static readonly string[] Responses =
    [
        "take a screenshot",
        "what time is it",
        "open folder documents",
        "hey voicy",
        "hey voicy take a screenshot",
        "hey voicy what time is it"
    ];
    
    private int _counter;
    
    public Task<string> RecognizeAsync(CancellationToken token)
    {
        // Simulate recognition delay
        Thread.Sleep(1000);
        
        // Get the next response in a circular pattern
        var response = Responses[_counter % Responses.Length];
        _counter++;
        
        return Task.FromResult(response);
    }
}

public class MockIntentParser : IIntentParser
{
    public Task<IntentResult> ParseAsync(string transcript)
    {
        var intent = new IntentResult();
        
        // Remove wake word prefix if present
        string text = transcript.ToLower();
        if (text.StartsWith("hey voicy"))
        {
            text = text.Substring("hey voicy".Length).Trim();
        }
        
        // Determine intent based on the text
        if (text.Contains("screenshot"))
        {
            intent.IntentName = "TakeScreenshot";
        }
        else if (text.Contains("time"))
        {
            intent.IntentName = "GetTime";
        }
        else if (text.Contains("open folder"))
        {
            intent.IntentName = "OpenFolder";
            
            // Extract folder name if present
            int folderIndex = text.IndexOf("open folder", StringComparison.Ordinal) + "open folder".Length;
            if (folderIndex < text.Length)
            {
                string folderName = text.Substring(folderIndex).Trim();
                intent.Parameters["FolderName"] = folderName;
            }
        }
        else
        {
            intent.IntentName = "Unknown";
        }
        
        return Task.FromResult(intent);
    }
}

public class MockTextToSpeechSynthesizer : ITextToSpeechSynthesizer
{
    public Task SpeakAsync(string textToSpeak, CancellationToken token = default)
    {
        Console.WriteLine($"[TTS] {textToSpeak}");
        return Task.CompletedTask;
    }
}
