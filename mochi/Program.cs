using Azure;
using Azure.AI.Language.Conversations;
using Azure.AI.OpenAI;
using Azure.Core;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;

static class Program
{
    static string cluEndpoint = "";
    static string cluKey = "";
    static string openAiEndpoint = "";
    static string openAiKey = "";
    static string speechEndpoint = "";
    static string speechKey = "";

    static string openAiModelOrDeployment = "krystofgpt4";

    readonly static SpeechSynthesizer synthetizer;
    readonly static SpeechRecognizer recognizer;

    static readonly ConversationAnalysisClient conversation = new ConversationAnalysisClient(new Uri(cluEndpoint), new AzureKeyCredential(cluKey));
    static readonly OpenAIClient ai = new OpenAIClient(new Uri(openAiEndpoint), new AzureKeyCredential(openAiKey));

    static readonly AudioConfig audioConfig = AudioConfig.FromDefaultMicrophoneInput();
    static readonly KeywordRecognizer keywordRecognizer = new KeywordRecognizer(audioConfig);
    static readonly KeywordRecognitionModel keywordModel = KeywordRecognitionModel.FromFile("hey_mochi.table");
    static string currentPersona = "You are a cat.";

    static Program()
    {
        var speechConfiguration = SpeechConfig.FromEndpoint(new Uri(speechEndpoint), speechKey);
        speechConfiguration.SpeechSynthesisVoiceName = "en-US-JaneNeural";
        synthetizer = new SpeechSynthesizer(speechConfiguration);
        recognizer = new SpeechRecognizer(speechConfiguration);
    }

    static async Task Main(string[] args)
    {
    start:
        var prompt = new ChatCompletionsOptions();
        prompt.Messages.Add(new ChatMessage(ChatRole.System, currentPersona + " Answer with short responses; around 3 sentences."));
        int totalLength = 0;

        while (true)
        {
            Console.Clear();
            Console.WriteLine(currentPersona);
            Console.WriteLine("waiting ...");

            KeywordRecognitionResult keyword = await keywordRecognizer.RecognizeOnceAsync(keywordModel);

            Console.WriteLine("listening ...");
            SpeechRecognitionResult recognized = await recognizer.RecognizeOnceAsync();
            var recognizedText = recognized.Text;

            if (string.IsNullOrEmpty(recognizedText)) continue;

            bool handled = await TryInterpret(recognizedText);
            if (handled) continue;

            Console.WriteLine("thinking ...");

            prompt.Messages.Add(new ChatMessage(ChatRole.User, recognizedText));
            totalLength += recognizedText.Length;

            ChatCompletions completions = await ai.GetChatCompletionsAsync(openAiModelOrDeployment, prompt);

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

        var response = await conversation.AnalyzeConversationAsync(RequestContent.Create(request));
        dynamic content = response.Content.ToDynamicFromJson();

        var confidence = (float)content.result.prediction.intents[0].confidenceScore;
        if (confidence < 0.8) return false;

        var entities = (Entity[])content.result.prediction.entities;
        switch ((string)content.result.prediction.topIntent)
        {
            case "None": return false;
            case "GetTime":
                await synthetizer.SpeakTextAsync("It's " + DateTimeOffset.Now.ToString("t"));
                break;
            case "ChangePersona":
                if (entities.Length>0) currentPersona = $"You are {entities[0].text}.";
                break;
            default: throw new NotImplementedException();
        }

        return true;
    }
}

struct Entity
{
    public string category { get; set; }
    public string text { get; set; }
}
