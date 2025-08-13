namespace Metadata
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public class ParameterDescriptionAttribute(string description) : Attribute
    {
        public readonly string Description = description;
    }
}