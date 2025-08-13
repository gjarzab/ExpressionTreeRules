namespace Core
{
    public class CompiledRule(string name, CompiledCondition condition, IReadOnlyList<CompiledAction> actions)
    {
        public string Name { get; } = name;
        private CompiledCondition Condition { get; } = condition;
        private IReadOnlyList<CompiledAction> Actions { get; } = actions;

        public void EvaluateAndExecute(IContext context)
        {
            if (!Condition.Evaluate(context))
            {
                return;
            }
            
            foreach (var action in Actions)
            {
                action.Execute(context);
            }
        }
    }
}
