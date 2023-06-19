
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;

static partial class Program
{
    public class Clu
    {
        AIServices ai = new AIServices();
        string _context;
        public bool Confirm { get; set; } = true;

        public Clu(params Type[] actions)
        {
            string api = ExtractSchema(actions);

            // this is the OpenAI system message
            _context = $$"""
                You are an expert C# programmer. 
                You have the following C# API awaliable: {{api}}. 
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
                    schema.Append($"\tpublic static {method.Name}();\n");
                }
                schema.Append("}");
            }
            Console.WriteLine( schema.ToString() );
            return schema.ToString();
        }

        internal async Task ExecuteAsync(string request)
        {
            var prompt = new Prompt(_context);
            prompt.Add($"Show me one line of code calling the API to compute {request}");

            string response = await ai.GetResponseAsync(prompt);

            MakeCall(response);
        }

        public void MakeCall(string source)
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
                if (key.KeyChar != 'y') return;
                
            }

            SyntaxTree tree = CSharpSyntaxTree.ParseText(s);

            string basePath = Path.GetDirectoryName(typeof(System.Runtime.GCSettings).GetTypeInfo().Assembly.Location);


            var references = new[] {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Assistant).Assembly.Location),
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
                        Console.WriteLine(emitResult.Diagnostics.First().ToString());
                        return;
                    }
                    dllStream.Position = 0;
                    var loaded = AssemblyLoadContext.Default.LoadFromStream(dllStream);
                    var executor = loaded.GetType("Executor");
                    var run = executor.GetMethod("RunAction");
                    run.Invoke(executor, new object[] { });
                }

            }
    }
}
