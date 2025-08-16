using Core;
using Metadata;

namespace UnitTests
{
    public class TestContext : IContext
    {
        public string ContextName => nameof(TestContext);

        public string TestField1 { get; set; }
        public InnerContext Inner { get; set; }
        public DateTime DateField { get; set; }

        public bool TestMethod1(int someId)
        {
            return someId > 10;
        }

        public bool TestMethodWithArray(int[] values)
        {
            return values.Length == 2;
        }

        public string TestMagicValueMethod()
        {
            return "a42";
        }

        [MethodDescription("A test method with a value provider.")]
        public bool TestMethodWithValueProvider([ParameterValueProvider("/api/v1/roles")] string role)
        {
            return !string.IsNullOrEmpty(role);
        }

        public class InnerContext
        {
            public int Value { get; set; }
            public bool IsPositive() => Value > 0;
        }

        [OmitFromDescription]
        public string OmittedProperty
        {
            get;
            set;
        }
    }
}
