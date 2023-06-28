// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Azure.AI.OpenAI;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using mochi;
using System;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Azure.FX.AI;

public class AIServices
{
    readonly string openAiEndpoint;          // MOCHI_AI_ENDPOINT
    readonly string openAiKey;               // MOCHI_AI_KEY
    readonly string openAiModelOrDeployment; // MOCHI_AI_MODEL
    readonly string speechEndpoint;          // MOCHI_SPEECH_ENDPOINT
    readonly string speechKey;               // MOCHI_SPEECH_KEY

    readonly SpeechSynthesizer synthetizer;
    readonly SpeechRecognizer recognizer;

    readonly OpenAIClient aiClient;
    readonly KeywordRecognizer keywordRecognizer;

    public AIServices(SettingsClient settings)
    {    
        openAiEndpoint = settings.GetSecret("mochi-ai-endpoint");
        openAiKey = settings.GetSecret("mochi-ai-key");
        openAiModelOrDeployment = settings.GetSecret("mochi-ai-model");

        speechEndpoint = settings.GetSecret("mochi-speech-endpoint");
        speechKey = settings.GetSecret("mochi-speech-key");

        var speechConfiguration = SpeechConfig.FromEndpoint(new Uri(speechEndpoint), speechKey);
        speechConfiguration.SpeechSynthesisVoiceName = "en-US-JaneNeural";
        synthetizer = new SpeechSynthesizer(speechConfiguration);
        recognizer = new SpeechRecognizer(speechConfiguration);

        aiClient = new OpenAIClient(new Uri(openAiEndpoint), new AzureKeyCredential(openAiKey));

        keywordRecognizer = new KeywordRecognizer(AudioConfig.FromDefaultMicrophoneInput());
    }

    public async Task WaitForKeywordAsync(KeywordRecognitionModel keywoards)
        => await keywordRecognizer.RecognizeOnceAsync(keywoards);

    public async Task<string> RecognizeFromMicrophoneAsync()
    {
        var result = await recognizer.RecognizeOnceAsync();
        return result.Text;
    }

    public void SpeakAsync(string response, bool blocking = true)
    {
        var task = synthetizer.SpeakTextAsync(response);
        if (blocking) task.GetAwaiter().GetResult();
    }

    public async Task StopSpeakingAsync() => await synthetizer.StopSpeakingAsync();

    public async Task<string> GetAnswerAsync(Prompt prompt)
    {
        var options = prompt.CreateChatOptions();
        ChatCompletions results = await aiClient.GetChatCompletionsAsync(openAiModelOrDeployment, options);
        var response = results.Choices.First().Message.Content;
        return response;
    }
}

struct Entity
{
    [JsonPropertyName("category")]
    public string Category { get; set; }
    [JsonPropertyName("text")]
    public string Text { get; set; }
}
