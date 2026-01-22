// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_Identity.given;

/// <summary>
/// A reusable context for identity provider type generation scenarios.
/// </summary>
public class an_identity_provider_scenario : Specification
{
    /// <summary>
    /// Generates TypeScript code for a type descriptor.
    /// </summary>
    /// <param name="type">The type to generate code for.</param>
    /// <returns>The generated TypeScript code.</returns>
    protected static string GenerateTypeScript(Type type)
    {
        var descriptor = type.ToTypeDescriptor(string.Empty, 0);
        return InMemoryProxyGenerator.GenerateType(descriptor);
    }

    /// <summary>
    /// Generates TypeScript code for an enum descriptor.
    /// </summary>
    /// <param name="type">The enum type to generate code for.</param>
    /// <returns>The generated TypeScript code.</returns>
    protected static string GenerateEnumTypeScript(Type type)
    {
        var descriptor = type.ToEnumDescriptor();
        return InMemoryProxyGenerator.GenerateEnum(descriptor);
    }

    /// <summary>
    /// Collects all types involved with a root type, including nested complex types.
    /// </summary>
    /// <param name="rootType">The root type to collect from.</param>
    /// <returns>A list of all types involved.</returns>
    protected static IReadOnlyList<Type> CollectAllTypesInvolved(Type rootType)
    {
        var types = new List<Type>();
        rootType.CollectTypesInvolved(types);
        return types;
    }
}
