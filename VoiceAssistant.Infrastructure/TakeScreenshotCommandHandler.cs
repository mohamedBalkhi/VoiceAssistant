using System.Diagnostics;
using System.Runtime.InteropServices;
using VoiceAssistant.Domain;
using VoiceAssistant.Domain.Platform;

namespace VoiceAssistant.Infrastructure;

// TODO: Make file path configurable
// TODO: Add proper error handling and logging
public class TakeScreenshotCommandHandler : ICommandHandler
{
    private readonly IScreenshotProvider _screenshotProvider;

    public TakeScreenshotCommandHandler(IScreenshotProvider screenshotProvider)
    {
        _screenshotProvider = screenshotProvider;
    }

    public string HandledIntentName => "TakeScreenshot";

    public async Task<CommandResult> ExecuteAsync(IntentResult intent, CancellationToken token = default)
    {
        // Let the platform-specific provider handle the screenshot taking
        var result = await _screenshotProvider.CaptureScreenshotAsync(interactive: true, token);

        if (!result.Success)
        {
            return CommandResult.Fail(result.ErrorMessage ?? "Failed to take screenshot.");
        }

        if (string.IsNullOrEmpty(result.FilePath))
        {
            // This likely means the user cancelled, but it was still "successful" from the operation perspective
            return CommandResult.Ok(result.ErrorMessage ?? "Screenshot cancelled.");
        }

        return CommandResult.Ok($"Screenshot taken and saved to {Path.GetDirectoryName(result.FilePath)}.");
    }
} 