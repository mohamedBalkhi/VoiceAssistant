using VoiceAssistant.Infrastructure;
using Xunit;

namespace VoiceAssistant.Tests;

public class EnhancedRuleBasedIntentParserTests
{
    private readonly RuleBasedIntentParser _parser;

    public EnhancedRuleBasedIntentParserTests()
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
    public async Task ParseAsync_OpenFolder_ReturnsOpenFolderIntentWithParameter()
    {
        // Arrange
        var transcript = "open folder Documents";

        // Act
        var result = await _parser.ParseAsync(transcript);

        // Assert
        Assert.Equal("OpenFolder", result.IntentName);
        Assert.Single(result.Parameters);
        Assert.True(result.Parameters.TryGetValue("FolderName", out var folderName));
        Assert.Equal("Documents", folderName);
    }
}
