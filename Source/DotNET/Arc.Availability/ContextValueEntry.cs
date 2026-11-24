// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability.Context;

namespace Cratis.Arc.Availability;

/// <summary>
/// A context value in the manifest: a 'provide' line.
/// </summary>
/// <param name="Name">The name of the value under '$context'.</param>
/// <param name="Type">The type of the value as authored. Empty when unknown.</param>
/// <param name="Origin">Where the value comes from, as authored. Empty when unknown.</param>
/// <param name="Scope">The level it is declared at.</param>
/// <param name="Required">Whether the value must exist.</param>
public record ContextValueEntry(string Name, string Type, string Origin, ContextScope Scope, bool Required)
{
    /// <summary>
    /// Creates the entry for a declaration. The provider's type name is the origin unless one is given.
    /// </summary>
    /// <param name="declaration">The declaration.</param>
    /// <param name="type">The type of the value as authored.</param>
    /// <param name="origin">Where the value comes from as authored; defaults to the provider type name.</param>
    /// <returns>The entry.</returns>
    public static ContextValueEntry From(ContextValueDeclaration declaration, string type = "", string? origin = null) =>
        new(declaration.Name, type, origin ?? declaration.Provider.GetType().Name, declaration.Scope, declaration.Required);
}
