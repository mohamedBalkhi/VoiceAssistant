namespace VoiceAssistant.Domain.Platform;

/// <summary>
/// Provides platform-specific folder opening functionality.
/// </summary>
public interface IFolderOpener
{
    /// <summary>
    /// Attempts to find a folder by name in common locations.
    /// </summary>
    /// <param name="folderName">The name of the folder to find</param>
    /// <returns>The resolved path if found, null otherwise</returns>
    string? ResolveFolderPath(string folderName);
    
    /// <summary>
    /// Opens a folder at the specified path using the platform's native method.
    /// </summary>
    /// <param name="folderPath">The path to the folder to open</param>
    /// <param name="token">Cancellation token</param>
    /// <returns>Success or failure with error message</returns>
    Task<(bool Success, string? ErrorMessage)> OpenFolderAsync(string folderPath, CancellationToken token = default);
} 