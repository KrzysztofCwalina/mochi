// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Azure.AI.OpenAI;
using Azure.FX.AI;
using Mochi;

static partial class Program
{
    public class TextToCode
    {
        AIServices ai = new AIServices();
        string _context;

        public TextToCode(params Type[] actions)
        {
            string api = ApiLister.CreateApiListing(actions);

            // this is the OpenAI system message
            _context = $$"""
                You are a C# programmer. 
                When you show me code, I want just code. No markup, no markdown, not comments, etc.
                You have the following C# API available: {{api}}. 
            """;
        }

        public async Task ExecuteAsync(string request, Func<string, bool> allow = default)
        {
            int retries = 5;
            var prompt = new Prompt(_context);
            prompt.Add($"Show me code (just code) to compute {request}.");

            while (retries-- > 0)
            {
                string response = await ai.GetAnswerAsync(prompt);
                Console.WriteLine("LOG: " + response);
                var error = ExecutionRuntime.MakeCall(response, allow);
                if (error == null) return;
                Console.WriteLine("LOG: " + error);
                prompt.Add($"I got the following error {error} when compiling the code. Can you fix the code you provided previously?", ChatRole.User);
            }
        }
    }
}
