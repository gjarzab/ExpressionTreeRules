namespace UnitTests;

public static class TestExtensionMethods
{
    public static void ExtensionMethod(this TestMethodProvider provider) { }
    public static void ExtensionMethodWithArg(this TestMethodProvider provider, int arg) { }
}