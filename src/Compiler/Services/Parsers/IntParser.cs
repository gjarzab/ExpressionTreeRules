namespace Compiler.Services.Parsers;

public readonly struct IntParser : ILiteralParser<int>
{
    public static string TypeName => "INT";
    public static bool TryParse(string input, out int value) => int.TryParse(input, out value);
}