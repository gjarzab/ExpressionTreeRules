using Compiler.Exceptions;
using Newtonsoft.Json.Linq;

namespace Compiler.Extensions
{
    public static class JsonCallExpressionExtensions
    {
        public static string GetMethodName(this JObject expression)
        {
            return JsonHelper.GetStringPath(expression, "method.name",
                "Expected value for property 'method.name' is missing");
        }

        public static string GetMethodXParameter(this JObject expression)
        {
            return (string)expression.SelectToken("method.x");
        }

        public static JArray GetMethodArgumentsArray(this JObject expression)
        {
            var result = (JArray)expression["arguments"];
            if (result == null)
            {
                throw new ExpressionException(
                    "Expected value for property 'arguments' is missing");
            }

            return result;
        }
    }
}
