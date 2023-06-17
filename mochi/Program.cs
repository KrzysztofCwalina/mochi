
static class Program
{
    static async Task Main()
    {
        AIServices ai = new AIServices();

        var api = """
            public static class Assistant {
                public string GetTime(string location = default);
                public string GetWeather(string location = default, DateTimeOffset time = default);
            }
        """;

        // this is the OpenAI system message
        var context = $$"""
            You are an expert C# programmer. 
            You have the following C# API awaliable: {{api}}. 
            When you show me code, I want just the calling code. No markup, no markdown, not comments, etc.
        """;
        
        var requests = new string[] {
            "what time is it?",
            "what's the time?",
            "what's the weather in Seattle?",
            "what's the weather tomorrow?"
        }; 

        foreach (string request in requests)
        {
            var prompt = new Prompt(context);
            prompt.Add($"Show me one line of code calling the API to compute {request}");

            string response = await ai.GetResponseAsync(prompt);

            Console.WriteLine($"Q: {request}\nA: {response}");
        }
    }
}
