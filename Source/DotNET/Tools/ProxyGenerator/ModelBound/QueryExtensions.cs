// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.ModelBound;

/// <summary>
/// Extensions for query types.
/// </summary>
public static class QueryExtensions
{
    /// <summary>
    /// Convert a query <see cref="TypeInfo"/> to a collection of <see cref="QueryDescriptor"/>.
    /// </summary>
    /// <param name="readModelType">Read model type to convert.</param>
    /// <param name="targetPath">The target path the proxies are generated to.</param>
    /// <param name="segmentsToSkip">Number of segments to skip from the namespace when generating the output path.</param>
    /// <param name="skipQueryNameInRoute">True if the query name should be skipped in the route, false if not.</param>
    /// <param name="apiPrefix">The API prefix to use in the route.</param>
    /// <param name="allQueryTypes">Collection of all query types to detect conflicts.</param>
    /// <returns>Collection of converted <see cref="QueryDescriptor"/>.</returns>
    public static IEnumerable<QueryDescriptor> ToQueryDescriptors(
        this TypeInfo readModelType,
        string targetPath,
        int segmentsToSkip,
        bool skipQueryNameInRoute,
        string apiPrefix,
        IEnumerable<TypeInfo> allQueryTypes)
    {
        var queryMethods = readModelType.GetQueryMethods();
        var descriptors = new List<QueryDescriptor>();

        foreach (var method in queryMethods)
        {
            var descriptor = method.ToQueryDescriptor(readModelType, targetPath, segmentsToSkip, skipQueryNameInRoute, apiPrefix, allQueryTypes);
            descriptors.Add(descriptor);
        }

        return descriptors;
    }

    /// <summary>
    /// Convert a static query method to a <see cref="QueryDescriptor"/>.
    /// </summary>
    /// <param name="method">Query method to convert.</param>
    /// <param name="readModelType">The read model type that contains this method.</param>
    /// <param name="targetPath">The target path the proxies are generated to.</param>
    /// <param name="segmentsToSkip">Number of segments to skip from the namespace when generating the output path.</param>
    /// <param name="skipQueryNameInRoute">True if the query name should be skipped in the route, false if not.</param>
    /// <param name="apiPrefix">The API prefix to use in the route.</param>
    /// <param name="allQueryTypes">Collection of all query types to detect conflicts.</param>
    /// <returns>Converted <see cref="QueryDescriptor"/>.</returns>
    public static QueryDescriptor ToQueryDescriptor(
        this MethodInfo method,
        TypeInfo readModelType,
        string targetPath,
        int segmentsToSkip,
        bool skipQueryNameInRoute,
        string apiPrefix,
        IEnumerable<TypeInfo> allQueryTypes)
    {
        var typesInvolved = new List<Type>();

        var responseModel = ModelDescriptor.Empty;
        if (method.ReturnType.IsAssignableTo<Task>() && method.ReturnType.IsGenericType)
        {
            var responseType = method.ReturnType.GetGenericArguments()[0];
            responseModel = responseType.ToModelDescriptor();
        }
        else if (method.ReturnType != TypeExtensions._voidType && method.ReturnType != TypeExtensions._taskType)
        {
            responseModel = method.ReturnType.ToModelDescriptor();
        }

        if (!responseModel.Type.IsKnownType())
        {
            typesInvolved.Add(responseModel.Type);
        }

        var parameters = method.GetQueryParameterDescriptors();
        var properties = method.GetQueryPropertyDescriptors();

        var parameterWithComplexTypes = parameters.Where(_ => !_.OriginalType.IsKnownType());
        typesInvolved.AddRange(parameterWithComplexTypes.Select(_ => _.OriginalType));

        var location = readModelType.Namespace?.Split('.') ?? [];
        var segments = location.Skip(segmentsToSkip).Select(segment => segment.ToKebabCase());
        var baseUrl = $"/{apiPrefix}/{string.Join('/', segments)}".TrimEnd('/');

        var namespaceKey = string.Join('.', location.Skip(segmentsToSkip));
        var queriesInSameNamespace = allQueryTypes.Where(t => string.Join('.', (t.Namespace?.Split('.') ?? []).Skip(segmentsToSkip)) == namespaceKey);
        var totalMethodsInNamespace = queriesInSameNamespace.Sum(t => t.GetQueryMethods().Count());
        var hasConflict = totalMethodsInNamespace > 1;
        var includeQueryName = !skipQueryNameInRoute || hasConflict;

        // Check for Path attribute on method or type
        var methodRouteAttr = method.GetCustomAttributesData().FirstOrDefault(a => a.AttributeType.Name == "PathAttribute");
        var typeRouteAttr = readModelType.GetCustomAttributesData().FirstOrDefault(a => a.AttributeType.Name == "PathAttribute");
        var routeAttributeData = methodRouteAttr ?? typeRouteAttr;

        var customRouteValue = routeAttributeData is { ConstructorArguments.Count: > 0 }
            ? routeAttributeData.ConstructorArguments[0].Value as string
            : null;

        string route;
        if (!string.IsNullOrEmpty(customRouteValue))
        {
            // Use the custom route from the attribute
            route = customRouteValue;
        }
        else
        {
            // Use conventional route generation
            route = includeQueryName ? $"{baseUrl}/{method.Name.ToKebabCase()}" : baseUrl;
            route = route.ToLowerInvariant();
        }

        var relativePath = readModelType.ResolveTargetPath(segmentsToSkip);
        var imports = typesInvolved
                        .GetImports(targetPath, relativePath, segmentsToSkip)
                        .DistinctBy(_ => _.Type)
                        .ToList();

        var additionalTypesInvolved = new List<Type>();
        foreach (var parameter in parameterWithComplexTypes)
        {
            parameter.CollectTypesInvolved(additionalTypesInvolved);
        }

        var parametersNeedingImportStatements = parameters.Where(_ => _.OriginalType.HasModule()).ToList();
        imports.AddRange(parametersNeedingImportStatements.Select(_ => _.OriginalType.GetImportStatement(targetPath, relativePath, segmentsToSkip)));

        foreach (var property in responseModel.Type.GetPropertyDescriptors())
        {
            property.CollectTypesInvolved(additionalTypesInvolved);
        }

        imports = [.. imports.DistinctBy(_ => _.Type)];

        var documentation = method.GetDocumentation();

        // Extract validation rules for the query's parameters from the three sources that can contribute them, merged
        // with the same precedence the command path uses: an explicit validator for a matching parameters class, the
        // validators of any concept-typed parameters, and DataAnnotations on the parameters as a per-parameter fallback.
        var parametersType = FindParametersTypeFor(readModelType, method);
        var explicitRules = parametersType is not null
            ? ValidationRulesExtractor.ExtractValidationRules(readModelType.Assembly, parametersType).ToList()
            : [];

        var conceptRules = new List<PropertyValidationDescriptor>();
        var dataAnnotationsRules = new List<PropertyValidationDescriptor>();
        foreach (var param in method.GetParameters())
        {
            var parameterName = param.Name.ToCamelCase();

            var rulesFromConcept = ValidationRulesExtractor.ExtractRulesForConceptType(readModelType.Assembly, param.ParameterType);
            if (rulesFromConcept.Count > 0)
            {
                conceptRules.Add(new PropertyValidationDescriptor(parameterName, [.. rulesFromConcept]));
            }

            var rulesFromDataAnnotations = ValidationRulesExtractor.ExtractDataAnnotationsFromParameter(param);
            if (rulesFromDataAnnotations.Count > 0)
            {
                dataAnnotationsRules.Add(new PropertyValidationDescriptor(parameterName, [.. rulesFromDataAnnotations]));
            }
        }

        var validationRules = ValidationRulesExtractor.MergeValidationRules(explicitRules, conceptRules, dataAnnotationsRules);

        // Check for TreatWarningsAsErrors attribute
        var treatWarningsAsErrors = method.GetCustomAttributesData().Any(a => a.AttributeType.Name == "TreatWarningsAsErrorsAttribute") ||
                                     readModelType.GetCustomAttributesData().Any(a => a.AttributeType.Name == "TreatWarningsAsErrorsAttribute");

        // Extract roles from authorization attributes on the method and its declaring type (read model)
        var roles = method.GetRoles().ToArray();

        // Check for QueryHttpMethod attribute on method (wins) or read-model type; the enum member name
        // (Get/Query/Auto) matches the TypeScript QueryHttpMethod enum and is emitted into the proxy.
        var httpMethodAttr = method.GetCustomAttributesData().FirstOrDefault(a => a.AttributeType.Name == "QueryHttpMethodAttribute") ??
                             readModelType.GetCustomAttributesData().FirstOrDefault(a => a.AttributeType.Name == "QueryHttpMethodAttribute");
        var httpMethod = httpMethodAttr is { ConstructorArguments.Count: > 0 }
            ? Enum.GetName(httpMethodAttr.ConstructorArguments[0].ArgumentType, httpMethodAttr.ConstructorArguments[0].Value!)
            : null;

        return new(
            readModelType,
            method,
            route,
            method.Name,
            responseModel.Name,
            responseModel.Constructor,
            responseModel.IsEnumerable,
            responseModel.IsObservable,
            imports.ToOrderedImports(),
            parameters.OrderBy(_ => _.Name),
            [.. parameters.Where(_ => !_.IsOptional).OrderBy(_ => _.Name)],
            properties.OrderBy(_ => _.Name),
            [.. typesInvolved.Concat(additionalTypesInvolved).Distinct().OrderBy(_ => _.FullName)],
            documentation,
            validationRules.OrderBy(_ => _.PropertyName),
            treatWarningsAsErrors,
            roles,
            httpMethod);
    }

    /// <summary>
    /// Finds a class that stands in for a query method's parameters, by the naming convention
    /// <c>{MethodName}Parameters</c> or <c>{ReadModelName}{MethodName}Parameters</c>, so an explicit
    /// <c>QueryValidator&lt;T&gt;</c> can be declared against it.
    /// </summary>
    /// <param name="readModelType">The read model type owning the query method.</param>
    /// <param name="method">The query method to find a parameters class for.</param>
    /// <returns>The matching <see cref="Type"/>, or null when there is none.</returns>
    /// <remarks>
    /// A candidate only counts when every method parameter has a matching property, so an unrelated type that happens
    /// to carry the name is never picked up.
    /// </remarks>
    static Type? FindParametersTypeFor(Type readModelType, MethodInfo method)
    {
        string[] parameterTypeNames =
        [
            $"{method.Name}Parameters",
            $"{readModelType.Name}{method.Name}Parameters"
        ];

        var methodParameters = method.GetParameters();

        foreach (var typeName in parameterTypeNames)
        {
            var candidateType = readModelType.Assembly.GetTypes().FirstOrDefault(_ => _.Name == typeName);
            if (candidateType is null)
            {
                continue;
            }

            var candidateProperties = candidateType.GetProperties();
            var allParametersMatch = methodParameters.All(parameter =>
                candidateProperties.Any(property =>
                    property.Name.Equals(parameter.Name, StringComparison.OrdinalIgnoreCase) &&
                    property.PropertyType == parameter.ParameterType));

            if (allParametersMatch)
            {
                return candidateType;
            }
        }

        return null;
    }

    /// <summary>
    /// Get query parameter descriptors from a method - primitives, concepts and enumerables of primitives/concepts are included.
    /// </summary>
    /// <param name="method">Method to get parameters for.</param>
    /// <returns>Collection of <see cref="RequestParameterDescriptor"/>.</returns>
    static IEnumerable<RequestParameterDescriptor> GetQueryParameterDescriptors(this MethodInfo method)
    {
        var parameters = method.GetParameters();

        // Include primitive types, concepts and enumerables of primitive/concept types as query parameters.
        // Everything else is assumed to be a dependency.
        var queryParameters = parameters.Where(p =>
            p.ParameterType.IsAPrimitiveType() ||
            p.ParameterType.IsConcept() ||
            p.ParameterType.IsEnumerableOfPrimitiveOrConcept());

        return queryParameters.Select(p => p.ToQueryRequestParameterDescriptor());
    }

    /// <summary>
    /// Get query property descriptors from a method - primitives, concepts and enumerables of primitives/concepts are included.
    /// </summary>
    /// <param name="method">Method to get properties for.</param>
    /// <returns>Collection of <see cref="PropertyDescriptor"/>.</returns>
    static IEnumerable<PropertyDescriptor> GetQueryPropertyDescriptors(this MethodInfo method)
    {
        var parameters = method.GetParameters();

        // Include primitive types, concepts and enumerables of primitive/concept types as query properties.
        var queryParameters = parameters.Where(p =>
            p.ParameterType.IsAPrimitiveType() ||
            p.ParameterType.IsConcept() ||
            p.ParameterType.IsEnumerableOfPrimitiveOrConcept());

        return queryParameters.Select(p => p.ToPropertyDescriptor());
    }

    /// <summary>
    /// Convert a <see cref="ParameterInfo"/> to a <see cref="RequestParameterDescriptor"/> for queries.
    /// </summary>
    /// <param name="parameterInfo">Parameter to convert.</param>
    /// <returns>Converted <see cref="RequestParameterDescriptor"/>.</returns>
    static RequestParameterDescriptor ToQueryRequestParameterDescriptor(this ParameterInfo parameterInfo)
    {
        var paramType = parameterInfo.ParameterType;
        var isEnumerable = paramType.IsEnumerableOfPrimitiveOrConcept();

        if (isEnumerable)
        {
            paramType = paramType.GetEnumerableElementType();
        }

        var type = paramType.GetTargetType();
        var optional = parameterInfo.IsOptional() || parameterInfo.HasDefaultValue;
        var documentation = parameterInfo.GetDocumentation();

        // All query parameters are considered query string parameters
        return new RequestParameterDescriptor(paramType, parameterInfo.Name!, type.Type, type.Constructor, optional, true, isEnumerable, documentation);
    }

    /// <summary>
    /// Check if a parameter is optional for model bound queries.
    /// </summary>
    /// <param name="parameter">Parameter to check.</param>
    /// <returns>True if it is optional, false if not.</returns>
    static bool IsOptional(this ParameterInfo parameter)
    {
        if (parameter.HasDefaultValue)
        {
            return true;
        }

        if (parameter.ParameterType.IsValueType)
        {
            return parameter.ParameterType.IsNullable();
        }

        var context = new NullabilityInfoContext();
        var nullabilityInfo = context.Create(parameter);
        return nullabilityInfo.WriteState == NullabilityState.Nullable;
    }
}
