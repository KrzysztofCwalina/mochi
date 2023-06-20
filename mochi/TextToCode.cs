
using Azure.AI.OpenAI;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;

static partial class Program
{
    public class TextToCode
    {
        AIServices ai = new AIServices();
        string _context;
        public bool Confirm { get; set; } = false;

        public TextToCode(params Type[] actions)
        {
            string api = ExtractSchema(actions);

            // this is the OpenAI system message
            _context = $$"""
                You are an expert C# programmer. 
                You have the following C# API available: {{api}}. 
                When you show me code, I want just the calling code. No markup, no markdown, not comments, etc.
            """;
        }

        private static string ExtractSchema(params Type[] actions)
        {
            var schema = new StringBuilder();
            foreach(var type in actions)
            {
                schema.Append($"public class {type.Name} {{\n");
                foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.Public))
                {
                    schema.Append($"\tpublic static {method.Name}(");
                    bool first = true;
                    foreach(var parameter in method.GetParameters())
                    {
                        if (!first) schema.Append(", ");
                        first = false;
                        schema.Append(parameter.ParameterType.Name);
                        schema.Append(" ");
                        schema.Append(parameter.Name);
                    }
                    schema.Append(");");
                }
                schema.Append("}");
            }
            //Console.WriteLine( schema.ToString() );
            return schema.ToString();
        }

        internal async Task ExecuteAsync(string request)
        {
            int retries = 5;
            var prompt = new Prompt(_context);
            prompt.Add($"Show me one line of code calling the API to compute {request}.");

            while (retries-- > 0)
            {
                string response = await ai.GetAnswerAsync(prompt);
                Console.WriteLine("LOG: " + response);
                var error = MakeCall(response);
                if (error == null) return;
                
                prompt.Add($"I got the following error {error}. Can you fix the code you provided previously?", ChatRole.User);          
            }
        }

        public string MakeCall(string source)
        {
            var s = $$"""
                using System;

                public static class Executor {
                    public static void RunAction() {
                        {{source}}
                    }
                }
                """;

            if (Confirm)
            {
                Console.WriteLine();
                Console.WriteLine(s);
                Console.Write("Run? [y/n] : ");
                var key = Console.ReadKey();
                Console.WriteLine();
                if (key.KeyChar != 'y') return null;
                
            }

            SyntaxTree tree = CSharpSyntaxTree.ParseText(s);

            string basePath = Path.GetDirectoryName(typeof(System.Runtime.GCSettings).GetTypeInfo().Assembly.Location);


            var references = new[] {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Code).Assembly.Location),
                MetadataReference.CreateFromFile(Path.Combine(basePath, "System.Runtime.dll"))
            };

            CSharpCompilation compilation = CSharpCompilation.Create(
                "A"  + Guid.NewGuid().ToString(),
                new[] { tree },
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

                using (var dllStream = new MemoryStream())
                {
                    EmitResult emitResult = compilation.Emit(dllStream);
                    if (!emitResult.Success)
                    {
                        Console.WriteLine(source);
                        var error = emitResult.Diagnostics.First().ToString();
                        Console.WriteLine(error);
                        return error;
                    }
                    dllStream.Position = 0;
                    var loaded = AssemblyLoadContext.Default.LoadFromStream(dllStream);
                    var executor = loaded.GetType("Executor");
                    var run = executor.GetMethod("RunAction");
                    run.Invoke(executor, new object[] { });
                    return null;
                }
            }
    }
}
