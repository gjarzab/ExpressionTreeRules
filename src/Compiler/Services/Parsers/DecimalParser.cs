namespace Compiler.Services.Parsers;

public readonly struct DecimalParser : ILiteralParser<decimal>
{
    public static string TypeName => "DECIMAL";
    public static bool TryParse(string input, out decimal value) => decimal.TryParse(input, out value);
}