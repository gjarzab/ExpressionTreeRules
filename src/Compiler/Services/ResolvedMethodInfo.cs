using System.Reflection;

namespace Compiler.Services;

public sealed class ResolvedMethodInfo(MethodInfo method, bool isExtensionMethod)
{
    public MethodInfo Method { get; } = method;
    public bool IsExtensionMethod { get; } = isExtensionMethod;
}