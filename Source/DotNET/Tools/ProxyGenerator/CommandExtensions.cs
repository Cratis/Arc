// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator;

/// <summary>
/// Extension methods for working with commands.
/// </summary>
public static class CommandExtensions
{
    /// <summary>
    /// Convert a <see cref="MethodInfo"/> to a <see cref="CommandDescriptor"/>.
    /// </summary>
    /// <param name="method">Method to convert.</param>
    /// <param name="commandName">Name of the command.</param>
    /// <param name="properties">Properties of the command.</param>
    /// <param name="parameters">Parameters for the command.</param>
    /// <param name="route">Route of the command.</param>
    /// <param name="targetPath">The target path the proxies are generated to.</param>
    /// <param name="segmentsToSkip">Number of segments to skip from the namespace when generating the output path.</param>
    /// <param name="overrideDocumentation">Optional documentation to override the method's documentation (used for model-bound commands).</param>
    /// <returns>Converted <see cref="CommandDescriptor"/>.</returns>
    public static CommandDescriptor ToCommandDescriptor(this MethodInfo method, string commandName, IEnumerable<PropertyDescriptor> properties, IEnumerable<RequestParameterDescriptor> parameters, string route, string targetPath, int segmentsToSkip, string? overrideDocumentation = null)
    {
        var (hasResponse, responseModel) = method.GetResponseModel();
        var typesInvolved = new List<Type>();

        if (!(responseModel.Type?.IsKnownType() ?? true))
        {
            typesInvolved.Add(responseModel.Type);
        }

        var propertiesWithComplexTypes = properties.Where(_ => !_.OriginalType.IsKnownType());
        var propertiesNeedingImportStatements = properties.Where(_ => _.OriginalType.HasModule()).ToList();
        typesInvolved.AddRange(propertiesWithComplexTypes.Select(_ => _.OriginalType));
        var relativePath = method.DeclaringType!.ResolveTargetPath(segmentsToSkip);
        var imports = typesInvolved.GetImports(targetPath, relativePath, segmentsToSkip).ToList();
        imports.AddRange(propertiesNeedingImportStatements.Select(_ => _.OriginalType.GetImportStatement(targetPath, relativePath, segmentsToSkip)));

        if (responseModel.Type is not null && responseModel.Type.GetTargetType().TryGetImportStatement(out var responseTypeImportStatement))
        {
            imports.Add(responseTypeImportStatement);
        }
        var additionalTypesInvolved = new List<Type>();
        foreach (var property in propertiesWithComplexTypes)
        {
            property.CollectTypesInvolved(additionalTypesInvolved);
        }

        imports = [.. imports.DistinctBy(_ => _.Type)];

        // Use override documentation if provided (for model-bound commands), otherwise use method documentation
        var documentation = overrideDocumentation ?? method.GetDocumentation();

        // Extract validation rules for the command type
        var commandType = method.GetCommandType();
        var validationRules = commandType != null
            ? ValidationRulesExtractor.ExtractValidationRules(method.DeclaringType!.Assembly, commandType)
            : [];

        return new(
            method.DeclaringType!,
            method,
            route,
            commandName,
            properties,
            imports.OrderBy(_ => _.Module),
            parameters,
            hasResponse,
            responseModel,
            [.. typesInvolved, .. additionalTypesInvolved],
            documentation,
            validationRules);
    }
}
