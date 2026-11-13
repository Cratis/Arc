// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Cratis.Arc.ProxyGenerator.Templates;
using Cratis.Arc.Queries;

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
        IEnumerable<TypeInfo> allQueryTypes) =>
        ToQueryDescriptors(readModelType, targetPath, segmentsToSkip, skipQueryNameInRoute, apiPrefix, allQueryTypes, null);

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
        IEnumerable<TypeInfo> allQueryTypes) =>
        ToQueryDescriptor(method, readModelType, targetPath, segmentsToSkip, skipQueryNameInRoute, apiPrefix, allQueryTypes, null);

    /// <summary>
    /// Converts queries within a metadata discovery scope.
    /// </summary>
    /// <param name="readModelType">The read model type.</param>
    /// <param name="targetPath">The output path.</param>
    /// <param name="segmentsToSkip">The namespace segments to skip.</param>
    /// <param name="skipQueryNameInRoute">Whether to skip the query name in the route.</param>
    /// <param name="apiPrefix">The API prefix.</param>
    /// <param name="allQueryTypes">All query types for route conflict detection.</param>
    /// <param name="findAttribute">The discovery-scoped marker probe.</param>
    /// <returns>The query descriptors.</returns>
    internal static IEnumerable<QueryDescriptor> ToQueryDescriptors(
        this TypeInfo readModelType,
        string targetPath,
        int segmentsToSkip,
        bool skipQueryNameInRoute,
        string apiPrefix,
        IEnumerable<TypeInfo> allQueryTypes,
        Func<MemberInfo, string, bool, CustomAttributeData?>? findAttribute)
    {
        var queryMethods = readModelType.GetQueryMethods(findAttribute);
        var descriptors = new List<QueryDescriptor>();

        foreach (var method in queryMethods)
        {
            var descriptor = method.ToQueryDescriptor(readModelType, targetPath, segmentsToSkip, skipQueryNameInRoute, apiPrefix, allQueryTypes, findAttribute);
            descriptors.Add(descriptor);
        }

        return descriptors;
    }

    /// <summary>
    /// Converts queries within a metadata discovery scope.
    /// </summary>
    /// <param name="method">The query method.</param>
    /// <param name="readModelType">The read model type.</param>
    /// <param name="targetPath">The output path.</param>
    /// <param name="segmentsToSkip">The namespace segments to skip.</param>
    /// <param name="skipQueryNameInRoute">Whether to skip the query name in the route.</param>
    /// <param name="apiPrefix">The API prefix.</param>
    /// <param name="allQueryTypes">All query types for route conflict detection.</param>
    /// <param name="findAttribute">The discovery-scoped marker probe.</param>
    /// <returns>The query descriptors.</returns>
    internal static QueryDescriptor ToQueryDescriptor(
        this MethodInfo method,
        TypeInfo readModelType,
        string targetPath,
        int segmentsToSkip,
        bool skipQueryNameInRoute,
        string apiPrefix,
        IEnumerable<TypeInfo> allQueryTypes,
        Func<MemberInfo, string, bool, CustomAttributeData?>? findAttribute)
    {
        var typesInvolved = new List<Type>();

        var responseModel = GetResponseModel(method);

        if (!responseModel.Type.IsKnownType())
        {
            typesInvolved.Add(responseModel.Type);
        }

        var parameters = method.GetQueryParameterDescriptors();
        var properties = responseModel.Type.GetPropertyDescriptors();

        var parameterTypes = parameters.Select(parameter => GetParameterDependencyType(parameter.OriginalType)).ToArray();
        var parameterWithComplexTypes = parameterTypes.Where(type => !type.IsKnownType());
        typesInvolved.AddRange(parameterWithComplexTypes);

        var routing = new RouteContext(readModelType, segmentsToSkip, apiPrefix, allQueryTypes);
        var includeQueryName = !skipQueryNameInRoute || HasRouteConflict(routing, findAttribute);
        var route = ResolveRoute(method, routing, includeQueryName);

        var paths = new ImportPaths(targetPath, readModelType.ResolveTargetPath(segmentsToSkip), segmentsToSkip);
        var imports = BuildImports(typesInvolved, parameterTypes, paths);
        var additionalTypesInvolved = CollectAdditionalTypesInvolved(parameterWithComplexTypes, properties);

        var documentation = method.GetDocumentation();
        var validationRules = ExtractValidationRules(readModelType, method);

        // Check for TreatWarningsAsErrors attribute
        var treatWarningsAsErrors = FindAttributeData(method, readModelType, a => a.AttributeType.Name == "TreatWarningsAsErrorsAttribute") is not null;

        // Extract roles from authorization attributes on the method and its declaring type (read model)
        var roles = method.GetRoles().ToArray();
        var httpMethod = ResolveHttpMethod(method, readModelType);

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
            httpMethod)
        {
            SliceLocation = readModelType.ResolveSliceLocation(segmentsToSkip)
        };
    }

    /// <summary>
    /// Finds the type modelling a query method's argument set, so an explicit <c>QueryValidator&lt;T&gt;</c> can be
    /// declared against it.
    /// </summary>
    /// <param name="readModelType">The read model type owning the query method.</param>
    /// <param name="method">The query method to find an argument model for.</param>
    /// <returns>The matching <see cref="Type"/>, or null when there is none.</returns>
    /// <remarks>
    /// Defers to <see cref="QueryArgumentsModelConvention"/>, the single source both this and the framework's
    /// <c>QueryArgumentsModels</c> compile in, so the client and the server cannot resolve different types for the
    /// same query. Injected dependencies are excluded here — they are not arguments the caller supplies, and the
    /// framework never sees them at all.
    /// </remarks>
    static Type? FindParametersTypeFor(Type readModelType, MethodInfo method) =>
        QueryArgumentsModelConvention.Resolve(
            readModelType.Name,
            method.Name,
            [.. method.GetParameters().Where(IsQueryParameter).Select(_ => new QueryArgumentDescriptor(_.Name ?? string.Empty, _.ParameterType))],
            readModelType.Assembly.GetTypes());

    /// <summary>
    /// Determines whether a parameter is an argument the caller supplies rather than an injected dependency.
    /// </summary>
    /// <param name="parameter">The <see cref="ParameterInfo"/> to check.</param>
    /// <returns>True when the parameter is a query argument; otherwise false.</returns>
    /// <remarks>
    /// Enums are included explicitly. Everything this predicate rejects is assumed to be an injected dependency, so an
    /// omission here does not merely skip a parameter - it silently reclassifies a caller-supplied argument as a
    /// dependency, and the generated proxy has no way to pass it. A nullable enum already qualified by way of
    /// <c>IsNullable</c>, which made <c>Status?</c> work while <c>Status</c> did not.
    /// </remarks>
    static bool IsQueryParameter(ParameterInfo parameter) =>
        parameter.ParameterType.IsAPrimitiveType() ||
        parameter.ParameterType.IsConcept() ||
        parameter.ParameterType.IsEnum ||
        parameter.ParameterType.IsDictionaryOfQueryArgumentValues(out _) ||
        parameter.ParameterType.IsEnumerableOfPrimitiveOrConcept();

    /// <summary>
    /// Get query parameter descriptors from a method - primitives, concepts and enumerables of primitives/concepts are included.
    /// </summary>
    /// <param name="method">Method to get parameters for.</param>
    /// <returns>Collection of <see cref="RequestParameterDescriptor"/>.</returns>
    static IEnumerable<RequestParameterDescriptor> GetQueryParameterDescriptors(this MethodInfo method)
    {
        // Everything that is not a query argument is assumed to be a dependency.
        return method.GetParameters().Where(IsQueryParameter).Select(p => p.ToQueryRequestParameterDescriptor());
    }

    static Type GetParameterDependencyType(Type type) =>
        type.IsDictionaryOfQueryArgumentValues(out var valueType)
            ? valueType.GetDictionaryEffectiveValueType()
            : type;

    /// <summary>
    /// Convert a <see cref="ParameterInfo"/> to a <see cref="RequestParameterDescriptor"/> for queries.
    /// </summary>
    /// <param name="parameterInfo">Parameter to convert.</param>
    /// <returns>Converted <see cref="RequestParameterDescriptor"/>.</returns>
    /// <remarks>
    /// <c>Nullable&lt;T&gt;</c> is unwrapped before anything else is derived from the type. Left wrapped, a nullable
    /// enum's <see cref="RequestParameterDescriptor.OriginalType"/> stays <c>Nullable&lt;TEnum&gt;</c> rather than
    /// <c>TEnum</c> - <see cref="TypeExtensions.IsKnownType"/> unwraps <c>Nullable&lt;T&gt;</c> before consulting the
    /// primitive map, but an enum is never in that map either way, so the wrapper type (not the enum) is what
    /// <see cref="ToQueryDescriptor(MethodInfo, TypeInfo, string, int, bool, string, IEnumerable{TypeInfo})"/> collects into <c>TypesInvolved</c>. <c>Nullable&lt;TEnum&gt;</c>
    /// is not itself an enum, so it is missed by the enum split in the generator and instead run through
    /// <see cref="TypeExtensions.ToTypeDescriptor"/> as if it were a plain class - reflecting <c>Nullable&lt;T&gt;</c>'s
    /// own <c>HasValue</c>/<c>Value</c> properties into a bogus emitted type that collides with the enum's real name.
    /// Optionality already comes from <see cref="IsOptional(ParameterInfo)"/>,
    /// not from the parameter's CLR type, so unwrapping here cannot change whether the parameter is treated as optional.
    /// </remarks>
    static RequestParameterDescriptor ToQueryRequestParameterDescriptor(this ParameterInfo parameterInfo)
    {
        var paramType = parameterInfo.ParameterType;

        if (paramType.IsNullable())
        {
            paramType = paramType.GetGenericArguments()[0];
        }

        var isEnumerable = !paramType.IsDictionaryOfQueryArgumentValues(out _) && paramType.IsEnumerableOfPrimitiveOrConcept();

        if (isEnumerable)
        {
            paramType = paramType.GetEnumerableElementType();
        }

        var type = paramType.GetTargetType();
        var optional = parameterInfo.IsOptional();
        var documentation = parameterInfo.GetDocumentation();

        // All query parameters are considered query string parameters
        return new RequestParameterDescriptor(paramType, parameterInfo.Name!, type.Type, type.Constructor, optional, true, isEnumerable, documentation);
    }

    /// <summary>
    /// Check if a parameter is optional for model bound queries.
    /// </summary>
    /// <param name="parameter">Parameter to check.</param>
    /// <returns>True if it is optional, false if not.</returns>
    static bool IsOptional(this ParameterInfo parameter) => ParameterNullability.IsOptional(parameter);

    static ModelDescriptor GetResponseModel(MethodInfo method)
    {
        var returnType = method.ReturnType;
        if (returnType.IsAssignableTo<Task>() && returnType.IsGenericType)
        {
            return returnType.GetGenericArguments()[0].ToModelDescriptor();
        }

        if (returnType != TypeExtensions._voidType && returnType != TypeExtensions._taskType)
        {
            return returnType.ToModelDescriptor();
        }

        return ModelDescriptor.Empty;
    }

    static CustomAttributeData? FindAttributeData(MemberInfo method, MemberInfo readModelType, Func<CustomAttributeData, bool> predicate) =>
        method.GetCustomAttributesData().FirstOrDefault(predicate) ??
        readModelType.GetCustomAttributesData().FirstOrDefault(predicate);

    static bool HasRouteConflict(RouteContext routing, Func<MemberInfo, string, bool, CustomAttributeData?>? findAttribute)
    {
        var namespaceKey = string.Join('.', (routing.ReadModelType.Namespace?.Split('.') ?? []).Skip(routing.SegmentsToSkip));
        var queriesInSameNamespace = routing.AllQueryTypes.Where(t => string.Join('.', (t.Namespace?.Split('.') ?? []).Skip(routing.SegmentsToSkip)) == namespaceKey);
        var totalMethodsInNamespace = queriesInSameNamespace.Sum(t => t.GetQueryMethods(findAttribute).Count());
        return totalMethodsInNamespace > 1;
    }

    static string BuildBaseUrl(RouteContext routing)
    {
        var location = routing.ReadModelType.Namespace?.Split('.') ?? [];
        var segments = location.Skip(routing.SegmentsToSkip).Select(segment => segment.ToKebabCase());
        return $"/{routing.ApiPrefix}/{string.Join('/', segments)}".TrimEnd('/');
    }

    static string ResolveRoute(MethodInfo method, RouteContext routing, bool includeQueryName)
    {
        // Check for Path attribute on method or type
        var routeAttributeData = FindAttributeData(method, routing.ReadModelType, a => a.AttributeType.FullName == "Cratis.Arc.Queries.ModelBound.PathAttribute");

        var customRouteValue = routeAttributeData is { ConstructorArguments.Count: > 0 }
            ? routeAttributeData.ConstructorArguments[0].Value as string
            : null;

        // Use the custom route from the attribute when present, otherwise use conventional route generation
        if (!string.IsNullOrEmpty(customRouteValue)) return customRouteValue;

        var baseUrl = BuildBaseUrl(routing);
        var route = includeQueryName ? $"{baseUrl}/{method.Name.ToKebabCase()}" : baseUrl;
        return route.ToLowerInvariant();
    }

    static List<ImportStatement> BuildImports(List<Type> typesInvolved, IEnumerable<Type> parameterTypes, ImportPaths paths)
    {
        var imports = typesInvolved
                        .GetImports(paths.TargetPath, paths.RelativePath, paths.SegmentsToSkip)
                        .DistinctBy(_ => _.Type)
                        .ToList();

        imports.AddRange(parameterTypes.Where(type => type.HasModule()).Select(type => type.GetImportStatement(paths.TargetPath, paths.RelativePath, paths.SegmentsToSkip)));

        return [.. imports.DistinctBy(_ => _.Type)];
    }

    static List<Type> CollectAdditionalTypesInvolved(IEnumerable<Type> parameterWithComplexTypes, IEnumerable<PropertyDescriptor> properties)
    {
        var additionalTypesInvolved = new List<Type>();
        foreach (var parameter in parameterWithComplexTypes)
        {
            parameter.CollectTypesInvolved(additionalTypesInvolved);
        }

        foreach (var property in properties)
        {
            property.CollectTypesInvolved(additionalTypesInvolved);
        }

        return additionalTypesInvolved;
    }

    /// <summary>
    /// Extracts validation rules for the query's parameters from the three sources that can contribute them, merged
    /// with the same precedence the command path uses: an explicit validator for a matching parameters class, the
    /// validators of any concept-typed parameters, and DataAnnotations on the parameters as a per-parameter fallback.
    /// </summary>
    /// <param name="readModelType">The read model type owning the query method.</param>
    /// <param name="method">The query method.</param>
    /// <returns>The merged validation rules.</returns>
    static IEnumerable<PropertyValidationDescriptor> ExtractValidationRules(TypeInfo readModelType, MethodInfo method)
    {
        var parametersType = FindParametersTypeFor(readModelType, method);

        // The convention matches CLR types, not nullable-reference annotations. Infer concept rules only from
        // the actual parameters so a stricter argument-model annotation cannot reintroduce presence rules.
        var explicitRules = parametersType is not null
            ? ValidationRulesExtractor.ExtractValidationRules(readModelType.Assembly, parametersType, includeConceptRules: false, includeDataAnnotations: false).ToList()
            : [];

        var parameters = method.GetParameters();
        var conceptRules = parameters.Select(param => ConceptRulesFor(readModelType.Assembly, param)).OfType<PropertyValidationDescriptor>().ToList();

        // Keep argument-model annotations at fallback precedence, below rules inferred from the actual parameters.
        var dataAnnotationsRules = parametersType is not null
            ? ValidationRulesExtractor.ExtractDataAnnotationsRules(parametersType).ToList()
            : [];
        dataAnnotationsRules.AddRange(parameters.Select(DataAnnotationsRulesFor).OfType<PropertyValidationDescriptor>());

        return ValidationRulesExtractor.MergeValidationRules(explicitRules, conceptRules, dataAnnotationsRules);
    }

    static PropertyValidationDescriptor? ConceptRulesFor(Assembly assembly, ParameterInfo param)
    {
        var isOptional = param.IsOptional() && !param.GetCustomAttributesData().Any(_ => _.AttributeType.FullName == typeof(RequiredAttribute).FullName);
        var rulesFromConcept = ValidationRulesExtractor.ExtractRulesForConceptType(assembly, param.ParameterType, isOptional);
        return rulesFromConcept.Count > 0
            ? new PropertyValidationDescriptor(param.Name.ToCamelCase(), [.. rulesFromConcept])
            : null;
    }

    static PropertyValidationDescriptor? DataAnnotationsRulesFor(ParameterInfo param)
    {
        var rulesFromDataAnnotations = ValidationRulesExtractor.ExtractDataAnnotationsFromParameter(param);
        return rulesFromDataAnnotations.Count > 0
            ? new PropertyValidationDescriptor(param.Name.ToCamelCase(), [.. rulesFromDataAnnotations])
            : null;
    }

    /// <summary>
    /// Resolves the QueryHttpMethod attribute on the method (wins) or the read-model type; the enum member name
    /// (Get/Query/Auto) matches the TypeScript QueryHttpMethod enum and is emitted into the proxy.
    /// </summary>
    /// <param name="method">The query method.</param>
    /// <param name="readModelType">The read model type owning the query method.</param>
    /// <returns>The enum member name, or null when no attribute is present.</returns>
    static string? ResolveHttpMethod(MethodInfo method, TypeInfo readModelType)
    {
        var httpMethodAttr = FindAttributeData(method, readModelType, a => a.AttributeType.Name == "QueryHttpMethodAttribute");
        return httpMethodAttr is { ConstructorArguments.Count: > 0 }
            ? Enum.GetName(httpMethodAttr.ConstructorArguments[0].ArgumentType, httpMethodAttr.ConstructorArguments[0].Value!)
            : null;
    }

    sealed record RouteContext(TypeInfo ReadModelType, int SegmentsToSkip, string ApiPrefix, IEnumerable<TypeInfo> AllQueryTypes);

    sealed record ImportPaths(string TargetPath, string RelativePath, int SegmentsToSkip);
}
