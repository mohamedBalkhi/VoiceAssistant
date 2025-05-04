# Voice Assistant

A modular voice assistant built with C#, Avalonia UI, and Azure AI services(Replaceable).

## Features

- Voice recognition with Azure Speech Services
- Natural language processing with Azure Language Understanding
- Text-to-speech capabilities
- Cross-platform UI with Avalonia
- Clean architecture design

## Azure Language Service Setup

### Step 1: Create Azure Language Service Resource

1. Go to the [Azure Portal](https://portal.azure.com/)
2. Create a new "Language Service" resource
3. Select the appropriate subscription, resource group, and region
4. Complete the resource creation process

### Step 2: Create a Conversational Language Understanding Project

1. Go to [Language Studio](https://language.cognitive.azure.com/)
2. Sign in with your Azure account
3. Create a new "Conversational Language Understanding" project
4. Name your project "VoiceAssistant" (or update the config accordingly)

### Step 3: Define Intents and Entities

Create the following intents:

1. **TakeScreenshot**
   - Add example utterances like:
     - "Take a screenshot"
     - "Capture my screen"
     - "Take screenshot"
     - "Screenshot please"

2. **GetTime**
   - Add example utterances like:
     - "What time is it"
     - "Tell me the current time"
     - "What's the time"
     - "Current time please"

3. **OpenFolder**
   - Add example utterances like:
     - "Open folder Documents"
     - "Browse to Downloads"
     - "Show me the Pictures folder"
   - Add an entity "FolderName" to extract the folder name from utterances

### Step 4: Train and Deploy Your Model

1. Train your model
2. Deploy it with the name "production" (or update the config accordingly)

### Step 5: Update Configuration

Create the `.env` file with your Azure service keys:

1. Open `.env.example`
2. Create `.env` based on it (or simply rename)
3. Replace the placeholder values with your actual Azure keys and endpoints.



## Local Development

For local development, you can set `useMockServices = true` in Program.cs to use mock implementations instead of making actual Azure API calls.

## Architecture

The application follows a clean architecture approach:

- **VoiceAssistant.Domain**: Core entities and interfaces
- **VoiceAssistant.Application**: Application logic and use cases
- **VoiceAssistant.Infrastructure**: External services implementation
- **VoiceAssistant.UI**: User interface with Avalonia


## Extensibility

This project is designed with extensibility in mind through the use of abstractions and dependency injection:


For example, to implement a different speech recognition service:

1. Create a new class implementing `ISpeechRecognitionService`
2. Register your implementation in the dependency injection container
3. No other code changes required!

## Running the Application

1. Set up your Azure services as described above
2. Create the .env with your API keys based on .env.example
3. Build and run the application:

```bash
dotnet run --project VoiceAssistant.UI
``` 