using VoiceAssistant.Domain;

namespace VoiceAssistant.Application.Commands;

/// <summary>
/// Command to parse recognized text into an intent.
/// </summary>
public class ParseIntentCommand
{
    public string Text { get; }

    public ParseIntentCommand(string text)
    {
        Text = text;
    }
}

/// <summary>
/// Handler for ParseIntentCommand.
/// </summary>
public class ParseIntentCommandHandler
{
    private readonly IIntentParser _intentParser;

    public ParseIntentCommandHandler(IIntentParser intentParser)
    {
        _intentParser = intentParser;
    }

    public async Task<IntentResult> HandleAsync(ParseIntentCommand command)
    {
        return await _intentParser.ParseAsync(command.Text);
    }
} 