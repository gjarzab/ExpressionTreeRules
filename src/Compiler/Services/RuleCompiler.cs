using Compiler.Exceptions;
using Compiler.Extensions;
using Compiler.Options;
using Core;
using Newtonsoft.Json.Linq;

namespace Compiler.Services
{
    public class RuleCompiler(
        MethodResolutionOptions methodResolutionOptions,
        LiteralExpressionCompiler literalExpressionCompiler)
    {
        private readonly RuleExpressionCompiler _expressionCompiler = new(methodResolutionOptions, literalExpressionCompiler);

        public CompiledRule CompileRule<TContext>(JObject rule) where TContext : IContext
        {
            var name = rule.GetRuleName() ?? "Unnamed Rule";
            var condition = rule.GetCondition();
            var actions = rule.GetActions();

            if (condition == null)
            {
                throw new ExpressionException("Rule must have a 'condition' property");
            }

            if (actions == null)
            {
                throw new ExpressionException("Rule must have an 'actions' array property");
            }

            var compiledCondition = _expressionCompiler.Compile<TContext>(condition);

            var compiledActions = new List<CompiledAction>();
            foreach (var actionJson in actions.Cast<JObject>())
            {
                var compiledAction = _expressionCompiler.CompileAction<TContext>(actionJson);
                compiledActions.Add(compiledAction);
            }

            return new CompiledRule(name, compiledCondition, compiledActions);
        }
    }
}
