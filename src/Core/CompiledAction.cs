namespace Core
{
    public class CompiledAction(Action<IContext> action, Type contextType, string expression, int compileTimeMs)
    {
        public void Execute(IContext context)
        {
            action(context);
        }

        public override string ToString()
        {
            return $"Context type '{contextType.Name}' expression: '{expression}' compiled in {compileTimeMs}ms";
        }
    }
}
