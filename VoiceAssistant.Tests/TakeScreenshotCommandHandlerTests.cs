using Moq;
using VoiceAssistant.Domain;
using VoiceAssistant.Domain.Platform;
using VoiceAssistant.Infrastructure;
using Xunit;

namespace VoiceAssistant.Tests;

public class TakeScreenshotCommandHandlerTests
{
    private readonly Mock<IScreenshotProvider> _mockScreenshotProvider;
    private readonly TakeScreenshotCommandHandler _handler;

    public TakeScreenshotCommandHandlerTests()
    {
        _mockScreenshotProvider = new Mock<IScreenshotProvider>();
        _handler = new TakeScreenshotCommandHandler(_mockScreenshotProvider.Object);
    }

    [Fact]
    public async Task ExecuteAsync_SuccessfulScreenshot_ReturnsSuccessResult()
    {
        // Arrange
        var intent = new IntentResult { IntentName = "TakeScreenshot" };
        var screenshotPath = "/Users/test/Desktop/Screenshot_123.png";
        
        _mockScreenshotProvider
            .Setup(x => x.CaptureScreenshotAsync(true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, screenshotPath, null));

        // Act
        var result = await _handler.ExecuteAsync(intent);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("Screenshot taken", result.OutputForTTS ?? string.Empty);
        Assert.Contains("/Users/test/Desktop", result.OutputForTTS ?? string.Empty);
    }

    [Fact]
    public async Task ExecuteAsync_ScreenshotCancelled_ReturnsSuccessWithCancelMessage()
    {
        // Arrange
        var intent = new IntentResult { IntentName = "TakeScreenshot" };
        
        _mockScreenshotProvider
            .Setup(x => x.CaptureScreenshotAsync(true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, string.Empty, "Screenshot cancelled by user."));

        // Act
        var result = await _handler.ExecuteAsync(intent);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Screenshot cancelled by user.", result.OutputForTTS);
    }

    [Fact]
    public async Task ExecuteAsync_ScreenshotFailed_ReturnsFailureResult()
    {
        // Arrange
        var intent = new IntentResult { IntentName = "TakeScreenshot" };
        
        _mockScreenshotProvider
            .Setup(x => x.CaptureScreenshotAsync(true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, string.Empty, "Permission denied"));

        // Act
        var result = await _handler.ExecuteAsync(intent);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Permission denied", result.FailureReason);
    }
} 