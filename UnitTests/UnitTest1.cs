using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Mochi;
using System.Reflection;

namespace UnitTests
{
    public class Tests
    {
        [Test]
        public void SandboxTest()
        {
            var call = "Code.GetTime(\"foo\".Substring(4));";
            var source = $$"""
                using System;

                public static class Executor {
                    public static void RunAction() {
                        {{call}}
                    }
                }
                """;

            SyntaxTree tree = CSharpSyntaxTree.ParseText(source);

            string basePath = Path.GetDirectoryName(typeof(System.Runtime.GCSettings).GetTypeInfo().Assembly.Location);

            var references = new[] {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Code).Assembly.Location),
                MetadataReference.CreateFromFile(Path.Combine(basePath, "System.Runtime.dll"))
            };

            var name = "A" + Guid.NewGuid().ToString();
            CSharpCompilation compilation = CSharpCompilation.Create(
                name,
                new[] { tree },
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            using (var dllStream = new MemoryStream())
            {
                EmitResult emitResult = compilation.Emit(dllStream);
                Assert.IsTrue(emitResult.Success);

                dllStream.Position = 0;
                Sandbox.AssertSafeToExecute(dllStream);
            }
        }
    }
}