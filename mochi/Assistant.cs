using Azure.AI.OpenAI;
using Microsoft.CognitiveServices.Speech;

static class Assistant2
{
    internal static async Task Run(AIServices ai)
    {
        Prompt prompt = new Prompt("a cat");
        var keywordModel = KeywordRecognitionModel.FromFile("hey_mochi.table");
        while (true)
        {
            Console.WriteLine("waiting ...");

            KeywordRecognitionResult keyword = await ai.RecognizeOnceAsync(keywordModel);
            await ai.StopSpeakingAsync();

            Console.WriteLine("listening ...");
            SpeechRecognitionResult recognized = await ai.RecognizeOnceAsync();
            var recognizedText = recognized.Text;
            Console.WriteLine(recognizedText);

            if (string.IsNullOrEmpty(recognizedText)) continue;

            bool handled = await ai.TryInterpret(recognizedText);
            if (handled) continue;

            Console.WriteLine("thinking ...");

            prompt.Add(recognizedText);

            var response = await ai.GetResponseAsync(prompt);

            prompt.Add(response, ChatRole.Assistant);

            Console.WriteLine("speaking ...");
            ai.SpeakTextAsync(response);
        }
    }

    static ChatMessage CreatePersonaMessage(string persona)
    {
        return new ChatMessage(ChatRole.System, $"You are {persona}. Answer with short responses; around 3 sentences.");

    }
}
