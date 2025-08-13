using Newtonsoft.Json.Linq;

namespace Compiler.Extensions
{
    public static class JsonMemberAccessExpressionExtensions
    {
        public static string GetMemberAccessPath(this JObject expression)
        {
            return JsonHelper.GetStringProperty(expression, "path");
        }
    }
}
