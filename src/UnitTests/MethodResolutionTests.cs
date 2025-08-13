using Compiler.Exceptions;
using Compiler.Options;
using Compiler.Services;
using Newtonsoft.Json.Linq;

namespace UnitTests
{
    public class MethodResolutionTests
    {
        private readonly RuleExpressionCompiler _compiler;

        public MethodResolutionTests()
        {
            var literalExpressionCompiler = new LiteralExpressionCompiler();
            _compiler = new RuleExpressionCompiler(
                MethodResolutionOptions.WithExtensionMethods(typeof(TestContext), typeof(TestContextExtensions)),
                literalExpressionCompiler);
        }

        [Fact]
        public void ResolveMethod_WithInstanceMethod_ShouldSucceed()
        {
            var options = MethodResolutionOptions.Default(typeof(TestMethodProvider));
            var methodInfo = MethodResolver.ResolveMethod("InstanceMethod", typeof(TestMethodProvider), Type.EmptyTypes,
                options);

            Assert.NotNull(methodInfo);
            Assert.False(methodInfo.IsExtensionMethod);
            Assert.Equal("InstanceMethod", methodInfo.Method.Name);
        }

        [Fact]
        public void ResolveMethod_WithStaticMethod_ShouldSucceed()
        {
            var options = MethodResolutionOptions.Default(typeof(TestMethodProvider));
            var methodInfo = MethodResolver.ResolveMethod("StaticMethod", typeof(TestMethodProvider), Type.EmptyTypes, options);

            Assert.NotNull(methodInfo);
            Assert.False(methodInfo.IsExtensionMethod);
            Assert.Equal("StaticMethod", methodInfo.Method.Name);
        }

        [Fact]
        public void ResolveMethod_WithMethodFromBase_ShouldSucceed()
        {
            var options = MethodResolutionOptions.Default(typeof(DerivedTestMethodProvider));
            var methodInfo = MethodResolver.ResolveMethod("InstanceMethod", typeof(DerivedTestMethodProvider),
                Type.EmptyTypes, options);

            Assert.NotNull(methodInfo);
            Assert.False(methodInfo.IsExtensionMethod);
            Assert.Equal("InstanceMethod", methodInfo.Method.Name);
        }

        [Fact]
        public void ResolveMethod_WithExtensionMethod_ShouldSucceed()
        {
            var options =
                MethodResolutionOptions.WithExtensionMethods(typeof(TestMethodProvider), typeof(TestExtensionMethods));
            var methodInfo = MethodResolver.ResolveMethod("ExtensionMethod", typeof(TestMethodProvider),
                Type.EmptyTypes, options);

            Assert.NotNull(methodInfo);
            Assert.True(methodInfo.IsExtensionMethod);
            Assert.Equal("ExtensionMethod", methodInfo.Method.Name);
        }

        [Fact]
        public void ResolveMethod_WithExtensionMethodAndArgument_ShouldSucceed()
        {
            var options =
                MethodResolutionOptions.WithExtensionMethods(typeof(TestMethodProvider), typeof(TestExtensionMethods));
            var methodInfo = MethodResolver.ResolveMethod("ExtensionMethodWithArg", typeof(TestMethodProvider),
                [typeof(int)], options);

            Assert.NotNull(methodInfo);
            Assert.True(methodInfo.IsExtensionMethod);
            Assert.Equal("ExtensionMethodWithArg", methodInfo.Method.Name);
        }

        [Fact]
        public void ResolveMethod_WithNonExistentMethod_ShouldThrowMethodResolutionException()
        {
            var options = MethodResolutionOptions.Default(typeof(TestMethodProvider));
            var ex = Assert.Throws<MethodResolutionException>(() =>
                MethodResolver.ResolveMethod("NonExistent", typeof(TestMethodProvider), Type.EmptyTypes, options));

            Assert.Contains("Failed to resolve method 'NonExistent'", ex.Message);
        }

        [Fact]
        public void ResolveMethod_WhenExtensionMethodsNotEnabled_ShouldThrowMethodResolutionException()
        {
            var options = MethodResolutionOptions.Default(typeof(TestMethodProvider));
            var ex = Assert.Throws<MethodResolutionException>(() =>
                MethodResolver.ResolveMethod("ExtensionMethod", typeof(TestMethodProvider), Type.EmptyTypes, options));

            Assert.Contains("Failed to resolve method 'ExtensionMethod'", ex.Message);
        }

        [Fact]
        public void ResolveMethod_WithOverloadedMethod_ShouldSucceed()
        {
            var options = MethodResolutionOptions.Default(typeof(TestMethodProvider));
            var methodInfoString = MethodResolver.ResolveMethod("OverloadedMethod", typeof(TestMethodProvider),
                [typeof(string)], options);
            var methodInfoInt =
                MethodResolver.ResolveMethod("OverloadedMethod", typeof(TestMethodProvider), [typeof(int)], options);

            Assert.NotNull(methodInfoString);
            Assert.False(methodInfoString.IsExtensionMethod);
            Assert.Equal("OverloadedMethod", methodInfoString.Method.Name);
            Assert.Single(methodInfoString.Method.GetParameters());
            Assert.Equal(typeof(string), methodInfoString.Method.GetParameters()[0].ParameterType);

            Assert.NotNull(methodInfoInt);
            Assert.False(methodInfoInt.IsExtensionMethod);
            Assert.Equal("OverloadedMethod", methodInfoInt.Method.Name);
            Assert.Single(methodInfoInt.Method.GetParameters());
            Assert.Equal(typeof(int), methodInfoInt.Method.GetParameters()[0].ParameterType);
        }

        [Fact]
        public void Compile_WithExtensionMethodOnContext_ShouldSucceed()
        {
            var expression = new JObject
            {
                ["expressionType"] = "Call",
                ["method"] = new JObject
                {
                    ["name"] = "IsAdult",
                },
                ["arguments"] = new JArray
                {
                    new JObject
                    {
                        ["expressionType"] = "BasicLiteral",
                        ["kind"] = "INT",
                        ["value"] = "18"
                    }
                }
            };

            var rule = _compiler.Compile(typeof(TestContext), expression);
            var result = rule.Evaluate(new TestContext { TestField1 = "Adult" });
            Assert.True(result);
        }

        [Fact]
        public void Compile_WithExtensionMethodOnPrimitiveType_ShouldSucceed()
        {
            var expression = new JObject
            {
                ["expressionType"] = "Call",
                ["method"] = new JObject
                {
                    ["name"] = "IsEven",
                    ["x"] = "Inner.Value" // 'Context.Inner.Value' is an int
                },
                ["arguments"] = new JArray()
            };

            var rule = _compiler.Compile(typeof(TestContext), expression);
            var result = rule.Evaluate(new TestContext { Inner = new TestContext.InnerContext { Value = 4 } });
            Assert.True(result);
        }

        [Fact]
        public void Compile_WithNonExistentExtensionMethod_ShouldThrowMethodResolutionException()
        {
            var expression = new JObject
            {
                ["expressionType"] = "Call",
                ["method"] = new JObject
                {
                    ["name"] = "NonExistentExtensionMethod",
                },
                ["arguments"] = new JArray()
            };

            var ex = Assert.Throws<MethodResolutionException>(() => _compiler.Compile(typeof(TestContext), expression));
            Assert.Contains("Failed to resolve method 'NonExistentExtensionMethod'", ex.Message);
        }
    }
}