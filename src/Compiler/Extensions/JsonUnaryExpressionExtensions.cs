using Newtonsoft.Json.Linq;

namespace Compiler.Extensions
{
    public static class JsonUnaryExpressionExtensions
    {
        public static JObject GetOperand(this JObject unaryExpression)
        {
            return (JObject)unaryExpression["expression"];
        }
    }
}
