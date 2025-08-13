namespace Compiler.Services.Parsers;

public interface ILiteralParser<T>
{
    static abstract string TypeName { get; }
    static abstract bool TryParse(string input, out T value);
}