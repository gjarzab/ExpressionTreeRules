using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Compiler.Exceptions;
using Compiler.Extensions;
using Compiler.Options;
using Core;
using FastExpressionCompiler;
using Newtonsoft.Json.Linq;

namespace Compiler.Services
{
    public class RuleExpressionCompiler(
        MethodResolutionOptions methodResolutionOptions,
        LiteralExpressionCompiler literalExpressionCompiler)
    {
        public CompiledCondition Compile(Type contextType, JObject expression)
        {
            if (!contextType.IsAssignableTo(typeof(IContext)))
            {
                throw new ArgumentException($"{nameof(contextType)} must be a type that implements {nameof(IContext)}", nameof(contextType));
            }

            var genericCompileMethod = GetType()
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(_ => _.Name == nameof(Compile) && _.IsGenericMethod && _.GetGenericArguments().Length == 1);

            if (genericCompileMethod == null)
            {
                throw new InvalidOperationException("Unable to resolve Compile method");
            }

            var compileMethod = genericCompileMethod.MakeGenericMethod(contextType);

            try
            {
                var result = (CompiledCondition)compileMethod.Invoke(this, [expression]);
                return result;
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                throw;
            }
        }

        public CompiledCondition Compile<TContext>(JObject expression) where TContext : IContext
        {
            if (typeof(TContext) != methodResolutionOptions.ContextType)
            {
                throw new InvalidOperationException(
                    $"Type of context '{typeof(TContext).Name}' does not match MethodResolutionOptions '{methodResolutionOptions.ContextType.Name}'");
            }

            var stopwatch = Stopwatch.StartNew();

            var typedContextParam = Expression.Parameter(typeof(TContext), "ctx");
            var bodyExpression = BuildExpression(expression, typedContextParam);
            var strongLambda = Expression.Lambda<Func<TContext, bool>>(bodyExpression, typedContextParam);
            var strongFunc = strongLambda.CompileFast();

            stopwatch.Stop();

            return new CompiledCondition(FinalFunc,
                typeof(TContext),
                strongLambda.ToString(),
                (int)stopwatch.ElapsedMilliseconds);

            bool FinalFunc(IContext untypedContext) => strongFunc((TContext)untypedContext);
        }

        public CompiledAction CompileAction<TContext>(JObject expression) where TContext : IContext
        {
            if (typeof(TContext) != methodResolutionOptions.ContextType)
            {
                throw new InvalidOperationException(
                    $"Type of context '{typeof(TContext).Name}' does not match MethodResolutionOptions '{methodResolutionOptions.ContextType.Name}'");
            }

            var stopwatch = Stopwatch.StartNew();

            var typedContextParam = Expression.Parameter(typeof(TContext), "ctx");
            var bodyExpression = BuildExpression(expression, typedContextParam);

            if (bodyExpression.Type != typeof(void))
            {
                throw new ExpressionException($"Action expression must be a void method call, but it returns '{bodyExpression.Type.Name}'");
            }

            var strongLambda = Expression.Lambda<Action<TContext>>(bodyExpression, typedContextParam);
            var strongFunc = strongLambda.CompileFast();

            stopwatch.Stop();

            return new CompiledAction(FinalFunc,
                typeof(TContext),
                strongLambda.ToString(),
                (int)stopwatch.ElapsedMilliseconds);

            void FinalFunc(IContext untypedContext) => strongFunc((TContext)untypedContext);
        }

        private static void AddSupportedExpression(Dictionary<string, ExpressionType> supportedBinaryExpressions, ExpressionType type, string name = null)
        {
            supportedBinaryExpressions.Add(name ?? type.ToString(), type);
        }

        private static readonly Lazy<Dictionary<string, ExpressionType>> SupportedBinaryOperators = new(() =>
        {
            var result = new Dictionary<string, ExpressionType>();
            
            AddSupportedExpression(result, ExpressionType.AddChecked);
            AddSupportedExpression(result, ExpressionType.SubtractChecked);
            
            AddSupportedExpression(result, ExpressionType.Equal);
            AddSupportedExpression(result, ExpressionType.NotEqual);
            
            AddSupportedExpression(result, ExpressionType.GreaterThan);
            AddSupportedExpression(result, ExpressionType.GreaterThanOrEqual);
            
            AddSupportedExpression(result, ExpressionType.LessThan);
            AddSupportedExpression(result, ExpressionType.LessThanOrEqual);
            
            
            // ExpressionType.And, ExpressionType.Or are for bit operations
            AddSupportedExpression(result, ExpressionType.AndAlso);
            AddSupportedExpression(result, ExpressionType.OrElse);

            return result;
        });

        private static readonly Lazy<Dictionary<string, ExpressionType>> SupportedUnaryOperators = new(() =>
        {
            var items = new List<ExpressionType>
            {
                ExpressionType.Not
            };

            return items.ToDictionary(x => x.ToString(), x => x);
        });

        private Expression BuildExpression(JObject expression, Expression contextExpression)
        {
            var expressionType = expression.GetExpressionType();
            return expressionType switch
            {
                RecognizedExpressionTypes.BinaryExpression => BuildBinaryExpression(expression, contextExpression),
                RecognizedExpressionTypes.UnaryExpression => BuildUnaryExpression(expression, contextExpression),
                RecognizedExpressionTypes.CallExpression => BuildCallExpression(expression, contextExpression),
                RecognizedExpressionTypes.MemberAccessExpression => BuildMemberAccessExpression(expression, contextExpression),
                RecognizedExpressionTypes.ArrayLiteralExpression => BuildArrayLiteralExpression(expression),
                RecognizedExpressionTypes.BasicLiteralExpression => BuildBasicLiteralExpression(expression),
                _ => throw new ExpressionException($"Unexpected expression type '{expressionType}'")
            };
        }

        private BinaryExpression BuildBinaryExpression(JObject binaryExpression, Expression contextExpression)
        {
            var operatorName = binaryExpression.GetOperator();

            if (!SupportedBinaryOperators.Value.TryGetValue(operatorName, out var binaryType))
            {
                throw new ExpressionException($"Operator '{operatorName}' is not a supported binary operator");
            }

            var left = binaryExpression.GetLeft();
            var right = binaryExpression.GetRight();

            var leftExpression = BuildExpression(left, contextExpression);
            var rightExpression = BuildExpression(right, contextExpression);

            try
            {
                return Expression.MakeBinary(binaryType, leftExpression, rightExpression);
            }
            catch (InvalidOperationException e) when (e.Message.Contains("The binary operator"))
            {
                throw new ExpressionException(
                    $"The binary operator {binaryType} must have operands of the same type. Operand types used where '{leftExpression.Type.Name}' and '{rightExpression.Type.Name}'");
            }
        }

        private UnaryExpression BuildUnaryExpression(JObject unaryExpression, Expression contextExpression)
        {
            var operatorName = unaryExpression.GetOperator();
            if (!SupportedUnaryOperators.Value.TryGetValue(operatorName, out var unaryType))
            {
                throw new ExpressionException($"Operator '{operatorName}' is not a supported unary operator");
            }

            var expressionProperty = unaryExpression.GetOperand();
            var expression = BuildExpression(expressionProperty, contextExpression);
            return Expression.MakeUnary(unaryType, expression, typeof(bool));
        }

        private MethodCallExpression BuildCallExpression(JObject callExpression, Expression contextExpression)
        {
            var methodName = callExpression.GetMethodName();
            var methodXParameter = callExpression.GetMethodXParameter(); // This can be null now

            var argumentsAndTypes = literalExpressionCompiler.ParseArguments(callExpression);
            
            var arguments = new List<Expression>(argumentsAndTypes.Count);
            var argumentTypes = new List<Type>(argumentsAndTypes.Count);

            foreach (var (argument, type) in argumentsAndTypes)
            {
                arguments.Add(argument);
                argumentTypes.Add(type);
            }
            
            var (instanceType, instanceExpression, resolvedArgumentTypes) = 
                ResolveCallInstanceAndArguments(methodXParameter, contextExpression, argumentTypes.ToArray());

            var resolvedMethodInfo = MethodResolver.ResolveMethod(methodName, instanceType,
                resolvedArgumentTypes,
                methodResolutionOptions);

            var method = resolvedMethodInfo.Method;

            if (resolvedMethodInfo.IsExtensionMethod)
            {
                // Prepend the instance expression to the arguments list for the Expression.Call
                var callArguments = new List<Expression> { instanceExpression };
                callArguments.AddRange(arguments);
                return Expression.Call(null, method, callArguments);
            }

            if (method.IsStatic)
            {
                return Expression.Call(null, method, arguments);
            }

            // Instance method
            return Expression.Call(instanceExpression, method, arguments);
        }
        
        private (Type instanceType, Expression instanceExpression, Type[] resolvedArgumentTypes) ResolveCallInstanceAndArguments(string methodXParameter, Expression contextExpression, Type[] originalArgumentTypes)
        {
            Type instanceType;
            Expression instanceExpression;

            if (string.IsNullOrEmpty(methodXParameter))
            {
                // Method is called directly on the context (e.g., ctx.MyMethod() or MyMethod())
                instanceType = methodResolutionOptions.ContextType;
                instanceExpression = contextExpression;
            }
            else
            {
                // Method is called on a member of the context (e.g., ctx.User.MyMethod())
                instanceExpression = BuildMemberAccessExpression(methodXParameter, contextExpression);
                instanceType = instanceExpression.Type;
            }

            return (instanceType, instanceExpression, originalArgumentTypes);
        }

        private Expression BuildMemberAccessExpression(string x, Expression contextExpression)
        {
            Expression result = contextExpression;
            foreach (var property in x.Split("."))
            {
                try
                {
                    result = Expression.PropertyOrField(result, property);
                }
                catch (ArgumentException)
                {
                    throw new ArgumentException($"'{property}' is not a member of '{result.Type.Name}'");
                }
            }

            return result;
        }

        private Expression BuildMemberAccessExpression(JObject memberAccessExpression, Expression contextExpression)
        {
            var path = memberAccessExpression.GetMemberAccessPath();
            var result = BuildMemberAccessExpression(path, contextExpression);
            return result;
        }

        private Expression BuildArrayLiteralExpression(JObject arrayLiteralExpression)
        {
            var (array, _) = literalExpressionCompiler.ParseArrayLiteral(arrayLiteralExpression);
            return array;
        }

        private Expression BuildBasicLiteralExpression(JObject basicLiteralExpression)
        {
            var (constantExpression, _) = literalExpressionCompiler.ParseBasicLiteral(basicLiteralExpression);
            return constantExpression;
        }
    }
}