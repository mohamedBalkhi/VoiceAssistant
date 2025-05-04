using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using VoiceAssistant.Domain;

namespace VoiceAssistant.Infrastructure;

/// <summary>
/// An enhanced rule-based intent parser with better pattern matching capabilities.
/// </summary>
public class EnhancedRuleBasedIntentParser : IIntentParser
{
    private readonly List<IntentPattern> _patterns = new();
    private readonly ILogger<EnhancedRuleBasedIntentParser> _logger;

    public EnhancedRuleBasedIntentParser(ILogger<EnhancedRuleBasedIntentParser> logger)
    {
        _logger = logger;
        
        // Initialize patterns for each intent
        InitializePatterns();
    }

    /// <inheritdoc />
    public Task<IntentResult> ParseAsync(string transcript)
    {
        if (string.IsNullOrWhiteSpace(transcript))
        {
            return Task.FromResult(new IntentResult { IntentName = "Unknown" });
        }

        string normalizedInput = transcript.Trim().ToLowerInvariant();
        _logger.LogDebug("Attempting to match: '{Transcript}'", normalizedInput);

        // Try each pattern in order
        foreach (var pattern in _patterns)
        {
            var match = pattern.Regex.Match(normalizedInput);
            if (match.Success)
            {
                _logger.LogInformation("Matched intent: {Intent} with pattern: {Pattern}", 
                    pattern.IntentName, pattern.Regex);
                
                var intentResult = new IntentResult { IntentName = pattern.IntentName };
                
                // Extract parameters using the provided function
                if (pattern.ParameterExtractor != null)
                {
                    try
                    {
                        var parameters = pattern.ParameterExtractor(match);
                        foreach (var (key, value) in parameters)
                        {
                            intentResult.Parameters[key] = value;
                            _logger.LogDebug("Extracted parameter: {Key} = {Value}", key, value);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error extracting parameters for intent {Intent}", pattern.IntentName);
                    }
                }
                
                return Task.FromResult(intentResult);
            }
        }

        _logger.LogInformation("No intent matched for: '{Transcript}'", normalizedInput);
        return Task.FromResult(new IntentResult { IntentName = "Unknown" });
    }

    /// <summary>
    /// Initialize all the intent patterns for recognition.
    /// </summary>
    private void InitializePatterns()
    {
        // Screenshot Intent
        _patterns.Add(new IntentPattern
        {
            IntentName = "TakeScreenshot",
            Regex = new Regex(@"^(take|capture|get|grab)?\s*(a\s*)?(screen\s*shot|screenshot|screen\scapture).*$", 
                RegexOptions.IgnoreCase | RegexOptions.Compiled)
        });
        
        // Get Time Intent
        _patterns.Add(new IntentPattern
        {
            IntentName = "GetTime",
            Regex = new Regex(@"^(what|tell me|show)?\s*(is|what's|the)?\s*(time|current time|the time)(\sis\sit|\snow)?.*$", 
                RegexOptions.IgnoreCase | RegexOptions.Compiled)
        });
        
        // Open Folder Intent
        _patterns.Add(new IntentPattern
        {
            IntentName = "OpenFolder",
            Regex = new Regex(@"^(open|browse|show|navigate to|go to)\s*(the|my)?\s*(?:folder\s+(.+)|(.+)\s+folder).*$", 
                RegexOptions.IgnoreCase | RegexOptions.Compiled),
            ParameterExtractor = match =>
            {
                var parameters = new Dictionary<string, string>();
                
                // Get the folder name from either group 3 or 4
                string? folderName = match.Groups[3].Success 
                    ? match.Groups[3].Value.Trim() 
                    : match.Groups[4].Success 
                        ? match.Groups[4].Value.Trim() 
                        : null;
                
                if (!string.IsNullOrWhiteSpace(folderName))
                {
                    parameters["FolderName"] = folderName;
                }
                
                return parameters;
            }
        });
        
        _logger.LogInformation("Initialized {Count} intent patterns", _patterns.Count);
    }
    
    /// <summary>
    /// Class to represent an intent pattern for matching.
    /// </summary>
    private class IntentPattern
    {
        /// <summary>
        /// The name of the intent to recognize.
        /// </summary>
        public required string IntentName { get; init; }
        
        /// <summary>
        /// The regex pattern to match this intent.
        /// </summary>
        public required Regex Regex { get; init; }
        
        /// <summary>
        /// Optional function to extract parameters from a match.
        /// </summary>
        public Func<Match, Dictionary<string, string>>? ParameterExtractor { get; init; }
    }
} 