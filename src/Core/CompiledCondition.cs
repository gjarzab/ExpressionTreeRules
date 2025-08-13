namespace Core
{
    public class CompiledCondition(Func<IContext, bool> predicate, Type contextType, string expression, int compileTimeMs)
    {
        public bool Evaluate(IContext context)
        {
            return predicate(context);
        }

        public override string ToString()
        {
            return $"Context type '{contextType.Name}' expression: '{expression}' compiled in {compileTimeMs}ms";
        }
    }
}