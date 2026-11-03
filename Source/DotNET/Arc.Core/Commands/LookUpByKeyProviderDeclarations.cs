// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.Commands;

/// <summary>
/// Reads lookup metadata across an override chain without constructing attributes or providers.
/// </summary>
internal static class LookUpByKeyProviderDeclarations
{
    /// <summary>Gets lookup declarations from the parameter and its override chain.</summary>
    /// <param name="parameter">The injected parameter.</param>
    /// <returns>The declared lookup metadata.</returns>
    internal static IEnumerable<CustomAttributeData> For(ParameterInfo parameter)
    {
        foreach (var declaration in parameter.GetCustomAttributesData().Where(IsLookup)) yield return declaration;
        if (parameter.Member is not MethodInfo { IsVirtual: true } method) yield break;

        var definition = method.GetBaseDefinition();
        for (var type = method.DeclaringType?.BaseType; type is not null; type = type.BaseType)
        {
            var overridden = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .FirstOrDefault(candidate => candidate.IsVirtual && candidate.GetBaseDefinition() == definition);
            if (overridden is null) continue;
            foreach (var declaration in overridden.GetParameters()[parameter.Position].GetCustomAttributesData().Where(IsLookup)) yield return declaration;
        }
    }

    /// <summary>Tests whether a declaration selects a key provider.</summary>
    /// <param name="declaration">The lookup metadata.</param>
    /// <returns>Whether the declaration supplies a key provider.</returns>
    internal static bool IsProvider(CustomAttributeData declaration) => typeof(ILookUpByKeyProvider).IsAssignableFrom(declaration.AttributeType);

    static bool IsLookup(CustomAttributeData declaration) => declaration.AttributeType == typeof(LookUpByAttribute) || IsProvider(declaration);
}
