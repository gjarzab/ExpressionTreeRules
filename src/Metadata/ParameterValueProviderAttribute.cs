namespace Metadata
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public class ParameterValueProviderAttribute(string apiEndpoint) : Attribute
    {
        public readonly string ApiEndpoint = apiEndpoint;
    }
}