using System.Diagnostics;
using System.Runtime.InteropServices;
using VoiceAssistant.Domain;
using VoiceAssistant.Domain.Platform;

namespace VoiceAssistant.Infrastructure;

// TODO: Add proper error handling and logging
// TODO: Improve folder name handling and validation
public class OpenFolderCommandHandler : ICommandHandler
{
    private readonly IFolderOpener _folderOpener;

    public OpenFolderCommandHandler(IFolderOpener folderOpener)
    {
        _folderOpener = folderOpener;
    }

    public string HandledIntentName => "OpenFolder";

    public async Task<CommandResult> ExecuteAsync(IntentResult intent, CancellationToken token = default)
    {
        // Get the folder name from the intent parameters
        if (!intent.Parameters.TryGetValue("FolderName", out string? folderName) || string.IsNullOrWhiteSpace(folderName))
        {
            return CommandResult.Fail("No folder name specified.");
        }

        // Try to resolve which folder to open based on name
        string? resolvedPath = _folderOpener.ResolveFolderPath(folderName);
        if (resolvedPath == null)
        {
            return CommandResult.Fail($"Couldn't find folder named '{folderName}'.");
        }

        // Use the platform-specific opener to open the folder
        var result = await _folderOpener.OpenFolderAsync(resolvedPath, token);
        if (!result.Success)
        {
            return CommandResult.Fail(result.ErrorMessage ?? $"Failed to open folder '{folderName}'.");
        }

        return CommandResult.Ok($"Opened folder {folderName}.");
    }
} 