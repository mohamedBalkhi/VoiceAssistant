using Moq;
using VoiceAssistant.Domain;
using VoiceAssistant.Domain.Platform;
using VoiceAssistant.Infrastructure;
using Xunit;

namespace VoiceAssistant.Tests;

public class OpenFolderCommandHandlerTests
{
    private readonly Mock<IFolderOpener> _mockFolderOpener;
    private readonly OpenFolderCommandHandler _handler;

    public OpenFolderCommandHandlerTests()
    {
        _mockFolderOpener = new Mock<IFolderOpener>();
        _handler = new OpenFolderCommandHandler(_mockFolderOpener.Object);
    }

    [Fact]
    public async Task ExecuteAsync_SuccessfullyOpensFolder_ReturnsSuccessResult()
    {
        // Arrange
        var folderName = "Documents";
        var resolvedPath = "/Users/test/Documents";
        var intent = new IntentResult 
        { 
            IntentName = "OpenFolder",
            Parameters = new Dictionary<string, string> { { "FolderName", folderName } }
        };
        
        _mockFolderOpener
            .Setup(x => x.ResolveFolderPath(folderName))
            .Returns(resolvedPath);
            
        _mockFolderOpener
            .Setup(x => x.OpenFolderAsync(resolvedPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, null));

        // Act
        var result = await _handler.ExecuteAsync(intent);

        // Assert
        Assert.True(result.Success);
        Assert.Contains($"Opened folder {folderName}", result.OutputForTTS ?? string.Empty);
    }

    [Fact]
    public async Task ExecuteAsync_NoFolderNameParameter_ReturnsFailureResult()
    {
        // Arrange
        var intent = new IntentResult 
        { 
            IntentName = "OpenFolder",
            Parameters = new Dictionary<string, string>() // Empty parameters
        };

        // Act
        var result = await _handler.ExecuteAsync(intent);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("No folder name specified.", result.FailureReason);
    }

    [Fact]
    public async Task ExecuteAsync_CannotResolveFolder_ReturnsFailureResult()
    {
        // Arrange
        var folderName = "NonExistentFolder";
        var intent = new IntentResult 
        { 
            IntentName = "OpenFolder",
            Parameters = new Dictionary<string, string> { { "FolderName", folderName } }
        };
        
        _mockFolderOpener
            .Setup(x => x.ResolveFolderPath(folderName))
            .Returns((string?)null); // Folder not found

        // Act
        var result = await _handler.ExecuteAsync(intent);

        // Assert
        Assert.False(result.Success);
        Assert.Contains($"Couldn't find folder named '{folderName}'", result.FailureReason ?? string.Empty);
    }

    [Fact]
    public async Task ExecuteAsync_FailsToOpenFolder_ReturnsFailureResult()
    {
        // Arrange
        var folderName = "Documents";
        var resolvedPath = "/Users/test/Documents";
        var errorMessage = "Access denied";
        var intent = new IntentResult 
        { 
            IntentName = "OpenFolder",
            Parameters = new Dictionary<string, string> { { "FolderName", folderName } }
        };
        
        _mockFolderOpener
            .Setup(x => x.ResolveFolderPath(folderName))
            .Returns(resolvedPath);
            
        _mockFolderOpener
            .Setup(x => x.OpenFolderAsync(resolvedPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, errorMessage));

        // Act
        var result = await _handler.ExecuteAsync(intent);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(errorMessage, result.FailureReason);
    }
} 