using Azure;
using Azure.AI.Language.Conversations;
using Azure.AI.OpenAI;
using Azure.Core;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using System.Text.Json.Serialization;

public class AIServices
{
    readonly string cluEndpoint;             // MOCHI_CLU_ENDPOINT
    readonly string cluKey;                  // MOCHI_CLU_KEY
    readonly string cluProjectName;          // MOCHI_CLU_PROJECT_NAME
    readonly string cluDeploymentName;       // MOCHI_CLU_DEPLOYMENT_NAME
    readonly string openAiEndpoint;          // MOCHI_AI_ENDPOINT
    readonly string openAiKey;               // MOCHI_AI_KEY
    readonly string openAiModelOrDeployment; // MOCHI_AI_MODEL
    readonly string speechEndpoint;          // MOCHI_SPEECH_ENDPOINT
    readonly string speechKey;               // MOCHI_SPEECH_KEY

    readonly SpeechSynthesizer synthetizer;
    readonly SpeechRecognizer recognizer;

    readonly ConversationAnalysisClient cluClient;
    readonly OpenAIClient aiClient;

    readonly KeywordRecognizer keywordRecognizer;
    readonly KeywordRecognitionModel keywordModel;

    public AIServices()
    {
        cluEndpoint = ReadConfigurationSetting("MOCHI_CLU_ENDPOINT");
        cluKey = ReadConfigurationSetting("MOCHI_CLU_KEY");
        cluProjectName = ReadConfigurationSetting("MOCHI_CLU_PROJECT_NAME");
        cluDeploymentName = ReadConfigurationSetting("MOCHI_CLU_DEPLOYMENT_NAME");

        openAiEndpoint = ReadConfigurationSetting("MOCHI_AI_ENDPOINT");
        openAiKey = ReadConfigurationSetting("MOCHI_AI_KEY");
        openAiModelOrDeployment = ReadConfigurationSetting("MOCHI_AI_MODEL");

        speechEndpoint = ReadConfigurationSetting("MOCHI_SPEECH_ENDPOINT");
        speechKey = ReadConfigurationSetting("MOCHI_SPEECH_KEY");

        var speechConfiguration = SpeechConfig.FromEndpoint(new Uri(speechEndpoint), speechKey);
        speechConfiguration.SpeechSynthesisVoiceName = "en-US-JaneNeural";
        synthetizer = new SpeechSynthesizer(speechConfiguration);
        recognizer = new SpeechRecognizer(speechConfiguration);

        cluClient = new ConversationAnalysisClient(new Uri(cluEndpoint), new AzureKeyCredential(cluKey));
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

    internal async Task<KeywordRecognitionResult> RecognizeOnceAsync(KeywordRecognitionModel keywordModel)
    {
        return await keywordRecognizer.RecognizeOnceAsync(keywordModel);
    }

    internal async Task StopSpeakingAsync()
    {
        await synthetizer.StopSpeakingAsync();
    }

    internal async Task<SpeechRecognitionResult> RecognizeOnceAsync()
    {
        return await recognizer.RecognizeOnceAsync();
    }

    async Task<ChatCompletions> GetChatCompletionsAsync(Prompt prompt)
    {
        return await aiClient.GetChatCompletionsAsync(openAiModelOrDeployment, prompt.CreateChatOptions());
    }

    internal async Task<string> GetResponseAsync(Prompt prompt)
    {
        ChatCompletions results = await GetChatCompletionsAsync(prompt);
        var response = results.Choices.First().Message.Content;
        return response;
    }

    public async Task<bool> TryInterpret(string text)
    {
        var request = new
        {
            analysisInput = new
            {
                conversationItem = new
                {
                    text,
                    id = "1",
                    participantId = "1",
                }
            },
            parameters = new
            {
                projectName = cluProjectName,
                deploymentName = cluDeploymentName,
                stringIndexType = "Utf16CodeUnit",
            },
            kind = "Conversation",
        };

        var response = await cluClient.AnalyzeConversationAsync(RequestContent.Create(request));
        dynamic content = response.Content.ToDynamicFromJson();

        var confidence = (float)content.result.prediction.intents[0].confidenceScore;
        if (confidence < 0.8) return false;

        var entities = (Entity[])content.result.prediction.entities;
        var intent = (string)content.result.prediction.topIntent;
        Console.WriteLine(intent);
        switch (intent)
        {
            case "None": return false;
            case "GetTime":
                await synthetizer.SpeakTextAsync("It's " + DateTimeOffset.Now.ToString("t"));
                break;
            case "ChangePersona":
                if (entities.Length < 1) return false;
                var newPersona = entities[0].Text;
                if (entities.Length<1) throw new NotImplementedException();
                await synthetizer.SpeakTextAsync($"I am {newPersona}"); ;
                break;
            default: throw new NotImplementedException();
        }

        return true;
    }

    internal void SpeakTextAsync(string response)
    {
        synthetizer.SpeakTextAsync(response);
    }
}


struct Entity
{
    [JsonPropertyName("category")]
    public string Category { get; set; }
    [JsonPropertyName("text")]
    public string Text { get; set; }
}
