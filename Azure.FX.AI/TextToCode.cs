// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Azure.AI.OpenAI;
using Azure.FX.AI.Runtime;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Azure.FX.AI;

public class TextToCode
{
    const string NATURAL_LANGUAGE = "NATURAL_LANGUAGE:";
    readonly AIServices ai = new AIServices();
    readonly Type _assistant;
    readonly string _context;
    readonly Sandbox _sandbox;

    public Action<string> NoMatchFallback { get; set; }
    public TraceSource Logger { get; } = new TraceSource("Azure.FX.AI");

    public TextToCode(Type assistant)
    {
        _assistant = assistant;

        string api = ApiLister.CreateApiListing(_assistant);

        // this is the OpenAI system message
        _context = $$"""
            You have the following C# APIs provided: {{api}}. 
            Your answers must be either C# source code calling one of the provided APIs, or a natural language text, if none of the provided APIs match.
            If you generate C# code, it must be just code; no markup, no markdown, no commentary, etc., as the code will be compiled without changes.
            If you generate C# code, you must call at least one of the APIs provided.
            If you generate a natural language answer, prefix the response with '{{NATURAL_LANGUAGE}}'.
            If I send you a compilation error, don't appologize, or add any natural language commentary. Just reply with fixed C# code. 
            Don't ever reply with the same text as the question/prompt.
        """;

        _sandbox = new Sandbox();
        _sandbox.AllowType(assistant);
    }

    public async Task<bool> ProcessAsync(string request)
    {
        int retries = 5;
        var prompt = new Prompt(_context);
        prompt.Add($"{request}");

        while (retries-- > 0)
        {
            string response = await ai.GetAnswerAsync(prompt);
            
            if (response.StartsWith(NATURAL_LANGUAGE))
            {
                var noMatch = NoMatchFallback;
                if (noMatch == null) return false;
                var text = response.Substring(NATURAL_LANGUAGE.Length);
                text = text.TrimStart(' ');
                noMatch(text);
                return true;
            }

            var logger = Logger;

            if (logger!=default) logger.TraceInformation($"EXECUTING: {response}");
            var error = ExecutionRuntime.ExecuteMethodBody(response, _sandbox);
            if (error == null) return true;
            if (logger != default) logger.TraceEvent(TraceEventType.Warning, 0, $"ERROR: {error}");
            prompt.Add($"I got the following error {error} when compiling the code. Can you fix the code you provided previously?", ChatRole.User);
        }

        return false;
    }
}
