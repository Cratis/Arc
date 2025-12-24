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
    /// <returns>Collection of converted <see cref="QueryDescriptor"/>.</returns>
    public static IEnumerable<QueryDescriptor> ToQueryDescriptors(
        this TypeInfo readModelType,
        string targetPath,
        int segmentsToSkip,
        bool skipQueryNameInRoute,
        string apiPrefix)
    {
        var queryMethods = readModelType.GetQueryMethods();
        var descriptors = new List<QueryDescriptor>();

        foreach (var method in queryMethods)
        {
            var descriptor = method.ToQueryDescriptor(readModelType, targetPath, segmentsToSkip, skipQueryNameInRoute, apiPrefix);
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
    /// <returns>Converted <see cref="QueryDescriptor"/>.</returns>
    public static QueryDescriptor ToQueryDescriptor(
        this MethodInfo method,
        TypeInfo readModelType,
        string targetPath,
        int segmentsToSkip,
        bool skipQueryNameInRoute,
        string apiPrefix)
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
        var baseUrl = $"/{apiPrefix}/{string.Join('/', segments)}";
        var route = skipQueryNameInRoute ? baseUrl : $"{baseUrl}/{method.Name.ToKebabCase()}".ToLowerInvariant();

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

        // Extract validation rules from query method parameters (not from the readModel/response type)
        var validationRules = new List<Templates.PropertyValidationDescriptor>();
        foreach (var param in method.GetParameters())
        {
            var rules = ValidationRulesExtractor.ExtractDataAnnotationsFromParameter(param);
            if (rules.Count > 0)
            {
                validationRules.Add(new Templates.PropertyValidationDescriptor(param.Name.ToCamelCase(), [.. rules]));
            }
        }

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
            parameters,
            [.. parameters.Where(_ => !_.IsOptional)],
            properties,
            [.. typesInvolved, .. additionalTypesInvolved],
            documentation,
            validationRules);
    }

    /// <summary>
    /// Get query parameter descriptors from a method - only primitives and concepts are included.
    /// </summary>
    /// <param name="method">Method to get parameters for.</param>
    /// <returns>Collection of <see cref="RequestParameterDescriptor"/>.</returns>
    static IEnumerable<RequestParameterDescriptor> GetQueryParameterDescriptors(this MethodInfo method)
    {
        var parameters = method.GetParameters();

        // Include only primitive types and concepts as query parameters
        // Everything else is assumed to be a dependency
        var queryParameters = parameters.Where(p => p.ParameterType.IsAPrimitiveType() || p.ParameterType.IsConcept());

        return queryParameters.Select(p => p.ToQueryRequestParameterDescriptor());
    }

    /// <summary>
    /// Get query property descriptors from a method - only primitives and concepts are included.
    /// </summary>
    /// <param name="method">Method to get properties for.</param>
    /// <returns>Collection of <see cref="PropertyDescriptor"/>.</returns>
    static IEnumerable<PropertyDescriptor> GetQueryPropertyDescriptors(this MethodInfo method)
    {
        var parameters = method.GetParameters();

        // Include only primitive types and concepts as query properties
        var queryParameters = parameters.Where(p => p.ParameterType.IsAPrimitiveType() || p.ParameterType.IsConcept());

        return queryParameters.Select(p => p.ToPropertyDescriptor());
    }

    /// <summary>
    /// Convert a <see cref="ParameterInfo"/> to a <see cref="RequestParameterDescriptor"/> for queries.
    /// </summary>
    /// <param name="parameterInfo">Parameter to convert.</param>
    /// <returns>Converted <see cref="RequestParameterDescriptor"/>.</returns>
    static RequestParameterDescriptor ToQueryRequestParameterDescriptor(this ParameterInfo parameterInfo)
    {
        var type = parameterInfo.ParameterType.GetTargetType();
        var optional = parameterInfo.IsOptional() || parameterInfo.HasDefaultValue;
        var documentation = parameterInfo.GetDocumentation();

        // All query parameters are considered query string parameters
        return new RequestParameterDescriptor(parameterInfo.ParameterType, parameterInfo.Name!, type.Type, type.Constructor, optional, true, documentation);
    }

    /// <summary>
    /// Check if a parameter is optional for model bound queries.
    /// </summary>
    /// <param name="parameter">Parameter to check.</param>
    /// <returns>True if it is optional, false if not.</returns>
    static bool IsOptional(this ParameterInfo parameter)
    {
        return parameter.ParameterType.IsNullable() || parameter.HasDefaultValue;
    }
}