// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Analysis;

/// <summary>
/// Reads the tags a declaration classifies itself by.
/// </summary>
/// <remarks>
/// Chronicle tags observers, read models and event types alike, and only an event has somewhere in a Screenplay to
/// carry them. Reading them in one place is what lets the rest be reported rather than passed over.
/// </remarks>
public static class Tags
{
    /// <summary>
    /// Gets the tags a declaration carries.
    /// </summary>
    /// <param name="symbol">The declaration to read.</param>
    /// <returns>The tags, ordered, without duplicates.</returns>
    public static string[] Of(ISymbol symbol) =>
    [
        .. symbol.GetAttributes()
            .Where(_ => _.AttributeClass.Is(WellKnownTypeNames.TagAttribute) || _.AttributeClass.Is(WellKnownTypeNames.TagsAttribute))
            .SelectMany(_ => _.ConstructorArguments)
            .SelectMany(_ => _.Kind == TypedConstantKind.Array ? _.Values.Select(value => value.Value) : [_.Value])
            .OfType<string>()
            .Where(_ => _.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
    ];
}
