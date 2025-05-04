namespace VoiceAssistant.Domain;

public interface IIntentParser
{
    Task<IntentResult> ParseAsync(string transcript);
} 