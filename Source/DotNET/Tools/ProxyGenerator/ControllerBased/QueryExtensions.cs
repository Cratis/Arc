// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

        // Extract validation rules from query method parameters
        // Try to find a FluentValidation validator by looking for a type whose properties match the query parameters
        var validationRules = new List<PropertyValidationDescriptor>();

        // Look for a type in the assembly whose properties match the method parameters
        // Prefer types with "Query" in the name to avoid false matches
        var methodParams = method.GetParameters();
        var matchingTypes = method.DeclaringType!.Assembly.GetTypes()
            .Where(t =>
            {
                var properties = t.GetProperties();
                if (properties.Length == 0 || properties.Length != methodParams.Length)
                {
                    return false;
                }

                // Check if all properties on the type correspond to method parameters
                return properties.All(prop =>
                    methodParams.Any(param =>
                        param.Name.Equals(prop.Name, StringComparison.OrdinalIgnoreCase) &&
                        param.ParameterType == prop.PropertyType));
            })
            .OrderByDescending(t => t.Name.Contains("Query", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var matchingType = matchingTypes.FirstOrDefault();

        if (matchingType != null)
        {
            // Found a matching DTO type, extract FluentValidation rules from it
            var fluentValidationRules = ValidationRulesExtractor.ExtractValidationRules(method.DeclaringType.Assembly, matchingType);
            validationRules.AddRange(fluentValidationRules);
        }

        // If no FluentValidation rules found, fall back to DataAnnotations on method parameters
        if (validationRules.Count == 0)
        {
            foreach (var param in method.GetParameters())
            {
                var rules = ValidationRulesExtractor.ExtractDataAnnotationsFromParameter(param);
                if (rules.Count > 0)
                {
                    validationRules.Add(new PropertyValidationDescriptor(param.Name.ToCamelCase(), [.. rules]));
                }
            }
        }

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
            validationRules.OrderBy(_ => _.PropertyName));
    }
}
