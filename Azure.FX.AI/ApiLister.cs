// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace Azure.FX.AI.Runtime;

static class ApiLister
{
    static Queue<Type> todo = new Queue<Type>();
    static List<Type> done = new List<Type>();

    public static string CreateApiListing(params Type[] types)
    {
        foreach (var type in types) todo.Enqueue(type);
        var output = new StringBuilder();
        while(todo.Count>0){
            var next = todo.Dequeue();
            done.Add(next);
            EmitType(output, next);
        }
        Debug.WriteLine(output);
        return output.ToString();
    }

    private static void EmitType(StringBuilder output, Type type)
    {
        if (type.IsEnum) EmitEnum(output, type);
        else EmitClass(output, type);
    }

    private static void EmitClass(StringBuilder output, Type classType)
    {
        output.Append($"public static class {classType.Name} {{\n");
        foreach (var method in classType.GetMethods(BindingFlags.Static | BindingFlags.Public))
        {
            output.Append($"\tpublic static {method.ReturnType.Name} {method.Name}(");
            bool first = true;
            foreach (var parameter in method.GetParameters())
            {
                var ptype = parameter.ParameterType;
                if (ptype.IsEnum && !done.Contains(ptype)) { todo.Enqueue(ptype); }

                if (!first) output.Append(", ");
                first = false;
                output.Append(ptype.Name);
                output.Append(" ");
                output.Append(parameter.Name);
            }
            output.Append(");");
        }
        output.Append("}");
    }

    private static void EmitEnum(StringBuilder output, Type enumType)
    {
        output.Append($"public enum {enumType.Name} {{\n");
        bool first = true;
        foreach (var member in Enum.GetNames(enumType))
        {
            if (!first) output.Append(",\n");
            first = false;
            output.Append($"\t{member}");

        }
        output.Append("}");
    }
}
