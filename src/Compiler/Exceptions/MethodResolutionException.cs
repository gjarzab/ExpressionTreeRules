namespace Compiler.Exceptions
{
    public class MethodResolutionException(string message, Exception innerException)
        : Exception(message, innerException);
}