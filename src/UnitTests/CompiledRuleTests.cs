using Compiler.Exceptions;
using Compiler.Options;
using Compiler.Services;
using Newtonsoft.Json.Linq;

namespace UnitTests
{
    public class CompiledRuleTests
    {
        private readonly RuleExpressionCompiler _compiler;

        public CompiledRuleTests()
        {
            var literalExpressionCompiler = new LiteralExpressionCompiler();
            _compiler = new RuleExpressionCompiler(
                MethodResolutionOptions.Default(typeof(TestContext)),
                literalExpressionCompiler);
        }

        [Fact]
        public void Compile_WithBasicLiteral_ShouldSucceed()
        {
            // true -> true
            var expression = new JObject
            {
                ["expressionType"] = "BasicLiteral",
                ["kind"] = "BOOL",
                ["value"] = "true"
            };

            var rule = _compiler.Compile(typeof(TestContext), expression);
            var result = rule.Evaluate(new TestContext());
            Assert.True(result);
        }

        [Fact]
        public void Compile_WithBasicBinaryLiteral_ShouldSucceed()
        {
            // 1 < 2 -> True
            var expression = new JObject
            {
                ["expressionType"] = "Binary",
                ["operator"] = "LessThan",
                ["left"] = new JObject
                {
                    ["expressionType"] = "BasicLiteral",
                    ["kind"] = "INT",
                    ["value"] = "1"
                },
                ["right"] = new JObject
                {
                    ["expressionType"] = "BasicLiteral",
                    ["kind"] = "INT",
                    ["value"] = "2"
                }
            };

            var rule = _compiler.Compile(typeof(TestContext), expression);
            var result = rule.Evaluate(new TestContext());
            Assert.True(result);
        }

        [Fact]
        public void Compile_WithNestedBinaryLiteral_ShouldSucceed()
        {
            // 42 < (7 + 7) -> False
            var expression = new JObject
            {
                ["expressionType"] = "Binary",
                ["operator"] = "LessThan",
                ["left"] = new JObject
                {
                    ["expressionType"] = "BasicLiteral",
                    ["kind"] = "INT",
                    ["value"] = "42"
                },
                ["right"] = new JObject
                {
                    ["expressionType"] = "Binary",
                    ["operator"] = "AddChecked",
                    ["left"] = new JObject
                    {
                        ["expressionType"] = "BasicLiteral",
                        ["kind"] = "INT",
                        ["value"] = "7"
                    },
                    ["right"] = new JObject
                    {
                        ["expressionType"] = "BasicLiteral",
                        ["kind"] = "INT",
                        ["value"] = "7"
                    }
                }
            };

            var rule = _compiler.Compile(typeof(TestContext), expression);
            var result = rule.Evaluate(new TestContext());
            Assert.False(result);
        }

        [Fact]
        public void Compile_WithMismatchedBinaryLiteralTypes_ShouldThrowExpressionException()
        {
            //42 + "7" -> Error
            var expression = new JObject
            {
                ["expressionType"] = "Binary",
                ["operator"] = "AddChecked",
                ["left"] = new JObject
                {
                    ["expressionType"] = "BasicLiteral",
                    ["kind"] = "INT",
                    ["value"] = "42"
                },
                ["right"] = new JObject
                {
                    ["expressionType"] = "BasicLiteral",
                    ["kind"] = "STRING",
                    ["value"] = "7"
                }
            };

            var ex = Assert.Throws<ExpressionException>(() => _compiler.Compile(typeof(TestContext), expression));
            Assert.Contains(
                "The binary operator AddChecked must have operands of the same type. Operand types used where 'Int32' and 'String'",
                ex.Message);
        }

        [Fact]
        public void Compile_WithUnaryExpression_ShouldSucceed()
        {
            // !false -> true
            var expression = new JObject
            {
                ["expressionType"] = "Unary",
                ["operator"] = "Not",
                ["expression"] = new JObject
                {
                    ["expressionType"] = "BasicLiteral",
                    ["kind"] = "BOOL",
                    ["value"] = "false"
                }
            };

            var rule = _compiler.Compile(typeof(TestContext), expression);
            var result = rule.Evaluate(new TestContext());
            Assert.True(result);
        }

        [Fact]
        public void Compile_WithMethodCall_ShouldSucceed()
        {
            //ctx.TestMethod1(11) -> true (11 > 10)
            var expression = new JObject
            {
                ["expressionType"] = "Call",
                ["method"] = new JObject
                {
                    ["name"] = "TestMethod1",
                },
                ["arguments"] = new JArray
                {
                    new JObject
                    {
                        ["expressionType"] = "BasicLiteral",
                        ["kind"] = "INT",
                        ["value"] = "11"
                    }
                }
            };

            var rule = _compiler.Compile(typeof(TestContext), expression);
            var result = rule.Evaluate(new TestContext());
            Assert.True(result);
        }

        [Fact]
        public void Compile_WithMemberAccessPath_ShouldSucceed()
        {
            var testContext = new TestContext
            {
                Inner = new TestContext.InnerContext
                {
                    Value = 5
                }
            };

            // ctx.Inner.Value == 5 -> true
            var expression = new JObject
            {
                ["expressionType"] = "Binary",
                ["operator"] = "Equal",
                ["left"] = new JObject
                {
                    ["expressionType"] = "MemberAccess",
                    ["path"] = "Inner.Value"
                },
                ["right"] = new JObject
                {
                    ["expressionType"] = "BasicLiteral",
                    ["kind"] = "INT",
                    ["value"] = "5"
                }
            };

            var rule = _compiler.Compile(typeof(TestContext), expression);
            var result = rule.Evaluate(testContext);
            Assert.True(result);
        }


        [Fact]
        public void Compile_WithMethodCallAndArrayLiteral_ShouldSucceed()
        {
            // ctx.TestMethodWithArray([1,2]) -> true
            var expression = new JObject
            {
                ["expressionType"] = "Call",
                ["method"] = new JObject
                {
                    ["name"] = "TestMethodWithArray",
                },
                ["arguments"] = new JArray
                {
                    new JObject
                    {
                        ["expressionType"] = "ArrayLiteral",
                        ["elements"] = new JArray
                        {
                            new JObject
                            {
                                ["expressionType"] = "BasicLiteral",
                                ["kind"] = "INT",
                                ["value"] = "1"
                            },
                            new JObject
                            {
                                ["expressionType"] = "BasicLiteral",
                                ["kind"] = "INT",
                                ["value"] = "2"
                            }
                        }
                    }
                }
            };

            var rule = _compiler.Compile(typeof(TestContext), expression);
            var result = rule.Evaluate(new TestContext());
            Assert.True(result);
        }

        [Fact]
        public void Compile_WithNotEqualExpression_ShouldSucceed()
        {
            // 1 != 2 -> true
            var expression = new JObject
            {
                ["expressionType"] = "Binary",
                ["operator"] = "NotEqual",
                ["left"] = new JObject
                {
                    ["expressionType"] = "BasicLiteral",
                    ["kind"] = "INT",
                    ["value"] = "1"
                },
                ["right"] = new JObject
                {
                    ["expressionType"] = "BasicLiteral",
                    ["kind"] = "INT",
                    ["value"] = "2"
                }
            };

            var rule = _compiler.Compile(typeof(TestContext), expression);
            var result = rule.Evaluate(new TestContext());
            Assert.True(result);
        }

        [Fact]
        public void Compile_WithAndAlsoExpression_ShouldSucceed()
        {
            // true && true -> true
            var expression = new JObject
            {
                ["expressionType"] = "Binary",
                ["operator"] = "AndAlso",
                ["left"] = new JObject
                {
                    ["expressionType"] = "BasicLiteral",
                    ["kind"] = "BOOL",
                    ["value"] = "true"
                },
                ["right"] = new JObject
                {
                    ["expressionType"] = "BasicLiteral",
                    ["kind"] = "BOOL",
                    ["value"] = "true"
                }
            };

            var rule = _compiler.Compile(typeof(TestContext), expression);
            var result = rule.Evaluate(new TestContext());
            Assert.True(result);
        }

        [Fact]
        public void Compile_WithOrElseExpression_ShouldSucceed()
        {
            // false || true -> true
            var expression = new JObject
            {
                ["expressionType"] = "Binary",
                ["operator"] = "OrElse",
                ["left"] = new JObject
                {
                    ["expressionType"] = "BasicLiteral",
                    ["kind"] = "BOOL",
                    ["value"] = "false"
                },
                ["right"] = new JObject
                {
                    ["expressionType"] = "BasicLiteral",
                    ["kind"] = "BOOL",
                    ["value"] = "true"
                }
            };

            var rule = _compiler.Compile(typeof(TestContext), expression);
            var result = rule.Evaluate(new TestContext());
            Assert.True(result);
        }

        [Fact]
        public void Compile_WithMismatchedMethodArguments_ShouldThrowMethodResolutionException()
        {
            var expression = new JObject
            {
                ["expressionType"] = "Call",
                ["method"] = new JObject
                {
                    ["name"] = "TestMethod1",
                },
                ["arguments"] = new JArray
                {
                    new JObject
                    {
                        ["expressionType"] = "BasicLiteral",
                        ["kind"] = "STRING",
                        ["value"] = "not an int"
                    }
                }
            };

            var ex = Assert.Throws<MethodResolutionException>(() => _compiler.Compile(typeof(TestContext), expression));
            Assert.Contains("Failed to resolve method 'TestMethod1' with arguments: String", ex.Message);
        }

        [Fact]
        public void Compile_WithNonExistentMethod_ShouldThrowMethodResolutionException()
        {
            var expression = new JObject
            {
                ["expressionType"] = "Call",
                ["method"] = new JObject
                {
                    ["name"] = "NonExistentMethod"
                },
                ["arguments"] = new JArray()
            };

            var ex = Assert.Throws<MethodResolutionException>(() => _compiler.Compile(typeof(TestContext), expression));
            Assert.Contains("Failed to resolve method 'NonExistentMethod' with arguments", ex.Message);
        }

        [Fact]
        public void Compile_WithInvalidMember_ShouldThrowArgumentException()
        {
            var expression = new JObject
            {
                ["expressionType"] = "MemberAccess",
                ["path"] = "InvalidMember"
            };

            var ex = Assert.Throws<ArgumentException>(() => _compiler.Compile(typeof(TestContext), expression));
            Assert.Contains("is not a member of", ex.Message);
        }

        [Fact]
        public void Compile_WithMethodCallOnMember_ShouldSucceed()
        {
            var testContext = new TestContext
            {
                Inner = new TestContext.InnerContext
                {
                    Value = 5
                }
            };

            // Inner.IsPositive() -> true
            var expression = new JObject
            {
                ["expressionType"] = "Call",
                ["method"] = new JObject
                {
                    ["name"] = "IsPositive",
                    ["x"] = "Inner"
                },
                ["arguments"] = new JArray()
            };

            var rule = _compiler.Compile(typeof(TestContext), expression);
            var result = rule.Evaluate(testContext);
            Assert.True(result);
        }

        [Fact]
        public void Compile_WithMethodCallInBinaryExpression_ShouldSucceed()
        {
            // ctx.TestMagicValueMethod() == "a42" => true
            var expression = new JObject
            {
                ["expressionType"] = "Binary",
                ["operator"] = "Equal",
                ["left"] = new JObject
                {
                    ["expressionType"] = "Call",
                    ["method"] = new JObject
                    {
                        ["name"] = "TestMagicValueMethod"
                    },
                    ["arguments"] = new JArray(),
                },
                ["right"] = new JObject
                {
                    ["expressionType"] = "BasicLiteral",
                    ["kind"] = "STRING",
                    ["value"] = "a42"
                }
            };

            var rule = _compiler.Compile(typeof(TestContext), expression);
            var result = rule.Evaluate(new TestContext());
            Assert.True(result);
        }
    }
}