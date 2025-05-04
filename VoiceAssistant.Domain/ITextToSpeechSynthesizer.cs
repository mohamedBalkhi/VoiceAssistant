namespace VoiceAssistant.Domain;

public interface ITextToSpeechSynthesizer
{
    Task SpeakAsync(string textToSpeak, CancellationToken token = default);
} 