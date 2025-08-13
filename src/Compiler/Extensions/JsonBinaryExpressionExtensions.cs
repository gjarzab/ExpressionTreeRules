using Newtonsoft.Json.Linq;

namespace Compiler.Extensions
{
    public static class JsonBinaryExpressionExtensions
    {
        public static JObject GetLeft(this JObject binaryExpression)
        {
            return (JObject)binaryExpression["left"];
        }

        public static JObject GetRight(this JObject binaryExpression)
        {
            return (JObject)binaryExpression["right"];
        }
    }
}
