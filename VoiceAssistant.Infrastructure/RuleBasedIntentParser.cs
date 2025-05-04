using VoiceAssistant.Domain;
using System.Text.RegularExpressions;

namespace VoiceAssistant.Infrastructure;

// Simple rule-based parser, assumes exact phrases for now.
// TODO: Define intent names as constants or enums in Domain layer
public class RuleBasedIntentParser : IIntentParser
{
    // Regex to capture folder name for the "Open folder" command
    private static readonly Regex OpenFolderRegex = new Regex(@"^open folder (.*)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public Task<IntentResult> ParseAsync(string transcript)
    {
        var intentResult = new IntentResult();

        if (string.IsNullOrWhiteSpace(transcript))
        {
            intentResult.IntentName = "Unknown"; // Or handle as no input
            return Task.FromResult(intentResult);
        }

        var lowerTranscript = transcript.Trim().ToLowerInvariant();

        // Check for specific commands
        if (lowerTranscript == "take a screenshot")
        {
            intentResult.IntentName = "TakeScreenshot";
        }
        else if (lowerTranscript == "what time is it")
        {
            intentResult.IntentName = "GetTime";
        }
        else
        {
            // Check for commands with parameters (e.g., Open Folder)
            var match = OpenFolderRegex.Match(transcript); // Use original transcript for case preservation if needed
            if (match.Success && match.Groups.Count > 1)
            {
                intentResult.IntentName = "OpenFolder";
                intentResult.Parameters["FolderName"] = match.Groups[1].Value.Trim();
            }
            else
            {
                intentResult.IntentName = "Unknown"; // Default if no rules match
            }
        }

        return Task.FromResult(intentResult);
    }
} 