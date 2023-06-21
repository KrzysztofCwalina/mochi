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

        public TextToCode(Type assistant)
        {
            _assistant = assistant;
            string api = ApiLister.CreateApiListing(_assistant);

            // this is the OpenAI system message
            _context = $$"""
                You have the following C# API available: {{api}}. 
                I will ask you to call one of the avaliable methods that corresponds the closest to your answer. 
                When you generate C# code, I want just code; no markup, no markdown, no commentary, etc. as I will be compiling the code.
                If none of the specific APIs match, call Assistant.Default and pass your free form textual response (not code) as the method's argument.
            """;

            _sandbox = new Sandbox();
            _sandbox.AllowType(assistant);
        }

        public async Task ExecuteAsync(string request)
        {
            int retries = 5;
            var prompt = new Prompt(_context);
            prompt.Add($"Show me code (just code) to compute {request}");

            while (retries-- > 0)
            {
                string response = await ai.GetAnswerAsync(prompt);
                Cli.WriteLine("LOG: " + response, ConsoleColor.DarkBlue);
                var error = ExecutionRuntime.MakeCall(response, _sandbox);
                if (error == null) return;
                Cli.WriteLine("LOG: " + error, ConsoleColor.DarkBlue);
                prompt.Add($"I got the following error {error} when compiling the code. Can you fix the code you provided previously?", ChatRole.User);
            }
        }
    }
}
