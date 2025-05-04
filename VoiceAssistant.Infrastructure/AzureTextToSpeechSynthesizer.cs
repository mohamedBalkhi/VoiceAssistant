using Microsoft.CognitiveServices.Speech;
using VoiceAssistant.Domain;

namespace VoiceAssistant.Infrastructure;


// TODO: Add error handling and logging
// TODO: Allow voice selection via configuration
public class AzureTextToSpeechSynthesizer(string speechKey, string speechRegion) : ITextToSpeechSynthesizer
{
    public async Task SpeakAsync(string textToSpeak, CancellationToken token = default)
    {
        var speechConfig = SpeechConfig.FromSubscription(speechKey, speechRegion);
        // Configure voice, e.g., speechConfig.SpeechSynthesisVoiceName = "en-US-JennyNeural";

        // Note: SpeechSynthesizer uses default speaker output automatically.
        // You might need AudioConfig for different outputs (e.g., files, streams).
        using var synthesizer = new SpeechSynthesizer(speechConfig);

        Console.WriteLine($"Synthesizing speech for text: [{textToSpeak}]"); // Replace with logging

        // Use a TaskCompletionSource to allow cancellation
        var tcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);

        // Register the cancellation token
        using var registration = token.Register(() =>
        {
            // Attempt to stop synthesis. Note: This might not be instantaneous.
            // We signal cancellation via the TaskCompletionSource.
            synthesizer.StopSpeakingAsync().ConfigureAwait(false); // Fire and forget stop
            tcs.TrySetCanceled(token);
        });
        
        // Handle synthesis events
        synthesizer.SynthesisCanceled += (s, e) => {
            var details = SpeechSynthesisCancellationDetails.FromResult(e.Result);
             Console.Error.WriteLine($"TTS CANCELED: Reason={details.Reason}, ErrorCode={details.ErrorCode}, Details={details.ErrorDetails}");
             tcs.TrySetException(new OperationCanceledException($"Speech synthesis canceled: {details.Reason}", new Exception(details.ErrorDetails)));
        };
        
        synthesizer.SynthesisCompleted += (s, e) => {
            Console.WriteLine("TTS Completed");
            tcs.TrySetResult(0); // Signal completion
        };

        synthesizer.SynthesisStarted += (s, e) => {
             Console.WriteLine("TTS Started");
        };

        // Start synthesis but don't wait here directly, wait on the TCS
        _ = synthesizer.SpeakTextAsync(textToSpeak); 

        // Wait for completion or cancellation via the TaskCompletionSource
        await tcs.Task;
    }
} 