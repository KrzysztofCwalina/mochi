// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Linq;

namespace Mochi;

class Sandbox
{
    readonly static string[] s_alowedBclTypes = new string[] {
        "System.Private.CoreLib,System.Runtime.CompilerServices.CompilationRelaxationsAttribute",
        "System.Private.CoreLib,System.Runtime.CompilerServices.RuntimeCompatibilityAttribute",
        "System.Private.CoreLib,System.Diagnostics.DebuggableAttribute",
        "System.Private.CoreLib,System.Runtime.CompilerServices.CompilerGeneratedAttribute",
        "System.Private.CoreLib,System.AttributeUsageAttribute",
        "System.Private.CoreLib,System.Attribute",
        "System.Private.CoreLib,System.DateTimeOffset",
        "System.Private.CoreLib,System.DateTime",
        "System.Private.CoreLib,System.Random"
    };
    readonly static string[] s_alowedBclMembers = new string[] {
    };

    readonly List<string> additionalAllowedTypes = new List<string>();
    readonly List<string> additionalAssemblyReferences = new List<string>();

    public void AllowType(Type type)
    {
        var typeName = type.FullName;
        if (type.Namespace == null) {
            typeName = "global::" + type.Name;
        }
        var assemblyName = type.Assembly.GetName().Name;

        additionalAllowedTypes.Add($"{assemblyName},{typeName}");
        additionalAssemblyReferences.Add(type.Assembly.Location);
    }

    public Func<string, bool> ShouldAllow { get; set; }
    public IEnumerable<string> AssemblyReferences => additionalAssemblyReferences;

    public void AssertSafeToExecute(Stream assemblyStream)
    {
        var allowCallback = ShouldAllow;
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

            var fullTypeName = $"{assemblyName},{typeNamespace}.{typeName}";
            if (string.IsNullOrEmpty(typeNamespace))
            {
                fullTypeName = $"{assemblyName},global::{typeName}";
            }

            if (additionalAllowedTypes.Contains(fullTypeName))
            {
                continue;
            }

            if (s_alowedBclTypes.Contains(fullTypeName))
            {
                continue;
            }

            var memberName = reader.GetString(mr.Name);
            var fullMemberName = $"{fullTypeName}.{memberName}";

            if (s_alowedBclMembers.Contains(fullMemberName))
            {
                continue;
            }

            if (allowCallback != null) if (allowCallback(fullMemberName)) continue;

            throw new SandboxEscapedException($"You cannot call {fullMemberName}");
        }     
    }
}

class SandboxEscapedException : Exception
{
    public SandboxEscapedException(string message) : base(message)
    {}
}
