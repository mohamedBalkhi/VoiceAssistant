namespace VoiceAssistant.Domain;

public interface IRecognizer
{
    Task<string> RecognizeAsync(CancellationToken token);
} 