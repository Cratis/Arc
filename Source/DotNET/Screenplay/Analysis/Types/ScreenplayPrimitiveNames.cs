// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission.Types;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Analysis.Types;

/// <summary>
/// Determines whether a symbol is one of the framework types a Screenplay primitive stands for.
/// </summary>
public static class ScreenplayPrimitiveNames
{
    /// <summary>
    /// Determines whether a type is a Screenplay primitive.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True when the type maps onto a primitive.</returns>
    public static bool IsPrimitive(INamedTypeSymbol type) =>
        ScreenplayPrimitiveTypes.TryResolve(type.FullMetadataName(), out _);
}
