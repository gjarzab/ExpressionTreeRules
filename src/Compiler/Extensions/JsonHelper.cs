using Compiler.Exceptions;
using Newtonsoft.Json.Linq;

namespace Compiler.Extensions
{
    internal static class JsonHelper
    {
        internal static string GetStringProperty(JObject expression, string property, string exceptionMessage = null)
        {
            ArgumentNullException.ThrowIfNull(expression);

            var result = (string)expression[property];
            if (!string.IsNullOrEmpty(result))
            {
                return result;
            }
            
            var message = string.IsNullOrEmpty(exceptionMessage)
                ? $"Expected value for property '{property}' is missing"
                : exceptionMessage;

            throw new ExpressionException(message);
        }

        internal static string GetStringPath(JObject expression, string property, string exceptionMessage)
        {
            ArgumentNullException.ThrowIfNull(expression);

            var result = (string)expression.SelectToken(property);
            if (string.IsNullOrEmpty(result))
            {
                throw new ExpressionException(exceptionMessage);
            }

            return result;
        }
    }
}
