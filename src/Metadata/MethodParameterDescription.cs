namespace Metadata
{
    public class MethodParameterDescription(string name, string type, string description, string valueProviderEndpoint)
    {
        public string Name { get; } = name;
        public string Type { get; } = type;
        public string Description { get; } = description;
        public string ValueProviderEndpoint { get; } = valueProviderEndpoint;
    }
}