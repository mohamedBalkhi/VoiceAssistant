#if WINDOWS
using Microsoft.Extensions.DependencyInjection;
using System.Windows.Forms;
using VoiceAssistant.Domain.Platform;
using VoiceAssistant.Infrastructure;
using VoiceAssistant.Infrastructure.Platform.Windows;
using Xunit;

namespace VoiceAssistant.Tests;

public class WindowsPlatformServiceTests
{
    [Fact]
    public void AddWindowsPlatformServices_RegisterServices()
    {
        var services = new ServiceCollection();

        services.AddWindowsPlatformServices();
        var provider = services.BuildServiceProvider();

        var screenshot = provider.GetRequiredService<IScreenshotProvider>();
        var folderOpener = provider.GetRequiredService<IFolderOpener>();

        Assert.IsType<WindowsScreenshotProvider>(screenshot);
        Assert.IsType<WindowsFolderOpener>(folderOpener);
    }

    [Theory]
    [InlineData("Documents")]
    [InlineData("Desktop")]
    public void ResolveFolderPath_KnownFolder_ReturnsPath(string folder)
    {
        var opener = new WindowsFolderOpener();

        var path = opener.ResolveFolderPath(folder);

        Assert.False(string.IsNullOrEmpty(path));
        Assert.True(Directory.Exists(path));
    }

    [Fact]
    public async Task CaptureScreenshotAsync_SavesFile()
    {
        if (SystemInformation.VirtualScreen.Width == 0 || SystemInformation.VirtualScreen.Height == 0)
        {
            return; // Skip when no virtual screen available
        }

        var provider = new WindowsScreenshotProvider();
        var result = await provider.CaptureScreenshotAsync(false);

        try
        {
            Assert.True(result.Success);
            Assert.True(File.Exists(result.FilePath));
        }
        finally
        {
            if (File.Exists(result.FilePath))
            {
                File.Delete(result.FilePath);
            }
        }
    }
}
#endif
