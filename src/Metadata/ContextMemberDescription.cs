namespace Metadata
{
    public class ContextMemberDescription(
        string name,
        string type,
        string path,
        IReadOnlyList<ContextMemberDescription> members,
        IReadOnlyList<ContextMemberMethodDescription> methods)
    {
        public string Name { get; } = name;
        public string Type { get; } = type;
        public string Path { get; } = path;
        public IReadOnlyList<ContextMemberDescription> Members { get; } = members;
        public IReadOnlyList<ContextMemberMethodDescription> Methods { get; } = methods;
    }
}