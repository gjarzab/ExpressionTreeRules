using System.Reflection;
using Compiler.Exceptions;
using Compiler.Options;

namespace Compiler.Services
{
    public static class MethodResolver
    {
        private static readonly BindingFlags MethodBindingFlags = BindingFlags.Public |
                                                                  BindingFlags.Instance |
                                                                  BindingFlags.Static |
                                                                  BindingFlags.FlattenHierarchy;
        private static MethodInfo ResolveMethod(Type declaringType, string methodName, Type[] argumentTypes)
        {
            var candidate = declaringType?.GetMethod(methodName, MethodBindingFlags, argumentTypes);
            return candidate;
        }

        public static ResolvedMethodInfo ResolveMethod(string methodName, Type instanceType, Type[] argumentTypes, MethodResolutionOptions methodResolutionOptions)
        {
            // Try to resolve as an instance method on instanceType
            var result = ResolveMethod(instanceType, methodName, argumentTypes);
            if (result != null)
            {
                return new ResolvedMethodInfo(result, false); // Not an extension method
            }

            // Try to resolve as an extension method if not found as an instance method
            if (methodResolutionOptions.ExtensionMethodsType != null && instanceType != null)
            {
                var extensionMethodArgumentTypes = new Type[argumentTypes.Length + 1];
                extensionMethodArgumentTypes[0] = instanceType;
                Array.Copy(argumentTypes, 0, extensionMethodArgumentTypes, 1, argumentTypes.Length);

                result = ResolveMethod(methodResolutionOptions.ExtensionMethodsType, methodName, extensionMethodArgumentTypes);
                if (result != null)
                {
                    return new ResolvedMethodInfo(result, true); // Is an extension method
                }
            }

            throw new MethodResolutionException($"Failed to resolve method '{methodName}' with arguments: {string.Join(", ", argumentTypes.Select(a => a.Name))}", null);
        }
    }
}