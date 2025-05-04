using System.Text.Json;
using Azure;
using Azure.AI.Language.Conversations;
using Azure.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VoiceAssistant.Domain;

namespace VoiceAssistant.Infrastructure;

/// <summary>
/// Intent parser that uses Azure Conversational Language Understanding.
/// </summary>
public class AzureLanguageIntentParser : IIntentParser
{
    private readonly ConversationAnalysisClient _client;
    private readonly AzureLanguageOptions _options;
    private readonly IIntentParser _fallbackParser;
    private readonly ILogger<AzureLanguageIntentParser> _logger;

    public AzureLanguageIntentParser(
        IOptions<AzureOptions> azureOptions,
        IIntentParser fallbackParser,
        ILogger<AzureLanguageIntentParser> logger)
    {
        _options = azureOptions.Value.Language;
        _fallbackParser = fallbackParser;
        _logger = logger;
        
        if (string.IsNullOrEmpty(_options.Endpoint) || string.IsNullOrEmpty(_options.ApiKey))
        {
            _logger.LogWarning("Azure Language options not configured. Will use fallback parser.");
            _client = null!;
            return;
        }
        
        try
        {
            _client = new ConversationAnalysisClient(
                new Uri(_options.Endpoint),
                new AzureKeyCredential(_options.ApiKey));
            
            _logger.LogInformation("Azure Language client initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Azure Language client");
            _client = null!;
        }
    }

    /// <inheritdoc />
    public async Task<IntentResult> ParseAsync(string transcript)
    {
        // If client is not initialized or options are not set, use fallback parser
        if (_client == null || string.IsNullOrEmpty(_options.ProjectName) || string.IsNullOrEmpty(_options.DeploymentName))
        {
            _logger.LogWarning("Azure client not initialized or configuration missing. Using fallback parser.");
            return await _fallbackParser.ParseAsync(transcript);
        }
        
        if (string.IsNullOrWhiteSpace(transcript))
        {
            return new IntentResult { IntentName = "Unknown" };
        }
        
        try
        {
            _logger.LogInformation("Sending request to Azure Language service: {Transcript}", transcript);
            
            // Create request data
            var requestData = new
            {
                analysisInput = new
                {
                    conversationItem = new
                    {
                        text = transcript,
                        id = "1",
                        participantId = "user"
                    }
                },
                parameters = new
                {
                    projectName = _options.ProjectName,
                    deploymentName = _options.DeploymentName,
                    verbose = true,
                    stringIndexType = "Utf16CodeUnit"
                },
                kind = "Conversation"
            };
            
            // Convert to JSON
            string requestJson = JsonSerializer.Serialize(requestData);
            
            // Send to Azure
            Response response = await _client.AnalyzeConversationAsync(
                RequestContent.Create(requestJson));
            
            // Get result
            string resultJson = response.Content.ToString();
            _logger.LogDebug("Azure Language response: {Response}", resultJson);
            
            // Parse result
            var responseData = JsonDocument.Parse(resultJson).RootElement;
            
            // Create result
            var result = new IntentResult();
            
            // Get top intent
            if (responseData.TryGetProperty("result", out var resultElement) &&
                resultElement.TryGetProperty("prediction", out var predictionElement))
            {
                // Get top intent
                if (predictionElement.TryGetProperty("topIntent", out var topIntentElement))
                {
                    result.IntentName = topIntentElement.GetString() ?? "Unknown";
                    _logger.LogInformation("Azure recognized intent: {Intent}", result.IntentName);
                    
                    // Intent confidence
                    if (predictionElement.TryGetProperty("intents", out var intentsArray))
                    {
                        foreach (var intentElement in intentsArray.EnumerateArray())
                        {
                            if (intentElement.TryGetProperty("category", out var categoryElement) &&
                                categoryElement.GetString() == result.IntentName &&
                                intentElement.TryGetProperty("confidenceScore", out var confidenceElement))
                            {
                                double confidence = confidenceElement.GetDouble();
                                if (confidence < 0.6) // Configurable threshold
                                {
                                    _logger.LogWarning("Intent confidence too low ({Confidence}), marking as unknown", confidence);
                                    result.IntentName = "Unknown";
                                }
                                break;
                            }
                        }
                    }
                }
                
                // Get entities
                if (predictionElement.TryGetProperty("entities", out var entitiesArray))
                {
                    // First pass: collect all entities by category
                    var entitiesByCategory = new Dictionary<string, List<string>>();

                    foreach (var entityElement in entitiesArray.EnumerateArray())
                    {
                        if (entityElement.TryGetProperty("category", out var categoryElement) &&
                            entityElement.TryGetProperty("text", out var textElement))
                        {
                            string category = categoryElement.GetString() ?? string.Empty;
                            string text = textElement.GetString() ?? string.Empty;
                            
                            // Format entity names to match expected parameter names
                            string parameterName = FormatEntityName(category);
                            
                            if (!string.IsNullOrEmpty(category) && !string.IsNullOrEmpty(text))
                            {
                                if (!entitiesByCategory.ContainsKey(parameterName))
                                {
                                    entitiesByCategory[parameterName] = new List<string>();
                                }
                                entitiesByCategory[parameterName].Add(text);
                                _logger.LogInformation("Found entity: {Category} = {Text}", parameterName, text);
                            }
                        }
                    }

                    // Second pass: handle special cases and add to result
                    foreach (var kvp in entitiesByCategory)
                    {
                        string parameterName = kvp.Key;
                        List<string> values = kvp.Value;

                        // Special case for FolderName - we want to prioritize the actual folder name
                        // and not include the word "folder" itself
                        if (parameterName == "FolderName" && values.Count > 1)
                        {
                            // Filter out the word "folder" if it exists
                            var filteredValues = values.Where(v => !v.Equals("folder", StringComparison.OrdinalIgnoreCase)).ToList();
                            if (filteredValues.Any())
                            {
                                result.Parameters[parameterName] = filteredValues[0];
                                _logger.LogInformation("Extracted entity: {Category} = {Text}", parameterName, filteredValues[0]);
                            }
                        }
                        else
                        {
                            // For other entities or when there's only one value, use the first one
                            result.Parameters[parameterName] = values[0];
                            _logger.LogInformation("Extracted entity: {Category} = {Text}", parameterName, values[0]);
                        }
                    }
                }
            }
            
            // If we couldn't parse the result or get an intent, try the fallback parser
            if (result.IntentName == "Unknown")
            {
                _logger.LogInformation("Azure didn't recognize intent, trying fallback parser");
                return await _fallbackParser.ParseAsync(transcript);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing intent with Azure Language service");
            
            // Use fallback parser if Azure fails
            _logger.LogInformation("Using fallback parser due to Azure error");
            return await _fallbackParser.ParseAsync(transcript);
        }
    }
    
    /// <summary>
    /// Formats entity names to match expected parameter names.
    /// E.g., "Folder.Name" or "folder_name" -> "FolderName"
    /// </summary>
    private string FormatEntityName(string entityName)
    {
        // Handle specific entity translations
        switch (entityName.ToLowerInvariant())
        {
            case "folder.name":
            case "folder_name":
            case "foldername":
                return "FolderName";
            default:
                // For generic formats, try to convert to PascalCase
                string[] parts = entityName.Split(new[] { '.', '_', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                string result = string.Join("", parts.Select(p => 
                    p.Length > 0 
                        ? char.ToUpperInvariant(p[0]) + (p.Length > 1 ? p.Substring(1) : "") 
                        : ""));
                return result;
        }
    }
} 