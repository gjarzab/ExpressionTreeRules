namespace Metadata
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
    public class OmitFromDescriptionAttribute : Attribute
    {
    }
}