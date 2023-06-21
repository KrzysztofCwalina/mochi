﻿using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace mochi
{
    class SandboxLeftException : Exception
    {
        public SandboxLeftException(string message) : base(message)
        {
        }
    }
    public class Sandbox
    {
        public static void AssertSafeToExecute(Stream assemblyStream)
        {
            var peReader = new PEReader(assemblyStream);
            MetadataReader reader = peReader.GetMetadataReader();
            foreach(MemberReferenceHandle mrh in reader.MemberReferences)
            {
                var mr = reader.GetMemberReference(mrh);
                TypeReferenceHandle typeReferenceHandle = (TypeReferenceHandle)mr.Parent;
                var typeReference = reader.GetTypeReference(typeReferenceHandle);
                var assemblyReferenceHandle = (AssemblyReferenceHandle)typeReference.ResolutionScope;
                var assemblyReference = reader.GetAssemblyReference(assemblyReferenceHandle);

                string assemblyName = reader.GetString(assemblyReference.Name);
                string typeNamespace = reader.GetString(typeReference.Namespace);
                var typeName = reader.GetString(typeReference.Name);

                var fullTypeName = $"{assemblyName}.{typeNamespace}.{typeName}";
                if (s_alowedTypes.Contains(fullTypeName))
                {
                    continue;
                }

                var memberName = reader.GetString(mr.Name);
                var fullMemberName = $"{fullTypeName}.{memberName}";

                if (s_alowedMembers.Contains(fullMemberName))
                {
                    continue;
                }

                throw new SandboxLeftException($"You cannot call {fullMemberName}");
            }     
        }

        readonly static string[] s_alowedTypes = new string[] {
            "System.Private.CoreLib.System.Runtime.CompilerServices.CompilationRelaxationsAttribute",
            "System.Private.CoreLib.System.Runtime.CompilerServices.RuntimeCompatibilityAttribute",
            "System.Private.CoreLib.System.Diagnostics.DebuggableAttribute",
            "System.Private.CoreLib.System.Runtime.CompilerServices.CompilerGeneratedAttribute",
            "System.Private.CoreLib.System.AttributeUsageAttribute",
            "System.Private.CoreLib.System.Attribute",
            "mochi..Code",
            "System.Private.CoreLib.System.DateTimeOffset"
        };
        readonly static string[] s_alowedMembers = new string[] {
        };
    }
}