using System.Linq.Expressions;
using Compiler.Exceptions;
using Compiler.Extensions;
using Compiler.Services.Parsers;
using Newtonsoft.Json.Linq;

namespace Compiler.Services
{
    public sealed class LiteralExpressionCompiler
    {
        public (Expression Constant, Type ConstantType) ParseBasicLiteral(JObject basicLiteralExpression)
        {
            var kind = basicLiteralExpression.GetLiteralKind();
            var value = basicLiteralExpression.GetLiteralValue();

            return kind switch
            {
                "BOOL" => CreateConstant<bool, BoolParser>(value),
                "INT" => CreateConstant<int, IntParser>(value),
                "DECIMAL" => CreateConstant<decimal, DecimalParser>(value),
                "STRING" => CreateConstant<string, StringParser>(value),
                _ => throw new LiteralParseException($"Unrecognized literal kind '{kind}'")
            };
        }

        private (Expression Constant, Type ConstantType) CreateConstant<T, TParser>(string value)
            where TParser : ILiteralParser<T>
        {
            if (TParser.TryParse(value, out var parsedValue))
            {
                return (Expression.Constant(parsedValue, typeof(T)), typeof(T));
            }
            
            throw new LiteralParseException($"Unable to parse literal of kind '{TParser.TypeName}' with value '{value}'");
        }

        public (Expression NewArray, Type ArrayType) ParseArrayLiteral(JObject arrayLiteralExpression)
        {
            Type arrayElementType = null;
            var elements = arrayLiteralExpression.GetArrayElementsArray();
            var elementConstants = new List<Expression>(elements.Count);

            var elementIndex = 0;
            foreach (var token in elements)
            {
                if (token is JObject element)
                {
                    try
                    {
                        var (elementLiteral, elementType) = ParseBasicLiteral(element);
                        elementIndex++;

                        if (arrayElementType == null)
                        {
                            arrayElementType = elementType;
                        } else if (arrayElementType != elementType)
                        {
                            throw new ExpressionException(
                                $"Array elements must all be of the same type. Found '{arrayElementType.Name}' and '{elementType.Name}'");
                        }

                        elementConstants.Add(elementLiteral);
                    }
                    catch (LiteralParseException e)
                    {
                        throw new LiteralParseException($"Failed to parse array element at index {elementIndex}", e);
                    }
                }
                else
                {
                    throw new LiteralParseException($"Failed to parse array element at index {elementIndex}, token was not an object");
                }
            }

            if (arrayElementType == null)
            {
                throw new ExpressionException("Arrays must have at least one element");
            }

            return (Expression.NewArrayInit(arrayElementType, elementConstants), arrayElementType.MakeArrayType());
        }

        private (Expression Argument, Type ArgumetnType) ParseArgument(JObject argumentExpression)
        {
            var expressionType = argumentExpression.GetExpressionType();
            return expressionType switch
            {
                RecognizedExpressionTypes.ArrayLiteralExpression => ParseArrayLiteral(argumentExpression),
                RecognizedExpressionTypes.BasicLiteralExpression => ParseBasicLiteral(argumentExpression),
                _ => throw new ExpressionException($"Argument expression '{expressionType}' is not supported")
            };
        }

        public List<(Expression Argument, Type ArgumentType)> ParseArguments(JObject callExpression)
        {
            var result = new List<(Expression Argument, Type ArgumentType)>();
            var argumentElements = callExpression.GetMethodArgumentsArray();

            var elementIndex = 0;
            foreach (var token in argumentElements)
            {
                if (token is JObject argument)
                {
                    var (expression, type) = ParseArgument(argument);
                    elementIndex++;
                    result.Add((expression, type));
                }
                else
                {
                    throw new LiteralParseException($"Failed to parse array element at index {elementIndex}, token was not an object");
                }
            }

            return result;
        }
    }
}
