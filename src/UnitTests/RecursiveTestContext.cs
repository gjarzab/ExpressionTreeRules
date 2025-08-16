
using Core;

namespace UnitTests
{
    public class RecursiveContext : IContext
    {
        public string ContextName => nameof(RecursiveContext);
        public RecursiveContext Parent { get; set; }
        public string Name { get; set; }
    }

    public class CoRecursiveA : IContext
    {
        public string ContextName => nameof(CoRecursiveA);
        public CoRecursiveB B { get; set; }
    }

    public class CoRecursiveB
    {
        public CoRecursiveA A { get; set; }
    }
}
