// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.Loader;

namespace Mochi;

static class ExecutionRuntime
{
    public static string ExecuteCode(string methodBody, Sandbox sandbox)
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

        var references = new List<MetadataReference>();
        references.Add(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));
        references.Add(MetadataReference.CreateFromFile(Path.Combine(basePath, "System.Runtime.dll")));
        foreach (string reference in sandbox.AssemblyReferences) {
            references.Add(MetadataReference.CreateFromFile(reference));
        }      

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
                sandbox.AssertSafeToExecute(dllStream);
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
