using System.Diagnostics;
using VoiceAssistant.Domain.Platform;

namespace VoiceAssistant.Infrastructure.Platform.MacOS;

public class MacOSScreenshotProvider : IScreenshotProvider
{
    public async Task<(bool Success, string FilePath, string? ErrorMessage)> CaptureScreenshotAsync(
        bool interactive = true,
        CancellationToken token = default)
    {
        // Create a filename with timestamp in the user's Desktop directory
        string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        string fileName = $"Screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png";
        string filePath = Path.Combine(desktopPath, fileName);

        // Prepare the arguments for screencapture
        string arguments = interactive ? $"-i \"{filePath}\"" : $"\"{filePath}\"";

        var processStartInfo = new ProcessStartInfo
        {
            FileName = "/usr/sbin/screencapture",
            Arguments = arguments,
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
                return (false, string.Empty, "Failed to start screencapture process.");
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
                // If in interactive mode, user might have canceled (ESC key)
                if (!File.Exists(filePath))
                {
                    return (true, string.Empty, "Screenshot cancelled by user.");
                }
                
                return (false, string.Empty, $"Failed to take screenshot. Error: {error?.Trim()}");
            }

            if (!File.Exists(filePath))
            {
                // User cancelled in interactive mode
                return (true, string.Empty, "Screenshot cancelled by user.");
            }

            return (true, filePath, null);
        }
        catch (Exception ex)
        {
            return (false, string.Empty, $"An error occurred while taking the screenshot: {ex.Message}");
        }
    }
} 