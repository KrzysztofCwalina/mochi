using Azure.AI.OpenAI;
using Microsoft.CognitiveServices.Speech;

static class Mochi
{
    internal static async Task Run(AIServices ai)
    {
        Prompt prompt = new Prompt("you are a cat.");
        var keyword = KeywordRecognitionModel.FromFile("hey_mochi.table");

        while (true)
        {
            Console.WriteLine("waiting ...");

            await ai.WaitForKeywordAsync(keyword);
            await ai.StopSpeakingAsync();

            Console.WriteLine("listening ...");
            string recognized = await ai.RecognizeFromMicrophoneAsync();
            Console.WriteLine(recognized);

            if (string.IsNullOrEmpty(recognized)) continue;

            Console.WriteLine("thinking ...");

            prompt.Add(recognized);

            var response = await ai.GetAnswerAsync(prompt);

            prompt.Add(response, ChatRole.Assistant);

            Console.WriteLine("speaking ...");
            ai.SpeakAsync(response);
        }
    }
}
