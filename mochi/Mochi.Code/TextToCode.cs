// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Azure.AI.OpenAI;
using Azure.FX.AI;
using Azure.FX.Tooling;
using Mochi;

namespace Azure.FX.AI;

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
                Your answers must be either C# code calling one of the provided APIs, or free form textual answer, if none of the APIs match.
                When you generate C# code, I want just code; no markup, no markdown, no commentary, etc. as I will be compiling the code.  
                When you generate free form text, prefix the response with 'TEXT:'.
                if you return code, you must call at least one API provided.
                If I send you a compilation error, don't appologize or add any commentary. Just reply with fixed code. 
            """;

        _sandbox = new Sandbox();
        _sandbox.AllowType(assistant);
    }

    public Action<string> NoMatchFallback { get; set; }
    public bool Log { get; set; } = false;

    public async Task<bool> ProcessAsync(string request)
    {
        int retries = 5;
        var prompt = new Prompt(_context);
        prompt.Add($"Give me a response for prompt: {request}");

        while (retries-- > 0)
        {
            string response = await ai.GetAnswerAsync(prompt);
            if (Log) Cli.WriteLine("LOG: " + response, ConsoleColor.DarkBlue);

            if (response.StartsWith("TEXT:"))
            {
                var noMatch = NoMatchFallback;
                if (noMatch == null) return false;

                var text = response.Substring("TEXT:".Length);
                text = text.TrimStart(' ');
                noMatch(text);
                return true;
            }
            var error = ExecutionRuntime.ExecuteCode(response, _sandbox);
            if (error == null) return true;
            if (Log) Cli.WriteLine("LOG: " + error, ConsoleColor.DarkBlue);
            prompt.Add($"I got the following error {error} when compiling the code. Can you fix the code you provided previously?", ChatRole.User);
        }

        return false;
    }
}
