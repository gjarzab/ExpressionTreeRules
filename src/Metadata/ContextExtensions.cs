using System.Collections.Concurrent;
using System.Reflection;
using Core;

namespace Metadata
{
    public static class ContextExtensions
    {
        private static readonly ConcurrentDictionary<Type, ContextDescription> DescriptionCache = new();

        public static ContextDescription GetContextDescription(Type contextType)
        {
            return DescriptionCache.GetOrAdd(contextType, type =>
            {
                if (!type.IsAssignableTo(typeof(IContext)))
                {
                    throw new ArgumentException("The specified type must be assignable to IContext", nameof(contextType));
                }

                var members = new List<ContextMemberDescription>();
                var methods = GetMethodsForType(type).ToList();

                var candidates = type
                    .GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                    .Where(member => member.MemberType == MemberTypes.Field || member.MemberType == MemberTypes.Property && member.Name != nameof(IContext.ContextName))
                    .ToArray();

                var visitedTypes = new HashSet<Type> { type };

                foreach (var member in candidates)
                {
                    var omitAttribute = member.GetCustomAttribute<OmitFromDescriptionAttribute>(false);
                    if (omitAttribute != null)
                    {
                        continue;
                    }

                    var memberType = GetMemberType(member);
                    members.Add(new ContextMemberDescription(
                        name: member.Name,
                        type: GetValueType(memberType),
                        path: member.Name,
                        members: GetMembersOfType(memberType, member.Name, visitedTypes).ToList(),
                        methods: GetMethodsForMember(member).ToList()
                    ));
                }

                return new ContextDescription(type.Name, members, methods);
            });
        }

        private static IEnumerable<ContextMemberDescription> GetMembersOfType(Type memberType, string path, ISet<Type> visitedTypes)
        {
            ArgumentNullException.ThrowIfNull(memberType);
            
            var alreadyVisited = visitedTypes.Contains(memberType);
            var fromSystemNamespace = memberType.Namespace != null && memberType.Namespace.StartsWith("System");
            if (memberType.IsPrimitive || memberType == typeof(string) || fromSystemNamespace || alreadyVisited)
            {
                return [];
            }

            var result = new List<ContextMemberDescription>();
            var members = new List<MemberInfo>();

            var candidates =
                memberType.GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                    .Where(member => member.MemberType == MemberTypes.Field || member.MemberType == MemberTypes.Property)
                    .ToArray();

            foreach (var member in candidates)
            {
                var omitAttribute = member.GetCustomAttribute<OmitFromDescriptionAttribute>(false);
                if (omitAttribute != null)
                {
                    continue;
                }

                if (member.MemberType == MemberTypes.Field || member.MemberType == MemberTypes.Property)
                {
                    members.Add(member);
                }
            }

            var newVisitedTypes = new HashSet<Type>(visitedTypes) { memberType };

            foreach (var member in members)
            {
                var memberChildType = GetMemberType(member);
                var memberName = member.Name;
                var childPath = string.IsNullOrEmpty(path) ? $"{memberName}" : $"{path}.{memberName}";

                var childMemberDescriptions = GetMembersOfType(memberChildType, childPath, newVisitedTypes);

                var contextDescription = new ContextMemberDescription(
                    name: member.Name,
                    type: GetValueType(memberChildType),
                    path: childPath,
                    methods: GetMethodsForMember(member).ToList(),
                    members: childMemberDescriptions.ToList()
                );

                result.Add(contextDescription);
            }

            return result;
        }

        private static IEnumerable<ContextMemberMethodDescription> GetMethodsForType(Type type)
        {
            var fromSystemNamespace = type.Namespace != null && type.Namespace.StartsWith("System");
            if (type.IsPrimitive || fromSystemNamespace)
            {
                return [];
            }

            var result = new List<ContextMemberMethodDescription>();
            var candidates = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
            
            foreach (var method in candidates)
            {
                if (method.IsSpecialName)
                {
                    continue;
                }

                if (method.DeclaringType != null && (method.DeclaringType.IsPrimitive || method.DeclaringType == typeof(string)))
                {
                    continue;
                }

                if (method.GetCustomAttribute<OmitFromDescriptionAttribute>() != null)
                {
                    continue;
                }

                var parameters = method
                    .GetParameters()
                    .Select(parameter => new MethodParameterDescription(
                        name: parameter.Name,
                        description: GetParameterDescription(parameter),
                        type: GetValueType(parameter.ParameterType),
                        valueProviderEndpoint: GetParameterValueProvider(parameter)
                    )).ToList();

                result.Add(new ContextMemberMethodDescription(
                    name: method.Name,
                    description: GetMethodDescription(method),
                    returnType: GetValueType(method.ReturnType),
                    parameters: parameters
                ));
            }

            return result;
        }

        private static IEnumerable<ContextMemberMethodDescription> GetMethodsForMember(MemberInfo memberInfo)
        {
            return memberInfo switch
            {
                FieldInfo fieldInfo       => GetMethodsForType(fieldInfo.FieldType),
                PropertyInfo propertyInfo => GetMethodsForType(propertyInfo.PropertyType),
                _                         => []
            };
        }

        private static Type GetMemberType(MemberInfo memberInfo)
        {
            return memberInfo switch
            {
                PropertyInfo propertyInfo => propertyInfo.PropertyType,
                FieldInfo fieldInfo       => fieldInfo.FieldType,
                _                         => throw new ArgumentException(nameof(memberInfo))
            };
        }

        private static string GetValueType(Type type)
        {
            if (type == typeof(int))
            {
                return "INT";
            }

            if (type == typeof(bool))
            {
                return "BOOL";
            }

            if (type == typeof(void))
            {
                return "VOID";
            }

            return type == typeof(string) ? "STRING" : "OTHER";
        }

        private static string GetParameterDescription(ParameterInfo parameterInfo)
        {
            ArgumentNullException.ThrowIfNull(parameterInfo);

            var parameterDescription = parameterInfo
                .GetCustomAttribute<ParameterDescriptionAttribute>();

            return parameterDescription?.Description;
        }

        private static string GetParameterValueProvider(ParameterInfo parameterInfo)
        {
            ArgumentNullException.ThrowIfNull(parameterInfo);

            var valueProvider = parameterInfo
                .GetCustomAttribute<ParameterValueProviderAttribute>();

            return valueProvider?.ApiEndpoint;
        }

        private static string GetMethodDescription(MethodInfo methodInfo)
        {
            ArgumentNullException.ThrowIfNull(methodInfo);

            var methodDescription = methodInfo
                .GetCustomAttribute<MethodDescriptionAttribute>();

            return methodDescription?.Description;
        }
    }
}