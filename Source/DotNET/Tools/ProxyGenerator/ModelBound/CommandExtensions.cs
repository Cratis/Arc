// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.ModelBound;

/// <summary>
/// Extensions for command types.
/// </summary>
public static class CommandExtensions
{
    /// <summary>
    /// Convert a command <see cref="TypeInfo"/> to a <see cref="CommandDescriptor"/>.
    /// </summary>
    /// <param name="commandType">Command type to convert.</param>
    /// <param name="targetPath">The target path the proxies are generated to.</param>
    /// <param name="segmentsToSkip">Number of segments to skip from the namespace when generating the output path.</param>
    /// <param name="skipCommandNameInRoute">True if the command name should be skipped in the route, false if not.</param>
    /// <param name="apiPrefix">The API prefix to use in the route.</param>
    /// <param name="allCommandTypes">Collection of all command types to detect conflicts.</param>
    /// <returns>Converted <see cref="CommandDescriptor"/>.</returns>
    public static CommandDescriptor ToCommandDescriptor(
        this TypeInfo commandType,
        string targetPath,
        int segmentsToSkip,
        bool skipCommandNameInRoute,
        string apiPrefix,
        IEnumerable<TypeInfo> allCommandTypes)
    {
        var properties = commandType.GetPropertyDescriptors();
        var location = commandType.Namespace?.Split('.') ?? [];
        var segments = location.Skip(segmentsToSkip).Select(segment => segment.ToKebabCase());
        var baseUrl = $"/{apiPrefix}/{string.Join('/', segments)}";

        var namespaceKey = string.Join('.', location.Skip(segmentsToSkip));
        var hasConflict = allCommandTypes.Count(t => string.Join('.', (t.Namespace?.Split('.') ?? []).Skip(segmentsToSkip)) == namespaceKey) > 1;
        var includeCommandName = !skipCommandNameInRoute || hasConflict;
        var route = includeCommandName ? $"{baseUrl}/{commandType.Name.ToKebabCase()}" : baseUrl;
        route = route.ToLowerInvariant();
        var handleMethod = commandType.GetHandleMethod();

        // For model-bound commands, we want the documentation from the command type, not the Handle method
        var documentation = commandType.GetDocumentation();

        // Extract validation rules for the command type
        var validationRules = ValidationRulesExtractor.ExtractValidationRules(commandType.Assembly, commandType);

        return handleMethod.ToCommandDescriptor(commandType.Name, properties, [], route, targetPath, segmentsToSkip, documentation, validationRules);
    }
}