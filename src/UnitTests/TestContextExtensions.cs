namespace UnitTests;

public static class TestContextExtensions
{
    public static bool IsAdult(this TestContext context, int ageThreshold)
    {
        return context.TestField1 == "Adult" && context.TestMethod1(ageThreshold);
    }

    public static bool IsEven(this int number)
    {
        return number % 2 == 0;
    }
}