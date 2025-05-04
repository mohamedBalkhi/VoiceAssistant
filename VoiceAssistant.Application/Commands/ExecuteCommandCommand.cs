using VoiceAssistant.Domain;

namespace VoiceAssistant.Application.Commands;

/// <summary>
/// Command to execute a specific command based on an intent.
/// </summary>
public class ExecuteCommandCommand
{
    public IntentResult Intent { get; }
    public CancellationToken CancellationToken { get; }

    public ExecuteCommandCommand(IntentResult intent, CancellationToken cancellationToken = default)
    {
        Intent = intent;
        CancellationToken = cancellationToken;
    }
}

/// <summary>
/// Handler for ExecuteCommandCommand.
/// </summary>
public class ExecuteCommandCommandHandler
{
    private readonly IDictionary<string, ICommandHandler> _commandHandlers;

    public ExecuteCommandCommandHandler(IEnumerable<ICommandHandler> commandHandlers)
    {
        _commandHandlers = commandHandlers.ToDictionary(h => h.HandledIntentName);
    }

    public async Task<(bool HandlerFound, CommandResult? Result)> HandleAsync(ExecuteCommandCommand command)
    {
        if (!_commandHandlers.TryGetValue(command.Intent.IntentName, out var handler))
        {
            return (false, null);
        }

        var result = await handler.ExecuteAsync(command.Intent, command.CancellationToken);
        return (true, result);
    }
} 