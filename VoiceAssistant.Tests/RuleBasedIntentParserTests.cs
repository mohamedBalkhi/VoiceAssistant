using VoiceAssistant.Infrastructure;
using Xunit;

namespace VoiceAssistant.Tests;

public class RuleBasedIntentParserTests
{
    private readonly RuleBasedIntentParser _parser;

    public RuleBasedIntentParserTests()
    {
        _parser = new RuleBasedIntentParser();
    }

    [Fact]
    public async Task ParseAsync_TakeScreenshot_ReturnsTakeScreenshotIntent()
    {
        // Arrange
        var transcript = "take a screenshot";

        // Act
        var result = await _parser.ParseAsync(transcript);

        // Assert
        Assert.Equal("TakeScreenshot", result.IntentName);
        Assert.Empty(result.Parameters);
    }

    [Fact]
    public async Task ParseAsync_GetTime_ReturnsGetTimeIntent()
    {
        // Arrange
        var transcript = "what time is it";

        // Act
        var result = await _parser.ParseAsync(transcript);

        // Assert
        Assert.Equal("GetTime", result.IntentName);
        Assert.Empty(result.Parameters);
    }

    [Fact]
    public async Task ParseAsync_OpenFolder_ReturnsOpenFolderIntentWithParameters()
    {
        // Arrange
        var transcript = "open folder Documents";

        // Act
        var result = await _parser.ParseAsync(transcript);

        // Assert
        Assert.Equal("OpenFolder", result.IntentName);
        Assert.Single(result.Parameters);
        Assert.Equal("Documents", result.Parameters["FolderName"]);
    }

    [Fact]
    public async Task ParseAsync_CaseInsensitive_ReturnsTakeScreenshotIntent()
    {
        // Arrange
        var transcript = "TaKe A ScReEnShOt";

        // Act
        var result = await _parser.ParseAsync(transcript);

        // Assert
        Assert.Equal("TakeScreenshot", result.IntentName);
    }

    [Fact]
    public async Task ParseAsync_UnknownInput_ReturnsUnknownIntent()
    {
        // Arrange
        var transcript = "play music";

        // Act
        var result = await _parser.ParseAsync(transcript);

        // Assert
        Assert.Equal("Unknown", result.IntentName);
        Assert.Empty(result.Parameters);
    }

    [Fact]
    public async Task ParseAsync_EmptyInput_ReturnsUnknownIntent()
    {
        // Arrange
        var transcript = "";

        // Act
        var result = await _parser.ParseAsync(transcript);

        // Assert
        Assert.Equal("Unknown", result.IntentName);
        Assert.Empty(result.Parameters);
    }
} 