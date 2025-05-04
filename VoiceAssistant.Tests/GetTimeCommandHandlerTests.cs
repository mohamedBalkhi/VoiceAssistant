using VoiceAssistant.Domain;
using VoiceAssistant.Infrastructure;
using Xunit;

namespace VoiceAssistant.Tests;

public class GetTimeCommandHandlerTests
{
    private readonly GetTimeCommandHandler _handler;

    public GetTimeCommandHandlerTests()
    {
        _handler = new GetTimeCommandHandler();
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsSuccessWithFormattedTime()
    {
        // Arrange
        var intent = new IntentResult { IntentName = "GetTime" };

        // Act
        var result = await _handler.ExecuteAsync(intent);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.OutputForTTS);
        Assert.Contains("It's", result.OutputForTTS); // Basic check for time format
        
        // Try to ensure it's in one of our expected formats
        bool hasExpectedFormat = 
            result.OutputForTTS.Contains("o'clock") ||
            result.OutputForTTS.Contains("quarter past") ||
            result.OutputForTTS.Contains("half past") ||
            result.OutputForTTS.Contains("quarter to") ||
            result.OutputForTTS.Contains(":"); // Default format with minutes
            
        Assert.True(hasExpectedFormat, $"Time output format unexpected: {result.OutputForTTS}");
    }

    [Fact]
    public void HandledIntentName_ReturnsGetTime()
    {
        // Assert
        Assert.Equal("GetTime", _handler.HandledIntentName);
    }
} 