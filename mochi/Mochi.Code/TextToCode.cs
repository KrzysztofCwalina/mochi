// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Azure.AI.OpenAI;
using Azure.FX.AI;
using Azure.FX.Tooling;
using Mochi;

static partial class Program
{
    public class TextToCode
    {
        readonly AIServices ai = new AIServices();
        readonly Type _assistant;
        readonly string _context;
        readonly Sandbox _sandbox;
        Action<string> _noMatch;
        bool _log;
        public TextToCode(Type assistant, Action<string> fallback, bool log)
        {
            _assistant = assistant;
            _noMatch = fallback;
            _log = log;

            string api = ApiLister.CreateApiListing(_assistant);

            // this is the OpenAI system message
            _context = $$"""
                You have the following C# API available: {{api}}. 
                Your answers must be either C# code calling one of the provided APIs, or free form textual answer, if none of the APIs match.
                When you generate C# code, I want just code; no markup, no markdown, no commentary, etc. as I will be compiling the code.  
                When you generate free form text, prefix the response with 'TEXT:'
            """;

            _sandbox = new Sandbox();
            _sandbox.AllowType(assistant);
        }

        public async Task ProcessAsync(string request)
        {
            int retries = 5;
            var prompt = new Prompt(_context);
            prompt.Add($"Give me a response for prompt: {request}");

            while (retries-- > 0)
            {
                string response = await ai.GetAnswerAsync(prompt);
                if (_log) Cli.WriteLine("LOG: " + response, ConsoleColor.DarkBlue);

                if (response.StartsWith("TEXT:"))
                {
                    var text = response.Substring("TEXT:".Length);
                    text = text.TrimStart(' ');
                    _noMatch(text);
                    return;
                }
                var error = ExecutionRuntime.ExecuteCode(response, _sandbox);
                if (error == null) return;
                if (_log) Cli.WriteLine("LOG: " + error, ConsoleColor.DarkBlue);
                prompt.Add($"I got the following error {error} when compiling the code. Can you fix the code you provided previously?", ChatRole.User);
            }
        }
    }
}
