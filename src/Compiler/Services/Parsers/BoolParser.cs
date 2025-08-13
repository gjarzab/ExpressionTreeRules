namespace Compiler.Services.Parsers;

public readonly struct BoolParser : ILiteralParser<bool>
{
    public static string TypeName => "BOOL";
    public static bool TryParse(string input, out bool value) => bool.TryParse(input, out value);
}