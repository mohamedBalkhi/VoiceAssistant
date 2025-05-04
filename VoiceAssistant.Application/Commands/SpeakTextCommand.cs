using VoiceAssistant.Domain;

namespace VoiceAssistant.Application.Commands;

/// <summary>
/// Command to speak text using text-to-speech.
/// </summary>
public class SpeakTextCommand
{
    public string TextToSpeak { get; }
    public CancellationToken CancellationToken { get; }

    public SpeakTextCommand(string textToSpeak, CancellationToken cancellationToken = default)
    {
        TextToSpeak = textToSpeak;
        CancellationToken = cancellationToken;
    }
}

/// <summary>
/// Handler for SpeakTextCommand.
/// </summary>
public class SpeakTextCommandHandler
{
    private readonly ITextToSpeechSynthesizer _tts;

    public SpeakTextCommandHandler(ITextToSpeechSynthesizer tts)
    {
        _tts = tts;
    }

    public async Task HandleAsync(SpeakTextCommand command)
    {
        await _tts.SpeakAsync(command.TextToSpeak, command.CancellationToken);
    }
} 