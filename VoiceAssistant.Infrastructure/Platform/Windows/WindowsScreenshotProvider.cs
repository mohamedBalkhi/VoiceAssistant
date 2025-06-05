#if WINDOWS
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using VoiceAssistant.Domain.Platform;

namespace VoiceAssistant.Infrastructure.Platform.Windows;

public class WindowsScreenshotProvider : IScreenshotProvider
{
    public Task<(bool Success, string FilePath, string? ErrorMessage)> CaptureScreenshotAsync(bool interactive = true, CancellationToken token = default)
    {
        try
        {
            if (token.IsCancellationRequested)
            {
                return Task.FromResult<(bool, string, string?)>((false, string.Empty, "Operation cancelled."));
            }

            var bounds = SystemInformation.VirtualScreen;
            using var bitmap = new Bitmap(bounds.Width, bounds.Height);
            using var graphics = Graphics.FromImage(bitmap);
            graphics.CopyFromScreen(bounds.X, bounds.Y, 0, 0, bounds.Size);

            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            string fileName = $"Screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png";
            string filePath = Path.Combine(desktopPath, fileName);

            bitmap.Save(filePath, ImageFormat.Png);

            return Task.FromResult<(bool, string, string?)>((true, filePath, null));
        }
        catch (Exception ex)
        {
            return Task.FromResult<(bool, string, string?)>((false, string.Empty, $"An error occurred while taking the screenshot: {ex.Message}"));
        }
    }
}
#endif
