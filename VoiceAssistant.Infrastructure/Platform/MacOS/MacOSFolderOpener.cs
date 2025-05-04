using System.Diagnostics;
using VoiceAssistant.Domain.Platform;

namespace VoiceAssistant.Infrastructure.Platform.MacOS;

public class MacOSFolderOpener : IFolderOpener
{
    public string? ResolveFolderPath(string folderName)
    {
        // Simple matching for common folders
        return folderName.ToLowerInvariant() switch
        {
            "desktop" => Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
            "documents" => Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "downloads" => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads"),
            "pictures" => Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
            "music" => Environment.GetFolderPath(Environment.SpecialFolder.MyMusic),
            "videos" => Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
            "home" => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            _ => TryFindFolderInCommonLocations(folderName)
        };
    }

    public async Task<(bool Success, string? ErrorMessage)> OpenFolderAsync(string folderPath, CancellationToken token = default)
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = "open",
            Arguments = $"\"{folderPath}\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        try
        {
            using var process = Process.Start(processStartInfo);
            if (process == null)
            {
                return (false, "Failed to start process to open folder.");
            }

            // Register cancellation
            using var registration = token.Register(() => 
            {
                try { process.Kill(); } 
                catch { /* Ignore errors during kill */ }
            });

            string error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync(token);

            if (process.ExitCode != 0 || !string.IsNullOrEmpty(error))
            {
                return (false, $"Failed to open folder. Error: {error?.Trim()}");
            }

            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, $"An error occurred while opening the folder: {ex.Message}");
        }
    }

    // Tries to find a folder with the given name in common locations
    private string? TryFindFolderInCommonLocations(string folderName)
    {
        var commonLocations = new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads"),
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        };

        // Try exact match first
        foreach (var location in commonLocations)
        {
            string potentialPath = Path.Combine(location, folderName);
            if (Directory.Exists(potentialPath))
            {
                return potentialPath;
            }
        }

        // Try case-insensitive match
        foreach (var location in commonLocations)
        {
            if (Directory.Exists(location))
            {
                var matches = Directory.GetDirectories(location)
                    .Where(dir => Path.GetFileName(dir).Equals(folderName, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (matches.Count == 1)
                {
                    return matches[0];
                }
                // If multiple matches, just return the first match
                if (matches.Count > 1)
                {
                    return matches[0];
                }
            }
        }

        // No match found
        return null;
    }
} 