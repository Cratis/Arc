// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Analysis.Aggregates;

/// <summary>
/// Recognizes the types that govern a state change from inside a class.
/// </summary>
public static class AggregateRoots
{
    /// <summary>
    /// Determines whether a type is an aggregate root.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True when the type is an aggregate root.</returns>
    public static bool Is(ITypeSymbol? type) =>
        type.FindBase(WellKnownTypeNames.AggregateRoot) is not null ||
        type.FindInterface(WellKnownTypeNames.AggregateRootInterface) is not null;

    /// <summary>
    /// Determines whether a type declares behavior of its own rather than being the framework's own base type.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True when the type is an aggregate root an application wrote.</returns>
    /// <remarks>
    /// The base type declares <c>Apply</c> and <c>Commit</c>, which are how a state change is carried out rather than
    /// what it is. Only what the application declares on top of them says anything about the application.
    /// </remarks>
    public static bool IsDeclaredByApplication(ITypeSymbol? type) =>
        type is { TypeKind: TypeKind.Class } && Is(type) && !type.Is(WellKnownTypeNames.AggregateRoot);
}
