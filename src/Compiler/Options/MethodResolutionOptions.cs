namespace Compiler.Options
{
    public sealed class MethodResolutionOptions
    {
        public readonly Type ContextType;
        public readonly Type ExtensionMethodsType;

        private MethodResolutionOptions(Type contextTypeType, Type extensionMethodsType)
        {
            ContextType = contextTypeType;
            ExtensionMethodsType = extensionMethodsType;
        }

        public static MethodResolutionOptions Default(Type contextType)
        {
            return new MethodResolutionOptions(contextType, null);
        }

        public static MethodResolutionOptions WithExtensionMethods(Type contextType, Type extensionMethodsType)
        {
            return new MethodResolutionOptions(contextType, extensionMethodsType);
        }
    }
}