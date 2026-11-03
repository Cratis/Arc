// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.Commands;

/// <summary>
/// Reads inherited lookup declarations across an override chain without constructing providers.
/// </summary>
internal static class LookUpByKeyProviderDeclarations
{
    /// <summary>Gets lookup declarations from the parameter and its override chain.</summary>
    /// <param name="parameter">The injected parameter.</param>
    /// <returns>The declared lookup metadata.</returns>
    internal static IEnumerable<Attribute> For(ParameterInfo parameter) =>
        Attribute.GetCustomAttributes(parameter, typeof(LookUpByAttribute), inherit: true)
            .Concat(Attribute.GetCustomAttributes(parameter, typeof(LookUpByAttribute<>), inherit: true))
            .Where(IsLookup);

    /// <summary>Tests whether a declaration selects a key provider.</summary>
    /// <param name="declaration">The lookup metadata.</param>
    /// <returns>Whether the declaration supplies a key provider.</returns>
    internal static bool IsProvider(Attribute declaration) => declaration is ILookUpByKeyProvider;

    static bool IsLookup(Attribute declaration) => declaration is LookUpByAttribute || IsProvider(declaration);
}
