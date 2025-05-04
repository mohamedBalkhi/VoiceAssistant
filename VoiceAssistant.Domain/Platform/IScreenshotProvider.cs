namespace VoiceAssistant.Domain.Platform;

/// <summary>
/// Provides platform-specific screenshot functionality.
/// </summary>
public interface IScreenshotProvider
{
    /// <summary>
    /// Takes a screenshot and saves it to the specified location.
    /// </summary>
    /// <param name="interactive">Whether to allow the user to select a region (if supported)</param>
    /// <param name="token">Cancellation token</param>
    /// <returns>A result with the path to the saved screenshot, or error details.</returns>
    Task<(bool Success, string FilePath, string? ErrorMessage)> CaptureScreenshotAsync(
        bool interactive = true,
        CancellationToken token = default);
} 