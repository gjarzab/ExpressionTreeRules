using Metadata;
using Newtonsoft.Json;

namespace UnitTests
{
    public class ContextExtensionsTests
    {
        [Fact]
        public void GetContextDescription_ShouldReturnCorrectDescriptionForTestContext()
        {
            // Arrange
            var contextType = typeof(TestContext);

            // Act
            var description = ContextExtensions.GetContextDescription(contextType);
            // Assert
            Assert.NotNull(description);
            Assert.Equal(nameof(TestContext), description.ContextName);
            
            Assert.Equal(3, description.Members.Count);
            var testField1 = description.Members.FirstOrDefault(m => m.Name == "TestField1");
            Assert.NotNull(testField1);
            Assert.Equal("TestField1", testField1.Name);
            Assert.Equal("STRING", testField1.Type);
            Assert.Equal("TestField1", testField1.Path);

            var dateField = description.Members.FirstOrDefault(m => m.Name == "DateField");
            Assert.NotNull(dateField);
            Assert.Equal("DateField", dateField.Name);
            Assert.Equal("OTHER", dateField.Type);
            Assert.Equal("DateField", dateField.Path);
            Assert.Empty(dateField.Members);
            Assert.Empty(dateField.Methods);

            Assert.Equal(4, description.Methods.Count);
            var testMethod1 = description.Methods.FirstOrDefault(m => m.Name == "TestMethod1");
            Assert.NotNull(testMethod1);
            Assert.Equal("TestMethod1", testMethod1.Name);
            Assert.Equal("BOOL", testMethod1.ReturnType);
            Assert.Single(testMethod1.Parameters);
            Assert.Equal("someId", testMethod1.Parameters[0].Name);
            Assert.Equal("INT", testMethod1.Parameters[0].Type);

            var testMethodWithArray = description.Methods.FirstOrDefault(m => m.Name == "TestMethodWithArray");
            Assert.NotNull(testMethodWithArray);
            Assert.Equal("TestMethodWithArray", testMethodWithArray.Name);
            Assert.Equal("BOOL", testMethodWithArray.ReturnType);
            Assert.Single(testMethodWithArray.Parameters);
            Assert.Equal("values", testMethodWithArray.Parameters[0].Name);
            Assert.Equal("OTHER", testMethodWithArray.Parameters[0].Type);
            
            var testMagicValueMethod = description.Methods.FirstOrDefault(m => m.Name == "TestMagicValueMethod");
            Assert.NotNull(testMagicValueMethod);
            Assert.Equal("TestMagicValueMethod", testMagicValueMethod.Name);
            Assert.Equal("STRING", testMagicValueMethod.ReturnType);
            Assert.Empty(testMagicValueMethod.Parameters);

            var omittedProperty =
                description.Members.FirstOrDefault(m => m.Name == nameof(TestContext.OmittedProperty));
            Assert.Null(omittedProperty);
        }

        [Fact]
        public void GetContextDescription_ShouldHandleNestedMembers()
        {
            // Arrange
            var contextType = typeof(TestContext);

            // Act
            var description = ContextExtensions.GetContextDescription(contextType);

            // Assert
            var innerMember = description.Members.FirstOrDefault(m => m.Name == "Inner");
            Assert.NotNull(innerMember);
            Assert.Equal("Inner", innerMember.Name);
            Assert.Equal("OTHER", innerMember.Type);
            Assert.Equal("Inner", innerMember.Path);
            Assert.Single(innerMember.Members);
            
            var valueMember = innerMember.Members[0];
            Assert.Equal("Value", valueMember.Name);
            Assert.Equal("INT", valueMember.Type);
            Assert.Equal("Inner.Value", valueMember.Path);
        }

        [Fact]
        public void GetContextDescription_ShouldReadParameterValueProviderAttribute()
        {
            // Arrange
            var contextType = typeof(TestContext);

            // Act
            var description = ContextExtensions.GetContextDescription(contextType);

            // Assert
            var method = description.Methods.FirstOrDefault(m => m.Name == "TestMethodWithValueProvider");
            Assert.NotNull(method);

            Assert.Equal("A test method with a value provider.", method.Description);
            Assert.Single(method.Parameters);

            var parameter = method.Parameters[0];
            Assert.Equal("role", parameter.Name);
            Assert.Equal("/api/v1/roles", parameter.ValueProviderEndpoint);
        }

        [Fact]
        public void GetContextDescription_ShouldHandleRecursiveTypes()
        {
            // Arrange
            var contextType = typeof(RecursiveContext);

            // Act
            var description = ContextExtensions.GetContextDescription(contextType);

            // Assert
            Assert.NotNull(description);
            var parentMember = description.Members.FirstOrDefault(m => m.Name == "Parent");
            Assert.NotNull(parentMember);
            Assert.Empty(parentMember.Members); // Should not expand the recursive member
        }

        [Fact]
        public void GetContextDescription_ShouldHandleCoRecursiveTypes()
        {
            // Arrange
            var contextType = typeof(CoRecursiveA);

            // Act
            var description = ContextExtensions.GetContextDescription(contextType);

            // Assert
            Assert.NotNull(description);
            var bMember = description.Members.FirstOrDefault(m => m.Name == "B");
            Assert.NotNull(bMember);
            Assert.Single(bMember.Members); // B should have one member 'A'
            var aMember = bMember.Members.FirstOrDefault(m => m.Name == "A");
            Assert.NotNull(aMember);
            Assert.Empty(aMember.Members); // Should not expand the co-recursive member 'A'
        }
    }
}