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
    const string ROOT_TYPE = "Executor";
    const string ROOT_METHOD = "RunAction";
    const string USINGS = "using System;";

    public static string ExecuteMethodBody(string methodBody, Sandbox sandbox)
    {
        SyntaxTree sources = CreateSourceCode(methodBody);
        var references = sandbox.AssemblyReferences;

        var dll = new MemoryStream();
        if (!TryCompile(sources, references, dll, out string errors))
        {
            return errors;
        }
        dll.Position = 0;

        try
        {
            sandbox.AssertSafeToExecute(dll);
            dll.Position = 0;
        }
        catch (SandboxEscapedException ex)
        {
            Debug.WriteLine(ex.Message);
            return ex.Message;
        }

        RunAndUnload(dll);
        return null;
    }

    private static bool TryCompile(SyntaxTree sources, IEnumerable<string> references, Stream output, out string errors)
    {
        string clr = Path.GetDirectoryName(typeof(object).GetTypeInfo().Assembly.Location);

        var allReferences = new List<MetadataReference>();
        allReferences.Add(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));
        allReferences.Add(MetadataReference.CreateFromFile(Path.Combine(clr, "System.Runtime.dll")));
        foreach (string reference in references) {
            allReferences.Add(MetadataReference.CreateFromFile(reference));
        }

        var assemblyName = "A" + Guid.NewGuid().ToString();
        CSharpCompilation compilation = CSharpCompilation.Create(
            assemblyName,
            new[] { sources },
            allReferences,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        EmitResult emitResult = compilation.Emit(output);
        if (!emitResult.Success)
        {
            errors = string.Join(" ", emitResult.Diagnostics);
            return false;
        }

        errors = null;
        return true;     
    }

    private static SyntaxTree CreateSourceCode(string methodBody)
    {
        var s = $$"""
            {{USINGS}}

            public static class {{ROOT_TYPE}} {
                public static void {{ROOT_METHOD}}() {
                    {{methodBody}}
                }
            }
         """;

        SyntaxTree tree = CSharpSyntaxTree.ParseText(s);
        return tree;
    }

    private static void RunAndUnload(Stream dll)
    {
        var context = new AssemblyLoadContext(null, isCollectible: true);
        try
        {
            var loaded = context.LoadFromStream(dll);
            var type = loaded.GetType(ROOT_TYPE);
            var method = type.GetMethod(ROOT_METHOD);
            method.Invoke(type, Array.Empty<object>());
        }
        finally
        {
            context.Unload();
        }
    }
}
