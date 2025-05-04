using VoiceAssistant.Domain;

namespace VoiceAssistant.Infrastructure;

// Simple command handler that gets the current time and formats it for speech
// This doesn't require any external process execution, just returns data.
public class GetTimeCommandHandler : ICommandHandler
{
    public string HandledIntentName => "GetTime";

    public Task<CommandResult> ExecuteAsync(IntentResult intent, CancellationToken token = default)
    {
        // Format the current time in a natural, speech-friendly way
        var now = DateTime.Now;
        string timeText = FormatTimeForSpeech(now);
        
        return Task.FromResult(CommandResult.Ok(timeText));
    }

    // Format the time in a more natural way for TTS
    private string FormatTimeForSpeech(DateTime time)
    {
        // For example, 3:45 PM -> "It's quarter to four in the afternoon"
        // 10:15 AM -> "It's quarter past ten in the morning"
        // 2:00 PM -> "It's two o'clock in the afternoon"
        
        string period = time.Hour < 12 ? "in the morning" :
                       time.Hour < 18 ? "in the afternoon" : "in the evening";
        
        int hour12 = time.Hour % 12;
        if (hour12 == 0) hour12 = 12; // 0 should be 12 in 12-hour format
        
        // Create a friendly version of minutes
        if (time.Minute == 0)
        {
            return $"It's {hour12} o'clock {period}.";
        }
        else if (time.Minute == 15)
        {
            return $"It's quarter past {hour12} {period}.";
        }
        else if (time.Minute == 30)
        {
            return $"It's half past {hour12} {period}.";
        }
        else if (time.Minute == 45)
        {
            int nextHour = (hour12 % 12) + 1;
            if (nextHour == 13) nextHour = 1;
            
            // Adjust period if we're crossing a boundary (11:45 AM -> "quarter to 12 in the afternoon")
            string nextPeriod = period;
            if (hour12 == 11 && time.Hour < 12)
            {
                nextPeriod = "in the afternoon";
            }
            else if (hour12 == 11 && time.Hour >= 12 && time.Hour < 23)
            {
                nextPeriod = "in the evening";
            }
            else if (hour12 == 11 && time.Hour >= 23)
            {
                nextPeriod = "in the morning";
            }
            
            return $"It's quarter to {nextHour} {nextPeriod}.";
        }
        else
        {
            // For other times, just use a simple format
            return $"It's {hour12}:{time.Minute:D2} {period}.";
        }
    }
} 