namespace VoiceAssistant.Domain;

public class CommandResult
{
    public bool Success { get; init; } = true; // Default to success
    public string? FailureReason { get; init; } // Reason if Success is false
    public string? OutputForTTS { get; init; } // Data to be spoken back (e.g., current time)

    // Static factory methods for convenience
    public static CommandResult Ok(string? outputForTTS = null) => new() { Success = true, OutputForTTS = outputForTTS };
    public static CommandResult Fail(string reason) => new() { Success = false, FailureReason = reason };
} 