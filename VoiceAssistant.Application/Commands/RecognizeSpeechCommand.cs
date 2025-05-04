using VoiceAssistant.Domain;

namespace VoiceAssistant.Application.Commands;

/// <summary>
/// Command to recognize speech from the microphone.
/// </summary>
public class RecognizeSpeechCommand
{
    public CancellationToken CancellationToken { get; }

    public RecognizeSpeechCommand(CancellationToken cancellationToken = default)
    {
        CancellationToken = cancellationToken;
    }
}

/// <summary>
/// Handler for RecognizeSpeechCommand.
/// </summary>
public class RecognizeSpeechCommandHandler
{
    private readonly IRecognizer _recognizer;

    public RecognizeSpeechCommandHandler(IRecognizer recognizer)
    {
        _recognizer = recognizer;
    }

    public async Task<string> HandleAsync(RecognizeSpeechCommand command)
    {
        return await _recognizer.RecognizeAsync(command.CancellationToken);
    }
} 