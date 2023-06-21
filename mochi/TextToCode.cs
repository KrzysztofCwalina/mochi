
using Azure.AI.OpenAI;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;
using mochi;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;

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
                You are an expert C# programmer. 
                You have the following C# API available: {{api}}. 
                When you show me code, I want just the calling code. No markup, no markdown, not comments, etc.
            """;
        }

        public async Task ExecuteAsync(string request, Func<string, bool> allow = default)
        {
            int retries = 5;
            var prompt = new Prompt(_context);
            prompt.Add($"Show me one line of code calling the API to compute {request}.");

            while (retries-- > 0)
            {
                string response = await ai.GetAnswerAsync(prompt);
                Console.WriteLine("LOG: " + response);
                var error = ExecutionRuntime.MakeCall(response, allow);
                if (error == null) return;
                Console.WriteLine("LOG: " + error);
                prompt.Add($"I got the following error {error}. Can you fix the code you provided previously?", ChatRole.User);
            }
        }
    }
}
