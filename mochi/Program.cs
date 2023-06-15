using Azure;
using Azure.AI.Language.Conversations;
using Azure.AI.OpenAI;
using Azure.Core;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using System.Text.Json.Serialization;

static class Program
{
    static readonly string cluEndpoint;             // MOCHI_CLU_ENDPOINT
    static readonly string cluKey;                  // MOCHI_CLU_KEY
    static readonly string cluProjectName;          // MOCHI_CLU_PROJECT_NAME
    static readonly string cluDeploymentName;       // MOCHI_CLU_DEPLOYMENT_NAME
    static readonly string openAiEndpoint;          // MOCHI_AI_ENDPOINT
    static readonly string openAiKey;               // MOCHI_AI_KEY
    static readonly string openAiModelOrDeployment; // MOCHI_AI_MODEL
    static readonly string speechEndpoint;          // MOCHI_SPEECH_ENDPOINT
    static readonly string speechKey;               // MOCHI_SPEECH_KEY

    static readonly SpeechSynthesizer synthetizer;
    static readonly SpeechRecognizer recognizer;

    static readonly ConversationAnalysisClient cluClient;
    static readonly OpenAIClient aiClient;

    static readonly KeywordRecognizer keywordRecognizer;
    static readonly KeywordRecognitionModel keywordModel;
;
    static Prompt prompt = new Prompt(CreatePersonaMessage("a cat"));

    static ChatMessage CreatePersonaMessage(string persona)
    {
        return new ChatMessage(ChatRole.System, $"You are {persona}. Answer with short responses; around 3 sentences.");
    }

    static Program()
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
        keywordModel = KeywordRecognitionModel.FromFile("hey_mochi.table");
    }

    static async Task Main(string[] args)
    {
        while (true)
        {
            Console.WriteLine("waiting ...");

            KeywordRecognitionResult keyword = await keywordRecognizer.RecognizeOnceAsync(keywordModel);
            await synthetizer.StopSpeakingAsync();

            Console.WriteLine("listening ...");
            SpeechRecognitionResult recognized = await recognizer.RecognizeOnceAsync();
            var recognizedText = recognized.Text;
            Console.WriteLine(recognizedText);

            if (string.IsNullOrEmpty(recognizedText)) continue;

            bool handled = await TryInterpret(recognizedText);
            if (handled) continue;

            Console.WriteLine("thinking ...");

            prompt.Add(new ChatMessage(ChatRole.User, recognizedText));

            ChatCompletions completions = await aiClient.GetChatCompletionsAsync(openAiModelOrDeployment, prompt.CreateChatOptions());

            var response = completions.Choices.First().Message.Content;

            prompt.Add(new ChatMessage(ChatRole.Assistant, response));

            Console.WriteLine("speaking ...");
            synthetizer.SpeakTextAsync(response);
        }
    }

    static async Task<bool> TryInterpret(string text)
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
                if(entities.Length < 1) return false;
                var newPersona = entities[0].Text;
                if (entities.Length<1) throw new NotImplementedException();
                currentPersona = newPersona;
                prompt.Messages[0] = SystemMessage;
                await synthetizer.SpeakTextAsync($"I am {newPersona}"); ;
                break;
            default: throw new NotImplementedException();
        }

        return true;
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
}

struct Entity
{
    [JsonPropertyName("category")]
    public string Category { get; set; }
    [JsonPropertyName("text")]
    public string Text { get; set; }
}

class Prompt
{
    ChatCompletionsOptions _options;

    public ChatCompletionsOptions CreateChatOptions() => _options;
    
    public Prompt(ChatMessage systemMessage)
    {
        if (systemMessage.Role != ChatRole.System) throw new ArgumentOutOfRangeException(nameof(systemMessage.Role));
        _options.Messages.Add(systemMessage);
    }
    
    internal void SetSystemMessage(ChatMessage systemMessage)
    {
        if (systemMessage.Role != ChatRole.System) throw new ArgumentOutOfRangeException(nameof(systemMessage.Role));
        _options.Messages[0] = systemMessage;
    }

    internal void Add(ChatMessage message)
    {
        _options.Messages.Add(message);
    }
}
