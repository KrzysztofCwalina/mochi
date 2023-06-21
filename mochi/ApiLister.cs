using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace mochi
{
    internal class ApiLister
    {
        public static string CreateApiListing(params Type[] actions)
        {
            var schema = new StringBuilder();
            foreach (var type in actions)
            {
                schema.Append($"public class {type.Name} {{\n");
                foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.Public))
                {
                    schema.Append($"\tpublic static {method.Name}(");
                    bool first = true;
                    foreach (var parameter in method.GetParameters())
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
            Debug.WriteLine(schema);
            return schema.ToString();
        }
    }
}
