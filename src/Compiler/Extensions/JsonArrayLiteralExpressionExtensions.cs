using Compiler.Exceptions;
using Newtonsoft.Json.Linq;

namespace Compiler.Extensions
{
    public static class JsonArrayLiteralExpressionExtensions
    {
        public static JArray GetArrayElementsArray(this JObject expression)
        {
            var result = (JArray)expression["elements"];
            if (result == null)
            {
                throw new ExpressionException(
                    "Expected value for property 'elements' is missing");
            }

            return result;
        }
    }
}
