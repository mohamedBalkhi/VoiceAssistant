using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using VoiceAssistant.Domain;

namespace VoiceAssistant.Infrastructure;

public class AzureSpeechRecognizer(string speechKey, string speechRegion) : IRecognizer
{
    public async Task<string> RecognizeAsync(CancellationToken token)
    {
        var speechConfig = SpeechConfig.FromSubscription(speechKey, speechRegion);
        // Configure for specific language if needed, e.g., speechConfig.SpeechRecognitionLanguage = "en-US";

        // Uses the default microphone input.
        using var audioConfig = AudioConfig.FromDefaultMicrophoneInput();
        using var recognizer = new SpeechRecognizer(speechConfig, audioConfig);

        Console.WriteLine("Speak into your microphone."); // Replace with proper logging/UI feedback
        SpeechRecognitionResult? result = null;
        try
        {
            // Register cancellation token
            using var registration = token.Register(async () => await recognizer.StopContinuousRecognitionAsync());

            result = await recognizer.RecognizeOnceAsync();

            // Check if cancellation was requested AFTER the call potentially completed or threw
            token.ThrowIfCancellationRequested();

        }
        catch (OperationCanceledException)
        {
            // This catches cancellation triggered by the token before or during RecognizeOnceAsync
            return "CANCELED: Operation was cancelled by the user.";
        }
        catch (Exception ex) // Catch other potential exceptions from RecognizeOnceAsync
        {
             // Log the exception details properly
             Console.Error.WriteLine($"ERROR: Recognition failed unexpectedly. Details: {ex}");
             return $"ERROR: Recognition failed. Check logs for details.";
        }
        finally
        {
            // Ensure recognition stops regardless of outcome, but don't wait if cancellation already occurred
            if (!token.IsCancellationRequested)
            {
                await recognizer.StopContinuousRecognitionAsync(); 
            }
        }

        if (result == null)
        {
            // This might happen if an exception occurred but wasn't caught, or logic error
            return "ERROR: Recognition result was unexpectedly null."; 
        }

        return result.Reason switch
        {
            ResultReason.RecognizedSpeech => result.Text,
            ResultReason.NoMatch => "NOMATCH: Speech could not be recognized.",
            ResultReason.Canceled => HandleCancellation(result), 
            _ => $"ERROR: Recognition failed with reason: {result.Reason}" // Generic error for other reasons
        };
    }

    private static string HandleCancellation(SpeechRecognitionResult result)
    {
        var cancellation = CancellationDetails.FromResult(result);
        string message = $"CANCELED: Reason={cancellation.Reason}";
        if (cancellation.Reason == CancellationReason.Error)
        {
            // Log the error details properly
            // ErrorUri is not directly available here, usually logged internally by SDK if needed.
            Console.Error.WriteLine($"CANCELED: ErrorCode={cancellation.ErrorCode}, ErrorDetails={cancellation.ErrorDetails}");
            message += $", ErrorCode={cancellation.ErrorCode}. Check logs for details."; // Corrected typo here
        }
        return message;
    }
} 