// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.ControllerBased;

/// <summary>
/// Extension methods for working with commands.
/// </summary>
public static class QueryExtensions
{
    /// <summary>
    /// Convert a <see cref="MethodInfo"/> to a <see cref="CommandDescriptor"/>.
    /// </summary>
    /// <param name="method">Method to convert.</param>
    /// <param name="targetPath">The target path the proxies are generated to.</param>
    /// <param name="segmentsToSkip">Number of segments to skip from the namespace when generating the output path.</param>
    /// <returns>Converted <see cref="CommandDescriptor"/>.</returns>
    public static QueryDescriptor ToQueryDescriptor(this MethodInfo method, string targetPath, int segmentsToSkip)
    {
        var typesInvolved = new List<Type>();
        var arguments = method.GetParameterDescriptors();
        var responseModel = ResolveResponseModel(method);

        if (!responseModel.Type.IsKnownType())
        {
            typesInvolved.Add(responseModel.Type);
        }

        var argumentsWithComplexTypes = arguments.Where(_ => !_.OriginalType.IsKnownType());
        typesInvolved.AddRange(argumentsWithComplexTypes.Select(_ => _.OriginalType));

        var relativePath = method.DeclaringType!.ResolveTargetPath(segmentsToSkip);
        var imports = typesInvolved
                        .GetImports(targetPath, relativePath, segmentsToSkip)
                        .DistinctBy(_ => _.Type)
                        .ToList();

        var additionalTypesInvolved = new List<Type>();
        foreach (var argument in argumentsWithComplexTypes)
        {
            argument.CollectTypesInvolved(additionalTypesInvolved);
        }
        var argumentsNeedingImportStatements = arguments.Where(_ => _.OriginalType.HasModule()).ToList();
        imports.AddRange(argumentsNeedingImportStatements.Select(_ => _.OriginalType.GetImportStatement(targetPath, relativePath, segmentsToSkip)));

        var propertyDescriptors = responseModel.Type.GetPropertyDescriptors();
        foreach (var property in propertyDescriptors)
        {
            property.CollectTypesInvolved(additionalTypesInvolved);
        }

        imports = [.. imports.DistinctBy(_ => _.Type)];

        var route = method.GetRoute(arguments, includeQueryStringParameters: false);
        var documentation = method.GetDocumentation();

        var methodParams = method.GetParameters();
        var validationRules = ExtractValidationRules(method, methodParams, FindMatchingArgumentType(method, methodParams));
        var treatWarningsAsErrors = HasTreatWarningsAsErrors(method);

        // Extract roles from authorization attributes on the method and its declaring type
        var roles = method.GetRoles().ToArray();

        return new(
            method.DeclaringType!,
            method,
            route,
            method.Name,
            responseModel.Name,
            responseModel.Constructor,
            responseModel.IsEnumerable,
            responseModel.IsObservable,
            imports.ToOrderedImports(),
            arguments.OrderBy(_ => _.Name),
            [.. arguments.Where(_ => !_.IsOptional).OrderBy(_ => _.Name)],
            propertyDescriptors.OrderBy(_ => _.Name),
            [.. typesInvolved.Concat(additionalTypesInvolved).Distinct().OrderBy(_ => _.FullName)],
            documentation,
            validationRules.OrderBy(_ => _.PropertyName),
            treatWarningsAsErrors,
            roles,
            HttpMethod: null)
        {
            SliceLocation = method.DeclaringType!.ResolveSliceLocation(segmentsToSkip)
        };
    }

    static ModelDescriptor ResolveResponseModel(MethodInfo method)
    {
        if (method.ReturnType.IsAssignableTo<Task>() && method.ReturnType.IsGenericType)
        {
            return method.ReturnType.GetGenericArguments()[0].ToModelDescriptor();
        }

        return method.ReturnType != TypeExtensions._voidType && method.ReturnType != TypeExtensions._taskType
            ? method.ReturnType.ToModelDescriptor()
            : ModelDescriptor.Empty;
    }

    /// <summary>
    /// Look for a type in the assembly whose properties match the method parameters.
    /// Prefer types with "Query" in the name to avoid false matches.
    /// </summary>
    /// <param name="method">The query method.</param>
    /// <param name="methodParams">The parameters of the method.</param>
    /// <returns>The matching type, or <see langword="null"/> when there is none.</returns>
    static Type? FindMatchingArgumentType(MethodInfo method, ParameterInfo[] methodParams) =>
        method.DeclaringType!.Assembly.GetTypes()
            .Where(_ => MatchesParameters(_, methodParams))
            .OrderByDescending(_ => _.Name.Contains("Query", StringComparison.OrdinalIgnoreCase))
            .FirstOrDefault();

    static bool MatchesParameters(Type type, ParameterInfo[] methodParams)
    {
        var properties = type.GetProperties();
        if (properties.Length == 0 || properties.Length != methodParams.Length)
        {
            return false;
        }

        // Check if all properties on the type correspond to method parameters
        return properties.All(prop =>
            methodParams.Any(param =>
                param.Name.Equals(prop.Name, StringComparison.OrdinalIgnoreCase) &&
                param.ParameterType == prop.PropertyType));
    }

    static List<PropertyValidationDescriptor> ExtractValidationRules(MethodInfo method, ParameterInfo[] methodParams, Type? matchingType)
    {
        // Matching CLR types need not agree on nullable-reference annotations. Only extract explicit rules
        // from the DTO; the actual parameter's optionality governs inferred concept rules.
        var explicitRules = matchingType is not null
            ? ValidationRulesExtractor.ExtractValidationRules(method.DeclaringType!.Assembly, matchingType, includeConceptRules: false, includeDataAnnotations: false).ToList()
            : [];
        var dataAnnotationsRules = matchingType is not null
            ? ValidationRulesExtractor.ExtractDataAnnotationsRules(matchingType).ToList()
            : [];

        // Controller queries only infer concept rules when a matching argument DTO exists.
        var conceptRules = matchingType is not null
            ? ExtractConceptRules(method, methodParams)
            : [];

        var validationRules = ValidationRulesExtractor.MergeValidationRules(explicitRules, conceptRules, dataAnnotationsRules).ToList();

        // Direct parameter annotations are a whole-DTO fallback, not a per-parameter addition.
        return validationRules.Count == 0 ? ExtractParameterAnnotationRules(methodParams) : validationRules;
    }

    static List<PropertyValidationDescriptor> ExtractConceptRules(MethodInfo method, ParameterInfo[] methodParams)
    {
        var conceptRules = new List<PropertyValidationDescriptor>();
        foreach (var param in methodParams)
        {
            var isOptional = param.IsOptional() && !param.GetCustomAttributesData().Any(_ => _.AttributeType.FullName == typeof(RequiredAttribute).FullName);
            var rulesFromConcept = ValidationRulesExtractor.ExtractRulesForConceptType(method.DeclaringType!.Assembly, param.ParameterType, isOptional);
            if (rulesFromConcept.Count > 0)
            {
                conceptRules.Add(new PropertyValidationDescriptor(param.Name.ToCamelCase(), [.. rulesFromConcept]));
            }
        }

        return conceptRules;
    }

    static List<PropertyValidationDescriptor> ExtractParameterAnnotationRules(ParameterInfo[] methodParams)
    {
        var validationRules = new List<PropertyValidationDescriptor>();
        foreach (var param in methodParams)
        {
            var rules = ValidationRulesExtractor.ExtractDataAnnotationsFromParameter(param);
            if (rules.Count > 0)
            {
                validationRules.Add(new PropertyValidationDescriptor(param.Name.ToCamelCase(), [.. rules]));
            }
        }

        return validationRules;
    }

    static bool HasTreatWarningsAsErrors(MethodInfo method) =>
        HasTreatWarningsAsErrorsAttribute(method.GetCustomAttributesData()) ||
        (method.DeclaringType is not null && HasTreatWarningsAsErrorsAttribute(method.DeclaringType.GetCustomAttributesData()));

    static bool HasTreatWarningsAsErrorsAttribute(IEnumerable<CustomAttributeData> attributes) =>
        attributes.Any(_ => _.AttributeType.Name == "TreatWarningsAsErrorsAttribute");
}
