// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace Mochi;

static class ApiLister
{
    public static string CreateApiListing(params Type[] types)
    {
        var listing = new StringBuilder();
        foreach (var type in types)
        {
            listing.Append($"public static class {type.Name} {{\n");
            foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.Public))
            {
                listing.Append($"\tpublic static {method.ReturnType.Name} {method.Name}(");
                bool first = true;
                foreach (var parameter in method.GetParameters())
                {
                    if (!first) listing.Append(", ");
                    first = false;
                    listing.Append(parameter.ParameterType.Name);
                    listing.Append(" ");
                    listing.Append(parameter.Name);
                }
                listing.Append(");");
            }
            listing.Append("}");
        }
        Debug.WriteLine(listing);
        return listing.ToString();
    }
}
