namespace Metadata
{
    public class ContextDescription(
        string contextName,
        IReadOnlyList<ContextMemberDescription> members,
        IReadOnlyList<ContextMemberMethodDescription> methods)
    {
        public string ContextName { get; } = contextName;
        public IReadOnlyList<ContextMemberDescription> Members { get; } = members;
        public IReadOnlyList<ContextMemberMethodDescription> Methods { get; } = methods;
        public IReadOnlyDictionary<string, IReadOnlyList<string>> OperatorNamesByType { get; } = new Dictionary<string, IReadOnlyList<string>>
        {
            { "BOOL", ["Equal", "AndAlso", "OrElse", "NotEqual"] },
            { "STRING", ["Equal"] },
            { "INT", ["AddChecked", "SubtractChecked", "LessThan", "LessThanOrEqual", "GreaterThan", "GreaterThanOrEqual", "Equal", "NotEqual"] }
        };
    }
}