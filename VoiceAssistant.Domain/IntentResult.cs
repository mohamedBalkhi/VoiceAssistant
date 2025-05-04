namespace VoiceAssistant.Domain;

public class IntentResult
{
    public string IntentName { get; set; } = string.Empty;
    public Dictionary<string, string> Parameters { get; set; } = new();

    // Add a constructor or factory method if needed
} 