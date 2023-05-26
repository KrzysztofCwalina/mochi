using Azure;
using Azure.AI.Language.Conversations;
using Azure.AI.OpenAI;
using Azure.Core;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;

static class Program
{
    static readonly string cluEndpoint;             // MOCHI_CLU_ENDPOINT
    static readonly string cluKey;                  // MOCHI_CLU_KEY
    static readonly string openAiEndpoint;          // MOCHI_AI_ENDPOINT
    static readonly string openAiKey;               // MOCHI_AI_KEY
    static readonly string openAiModelOrDeployment; // MOCHI_AI_MODEL
    static readonly string speechEndpoint;          // MOCHI_SPEECH_ENDPOINT
    static readonly string speechKey;               // MOCHI_SPEECH_KEY

    static readonly SpeechSynthesizer synthetizer;
    static readonly SpeechRecognizer recognizer;

    static readonly ConversationAnalysisClient conversationClient;
    static readonly OpenAIClient aiClient;

    static readonly AudioConfig audioConfig;
    static readonly KeywordRecognizer keywordRecognizer;
    static readonly KeywordRecognitionModel keywordModel;

    static string currentPersona = "You are a cat.";

    static Program()
    {
        cluEndpoint = ReadConfigurationSetting("MOCHI_CLU_ENDPOINT");
        cluKey = ReadConfigurationSetting("MOCHI_CLU_KEY");

        openAiEndpoint = ReadConfigurationSetting("MOCHI_AI_ENDPOINT");
        openAiKey = ReadConfigurationSetting("MOCHI_AI_KEY");
        openAiModelOrDeployment = ReadConfigurationSetting("MOCHI_AI_MODEL");

        speechEndpoint = ReadConfigurationSetting("MOCHI_SPEECH_ENDPOINT");
        speechKey = ReadConfigurationSetting("MOCHI_SPEECH_KEY");

        var speechConfiguration = SpeechConfig.FromEndpoint(new Uri(speechEndpoint), speechKey);
        speechConfiguration.SpeechSynthesisVoiceName = "en-US-JaneNeural";
        synthetizer = new SpeechSynthesizer(speechConfiguration);
        recognizer = new SpeechRecognizer(speechConfiguration);

        conversationClient = new ConversationAnalysisClient(new Uri(cluEndpoint), new AzureKeyCredential(cluKey));
        aiClient = new OpenAIClient(new Uri(openAiEndpoint), new AzureKeyCredential(openAiKey));
        audioConfig = AudioConfig.FromDefaultMicrophoneInput();
        keywordRecognizer = new KeywordRecognizer(audioConfig);
        keywordModel = KeywordRecognitionModel.FromFile("hey_mochi.table");
    }

    static async Task Main(string[] args)
    {
    start:
        var prompt = new ChatCompletionsOptions();
        prompt.Messages.Add(new ChatMessage(ChatRole.System, currentPersona + " Answer with short responses; around 3 sentences."));
        int totalLength = 0;

        while (true)
        {
            Console.WriteLine(currentPersona);
            Console.WriteLine("waiting ...");

            KeywordRecognitionResult keyword = await keywordRecognizer.RecognizeOnceAsync(keywordModel);

            Console.WriteLine("listening ...");
            SpeechRecognitionResult recognized = await recognizer.RecognizeOnceAsync();
            var recognizedText = recognized.Text;
            Console.WriteLine(recognizedText);

            if (string.IsNullOrEmpty(recognizedText)) continue;

            bool handled = await TryInterpret(recognizedText);
            if (handled) continue;

            Console.WriteLine("thinking ...");

            prompt.Messages.Add(new ChatMessage(ChatRole.User, recognizedText));
            totalLength += recognizedText.Length;

            ChatCompletions completions = await aiClient.GetChatCompletionsAsync(openAiModelOrDeployment, prompt);

            var response = completions.Choices.First().Message.Content;

            prompt.Messages.Add(new ChatMessage(ChatRole.Assistant, response));
            totalLength -= response.Length;

            if (totalLength > 1000) goto start;

            Console.WriteLine("speaking ...");
            await synthetizer.SpeakTextAsync(response);
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
                projectName = "mochi",
                deploymentName = "mochiclu",
                stringIndexType = "Utf16CodeUnit",
            },
            kind = "Conversation",
        };

        var response = await conversationClient.AnalyzeConversationAsync(RequestContent.Create(request));
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
                var newPersona = entities[0].text;
                if (entities.Length>0) currentPersona = $"You are {newPersona}.";
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
    public string category { get; set; }
    public string text { get; set; }
}
