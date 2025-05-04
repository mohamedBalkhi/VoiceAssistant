namespace VoiceAssistant.Infrastructure;

/// <summary>
/// Configuration options for Azure services.
/// </summary>
public class AzureOptions
{
    /// <summary>
    /// Options for Azure Language Understanding service.
    /// </summary>
    public AzureLanguageOptions Language { get; set; } = new();
    
    /// <summary>
    /// Options for Azure Speech services.
    /// </summary>
    public AzureSpeechOptions Speech { get; set; } = new();
}

/// <summary>
/// Configuration options for Azure Language Understanding services.
/// </summary>
public class AzureLanguageOptions
{
    /// <summary>
    /// The endpoint URL for the Azure Language service.
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;
    
    /// <summary>
    /// The API key for the Azure Language service.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;
    
    /// <summary>
    /// The project name in the Azure Language Studio.
    /// </summary>
    public string ProjectName { get; set; } = string.Empty;
    
    /// <summary>
    /// The deployment name in the Azure Language Studio.
    /// </summary>
    public string DeploymentName { get; set; } = string.Empty;
}

/// <summary>
/// Configuration options for Azure Speech services.
/// </summary>
public class AzureSpeechOptions
{
    /// <summary>
    /// The API key for Azure Speech services.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;
    
    /// <summary>
    /// The region for Azure Speech services.
    /// </summary>
    public string Region { get; set; } = string.Empty;
} 