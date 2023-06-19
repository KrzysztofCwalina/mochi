using Azure;
using Azure.AI.OpenAI;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using System.Text.Json.Serialization;

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
    readonly KeywordRecognitionModel keywordModel;

    public AIServices()
    {
        openAiEndpoint = ReadConfigurationSetting("MOCHI_AI_ENDPOINT");
        openAiKey = ReadConfigurationSetting("MOCHI_AI_KEY");
        openAiModelOrDeployment = ReadConfigurationSetting("MOCHI_AI_MODEL");

        speechEndpoint = ReadConfigurationSetting("MOCHI_SPEECH_ENDPOINT");
        speechKey = ReadConfigurationSetting("MOCHI_SPEECH_KEY");

        var speechConfiguration = SpeechConfig.FromEndpoint(new Uri(speechEndpoint), speechKey);
        speechConfiguration.SpeechSynthesisVoiceName = "en-US-JaneNeural";
        synthetizer = new SpeechSynthesizer(speechConfiguration);
        recognizer = new SpeechRecognizer(speechConfiguration);

        aiClient = new OpenAIClient(new Uri(openAiEndpoint), new AzureKeyCredential(openAiKey));

        keywordRecognizer = new KeywordRecognizer(AudioConfig.FromDefaultMicrophoneInput());
    }

    static string ReadConfigurationSetting(string settingName)
    {
        var value = Environment.GetEnvironmentVariable(settingName);
        if (value == null)
        {
            Console.WriteLine($"{settingName} not set.");
            Environment.Exit(0);
        }
        return value;
    }

    public async Task WaitForKeyword(KeywordRecognitionModel keywoards)
    {
        await keywordRecognizer.RecognizeOnceAsync(keywoards);
    }

    public async Task<string> RecognizeFromMicrophoneAsync()
    {
        var result = await recognizer.RecognizeOnceAsync();
        return result.Text;
    }

    public async Task StopSpeakingAsync() => await synthetizer.StopSpeakingAsync();

    async Task<ChatCompletions> GetChatCompletionsAsync(Prompt prompt)
        => await aiClient.GetChatCompletionsAsync(openAiModelOrDeployment, prompt.CreateChatOptions());
    
    public async Task<string> GetAnswerAsync(Prompt prompt)
    {
        ChatCompletions results = await GetChatCompletionsAsync(prompt);
        var response = results.Choices.First().Message.Content;
        return response;
    }

    public void SpeakTextAsync(string response) => synthetizer.SpeakTextAsync(response);
}


struct Entity
{
    [JsonPropertyName("category")]
    public string Category { get; set; }
    [JsonPropertyName("text")]
    public string Text { get; set; }
}
