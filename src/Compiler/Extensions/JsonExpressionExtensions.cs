using Newtonsoft.Json.Linq;

namespace Compiler.Extensions
{
    public static class JsonExpressionExtensions
    {
        public static string GetExpressionType(this JObject expression)
        {
            return JsonHelper.GetStringProperty(expression, "expressionType");
        }

        public static string GetOperator(this JObject expression)
        {
            return JsonHelper.GetStringProperty(expression, "operator");
        }

        public static string GetRuleName(this JObject rule)
        {
            return JsonHelper.GetStringProperty(rule, "name");
        }

        public static JObject GetCondition(this JObject rule)
        {
            return rule["condition"] as JObject;
        }

        public static JArray GetActions(this JObject rule)
        {
            return rule["actions"] as JArray;
        }
    }
}
