using Newtonsoft.Json.Linq;

namespace Compiler.Extensions
{
    public static class JsonLiteralExpressionExtensions
    {
        public static string GetLiteralKind(this JObject expression)
        {
            return JsonHelper.GetStringProperty(expression, "kind");
        }

        public static string GetLiteralValue(this JObject expression)
        {
            return JsonHelper.GetStringProperty(expression, "value");
        }
    }
}
