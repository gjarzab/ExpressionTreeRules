namespace Metadata
{
    public class ContextMemberMethodDescription(
        string name,
        string description,
        string returnType,
        IReadOnlyList<MethodParameterDescription> parameters)
    {
        public string Name { get; } = name;
        public string Description { get; } = description;
        public string ReturnType { get; } = returnType;
        public IReadOnlyList<MethodParameterDescription> Parameters { get; } = parameters;
    }
}