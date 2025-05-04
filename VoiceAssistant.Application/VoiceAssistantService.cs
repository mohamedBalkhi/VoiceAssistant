using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using VoiceAssistant.Application.Commands;
using VoiceAssistant.Domain;

namespace VoiceAssistant.Application;

/// <summary>
/// Main service for the voice assistant that orchestrates the recognition, intent parsing, 
/// command execution, and feedback through TTS.
/// </summary>
public class VoiceAssistantService
{
    private readonly RecognizeSpeechCommandHandler _recognizeSpeechHandler;
    private readonly ParseIntentCommandHandler _parseIntentHandler;
    private readonly ExecuteCommandCommandHandler _executeCommandHandler;
    private readonly SpeakTextCommandHandler _speakTextHandler;
    private readonly ILogger<VoiceAssistantService> _logger;
    
    private CancellationTokenSource? _listeningCts;
    private bool _isListening;
    private readonly ConcurrentQueue<string> _activityLog = new();
    private const int MaxLogEntries = 100;
    private const string DefaultWakeWord = "hey voicy";
    private bool _useWakeWord = true;
    private bool _isProcessingCommand = false;

    public event EventHandler<string>? ActivityLogged;
    public event EventHandler<IntentResult>? IntentRecognized;
    public event EventHandler<bool>? ListeningStateChanged;
    public event EventHandler<CommandResult>? CommandExecuted;

    public VoiceAssistantService(
        RecognizeSpeechCommandHandler recognizeSpeechHandler,
        ParseIntentCommandHandler parseIntentHandler,
        ExecuteCommandCommandHandler executeCommandHandler,
        SpeakTextCommandHandler speakTextHandler,
        ILogger<VoiceAssistantService> logger)
    {
        _recognizeSpeechHandler = recognizeSpeechHandler;
        _parseIntentHandler = parseIntentHandler;
        _executeCommandHandler = executeCommandHandler;
        _speakTextHandler = speakTextHandler;
        _logger = logger;
    }

    public bool IsListening => _isListening;
    
    public IReadOnlyCollection<string> ActivityLog => _activityLog.ToArray();

    public bool UseWakeWord
    {
        get => _useWakeWord;
        set => _useWakeWord = value;
    }

    /// <summary>
    /// Starts listening for voice commands.
    /// </summary>
    public async Task StartListeningAsync()
    {
        if (_isListening)
        {
            LogActivity("Already listening");
            return;
        }

        _isListening = true;
        _listeningCts = new CancellationTokenSource();
        ListeningStateChanged?.Invoke(this, _isListening);
        LogActivity("Started listening");

        try
        {
            await ListenContinuouslyAsync(_listeningCts.Token);
        }
        catch (OperationCanceledException)
        {
            LogActivity("Listening cancelled");
        }
        catch (Exception ex)
        {
            LogActivity($"Error while listening: {ex.Message}");
            _logger.LogError(ex, "Error during continuous listening");
        }
        finally
        {
            _isListening = false;
            ListeningStateChanged?.Invoke(this, _isListening);
        }
    }

    /// <summary>
    /// Stops listening for voice commands.
    /// </summary>
    public void StopListening()
    {
        if (!_isListening || _listeningCts == null)
        {
            return;
        }

        try
        {
            // Signal cancellation
            _listeningCts.Cancel();
            
            // Clean up
            _listeningCts.Dispose();
            _listeningCts = null;
            
            // Explicitly update state
            _isListening = false;
            ListeningStateChanged?.Invoke(this, _isListening);
            
            LogActivity("Stopped listening");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while stopping listening");
        }
    }

    /// <summary>
    /// Processes a single voice command (useful for testing or direct text input).
    /// </summary>
    public async Task ProcessTextCommandAsync(string text)
    {
        LogActivity($"Processing text command: {text}");
        
        try
        {
            _isProcessingCommand = true;
            
            // Parse the intent
            var intent = await _parseIntentHandler.HandleAsync(new ParseIntentCommand(text));
            IntentRecognized?.Invoke(this, intent);
            LogActivity($"Intent recognized: {intent.IntentName}");

            // Check if intent is unknown
            if (intent.IntentName == "Unknown")
            {
                LogActivity("Unknown intent, ignored");
                await _speakTextHandler.HandleAsync(new SpeakTextCommand("I didn't understand that command."));
                return;
            }
            
            // Execute the command
            await ExecuteIntentAsync(intent);
        }
        catch (Exception ex)
        {
            LogActivity($"Error processing text command: {ex.Message}");
            _logger.LogError(ex, "Error processing text command: {Text}", text);
        }
        finally
        {
            _isProcessingCommand = false;
        }
    }

    private async Task ListenContinuouslyAsync(CancellationToken token)
    {
        bool wakeWordDetected = !_useWakeWord;
        
        try
        {
            while (!token.IsCancellationRequested)
            {
                // Check cancellation at the beginning of each loop
                token.ThrowIfCancellationRequested();
                
                if (_isProcessingCommand)
                {
                    // Skip listening while processing a command
                    await Task.Delay(100, token);
                    continue;
                }
                
                if (_useWakeWord && !wakeWordDetected)
                {
                    LogActivity("Listening for wake word...");
                }
                else
                {
                    LogActivity("Listening for voice input...");
                }
                
                // Use the speech recognizer to get text from voice
                string recognizedText;
                try
                {
                    recognizedText = await _recognizeSpeechHandler.HandleAsync(new RecognizeSpeechCommand(token));
                    
                    if (string.IsNullOrWhiteSpace(recognizedText) || 
                        recognizedText.StartsWith("CANCELED") || 
                        recognizedText.StartsWith("ERROR") || 
                        recognizedText.StartsWith("NOMATCH"))
                    {
                        LogActivity($"Recognition issue: {recognizedText}");
                        continue;
                    }
                    
                    LogActivity($"Recognized: {recognizedText}");
                    
                    // Check for wake word if needed
                    if (_useWakeWord && !wakeWordDetected)
                    {
                        if (recognizedText.ToLower().Contains(DefaultWakeWord))
                        {
                            wakeWordDetected = true;
                            await _speakTextHandler.HandleAsync(new SpeakTextCommand("I'm listening"));
                            continue;
                        }
                        else
                        {
                            // Not the wake word, continue listening
                            continue;
                        }
                    }
                    
                    // If we got here, either wake word is not used or it was already detected
                    await ProcessTextCommandAsync(recognizedText);
                    
                    // Reset wake word detection after processing command
                    if (_useWakeWord)
                    {
                        wakeWordDetected = false;
                    }
                }
                catch (OperationCanceledException)
                {
                    throw; // Let the caller handle cancellation
                }
                catch (Exception ex)
                {
                    LogActivity($"Error during recognition: {ex.Message}");
                    _logger.LogError(ex, "Error during speech recognition");
                    continue;
                }
            }
        }
        catch (OperationCanceledException)
        {
            LogActivity("Listening operation was canceled");
            throw;
        }
        catch (Exception ex)
        {
            LogActivity($"Error in continuous listening: {ex.Message}");
            _logger.LogError(ex, "Error in continuous listening loop");
            throw;
        }
    }

    private async Task ExecuteIntentAsync(IntentResult intent, CancellationToken token = default)
    {
        try
        {
            LogActivity($"Executing command: {intent.IntentName}");
            
            var (handlerFound, result) = await _executeCommandHandler.HandleAsync(
                new ExecuteCommandCommand(intent, token));
            
            if (!handlerFound)
            {
                LogActivity($"No handler found for intent: {intent.IntentName}");
                await _speakTextHandler.HandleAsync(
                    new SpeakTextCommand($"I don't know how to {intent.IntentName}.", token));
                return;
            }
            
            // We know result is not null here since handlerFound is true
            CommandExecuted?.Invoke(this, result!);
            
            if (result!.Success)
            {
                LogActivity($"Command executed successfully: {intent.IntentName}");
                if (!string.IsNullOrWhiteSpace(result.OutputForTTS))
                {
                    await _speakTextHandler.HandleAsync(
                        new SpeakTextCommand(result.OutputForTTS, token));
                }
            }
            else
            {
                LogActivity($"Command failed: {result.FailureReason}");
                await _speakTextHandler.HandleAsync(
                    new SpeakTextCommand($"Sorry, I couldn't do that. {result.FailureReason}", token));
            }
        }
        catch (Exception ex)
        {
            LogActivity($"Error executing command: {ex.Message}");
            _logger.LogError(ex, "Error executing command for intent {IntentName}", intent.IntentName);
            await _speakTextHandler.HandleAsync(
                new SpeakTextCommand("Sorry, there was an error executing your command.", token));
        }
    }

    private void LogActivity(string message)
    {
        _activityLog.Enqueue(message);
        while (_activityLog.Count > MaxLogEntries)
        {
            _activityLog.TryDequeue(out _);
        }
        
        _logger.LogInformation(message);
        ActivityLogged?.Invoke(this, message);
    }
} 