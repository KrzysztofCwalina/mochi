using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.Loader;

namespace Mochi;

internal class ExecutionRuntime
{
    public static string MakeCall(string methodBody, Func<string, bool> allow = default)
    {
        var s = $$"""
            using System;

            public static class Executor {
                public static void RunAction() {
                    {{methodBody}}
                }
            }
         """;

        SyntaxTree tree = CSharpSyntaxTree.ParseText(s);

        string basePath = Path.GetDirectoryName(typeof(System.Runtime.GCSettings).GetTypeInfo().Assembly.Location);

        var references = new[] {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Code).Assembly.Location),
            MetadataReference.CreateFromFile(Path.Combine(basePath, "System.Runtime.dll"))
        };

        CSharpCompilation compilation = CSharpCompilation.Create(
            "A" + Guid.NewGuid().ToString(),
            new[] { tree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        using (var dllStream = new MemoryStream())
        {
            EmitResult emitResult = compilation.Emit(dllStream);
            if (!emitResult.Success)
            {
                Debug.WriteLine(methodBody);
                var error = emitResult.Diagnostics.First().ToString();
                Debug.WriteLine(error);
                return error;
            }

            dllStream.Position = 0;
            try
            {
                Sandbox.AssertSafeToExecute(dllStream, allow);
            }
            catch (SandboxEscapedException ex)
            {
                Debug.WriteLine(ex.Message);
                return ex.Message;
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
