// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator;

/// <summary>Extension methods for command validation extraction.</summary>
public static partial class CommandExtensions
{
    /// <summary>
    /// Fall back to extracting from the method's command parameter only when no rules were supplied at all. A
    /// caller that supplied an empty set - a model-bound command whose validator holds no client-projectable rule
    /// - means exactly that; treating empty as "not supplied" would re-extract from GetCommandType, which for
    /// a model-bound Handle(target) resolves to the provided-value type rather than the command.
    /// </summary>
    /// <param name="method">The command method.</param>
    /// <returns>The extracted validation rules.</returns>
    static IEnumerable<PropertyValidationDescriptor> ExtractCommandValidationRules(MethodInfo method)
    {
        var commandType = method.GetCommandType();
        return commandType is not null
            ? ValidationRulesExtractor.ExtractValidationRules(method.DeclaringType!.Assembly, commandType)
            : [];
    }

    static IEnumerable<PropertyValidationDescriptor> ResolveValidationRules(MethodInfo method, IEnumerable<PropertyValidationDescriptor>? supplied, string name, string route)
    {
        using var recording = ValidationProjectionRecorder.IsEnabled ? ValidationProjectionRecorder.Begin(new("command", name, route)) : null;
        var rules = supplied ?? ExtractCommandValidationRules(method);
        ValidationProjectionRecorder.CompleteEndpoint();
        return rules;
    }
}
