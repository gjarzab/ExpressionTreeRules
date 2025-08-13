namespace Metadata
{
    [AttributeUsage(AttributeTargets.Method)]
    public class MethodDescriptionAttribute : Attribute
    {
        public readonly string Description;

        public MethodDescriptionAttribute(string description)
        {
            Description = description;
        }
    }
}