#if WINDOWS
using System.Diagnostics;
using VoiceAssistant.Domain.Platform;

namespace VoiceAssistant.Infrastructure.Platform.Windows;

public class WindowsFolderOpener : IFolderOpener
{
    public string? ResolveFolderPath(string folderName)
    {
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
            FileName = "explorer.exe",
            Arguments = $"\"{folderPath}\"",
            UseShellExecute = true
        };

        try
        {
            using var process = Process.Start(processStartInfo);
            if (process == null)
            {
                return (false, "Failed to start process to open folder.");
            }

            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, $"An error occurred while opening the folder: {ex.Message}");
        }
    }

    private string? TryFindFolderInCommonLocations(string folderName)
    {
        var commonLocations = new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads"),
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        };

        foreach (var location in commonLocations)
        {
            string potentialPath = Path.Combine(location, folderName);
            if (Directory.Exists(potentialPath))
            {
                return potentialPath;
            }
        }

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
                if (matches.Count > 1)
                {
                    return matches[0];
                }
            }
        }

        return null;
    }
}
#endif
