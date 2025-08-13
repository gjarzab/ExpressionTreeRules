namespace Compiler.Exceptions
{
    public sealed class LiteralParseException : Exception
    {
        public LiteralParseException(string message) : base(message)
        {
        }

        public LiteralParseException(string message, Exception e) : base(message, e)
        {
        }
    }
}