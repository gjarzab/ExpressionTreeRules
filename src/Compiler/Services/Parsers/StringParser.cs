namespace Compiler.Services.Parsers;

public readonly struct StringParser : ILiteralParser<string>
{
    public static string TypeName => "STRING";
    public static bool TryParse(string input, out string value)
    {
        value = input;
        return true;
    }
}