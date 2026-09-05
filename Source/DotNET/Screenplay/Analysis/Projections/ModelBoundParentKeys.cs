// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Analysis.Projections;

/// <summary>
/// Resolves the expression a child collection finds its parent by.
/// </summary>
/// <remarks>
/// A declaration that names no parent key does not mean the parent is the event source - the runtime looks for a
/// property of the event carrying the same kind of value the parent is identified by, and uses that. A document
/// saying nothing would therefore describe a hierarchy that is assembled differently from the one that really runs,
/// so the same search is made here rather than the default being assumed.
/// </remarks>
public static class ModelBoundParentKeys
{
    /// <summary>
    /// Gets the expression a child finds its parent by.
    /// </summary>
    /// <param name="attribute">The attribute declaring the child.</param>
    /// <param name="readModel">The read model holding the child.</param>
    /// <returns>The expression, or <see langword="null"/> when the event source identifies the parent.</returns>
    public static string? Of(AttributeData attribute, ITypeSymbol readModel) =>
        ModelBoundAttributes.Path(attribute, "ParentKey", 2) ??
        Discover(
            ModelBoundAttributes.EventTypeSymbolOf(attribute),
            readModel,
            ModelBoundAttributes.Argument(attribute, "Key", 0));

    /// <summary>
    /// Searches an event for the property carrying the identifier of the parent.
    /// </summary>
    /// <param name="eventType">The event type bringing an instance of the child into being.</param>
    /// <param name="readModel">The read model holding the child.</param>
    /// <param name="key">The property the child itself is keyed on, which is never also the parent.</param>
    /// <returns>The expression, or <see langword="null"/> when nothing matches.</returns>
    /// <remarks>
    /// More than one property may carry the same kind of value, in which case the first one declared is used - the
    /// declaration really is ambiguous, and naming the parent key explicitly is what resolves it.
    /// </remarks>
    static string? Discover(ITypeSymbol? eventType, ITypeSymbol readModel, string? key)
    {
        if (eventType is null || IdentifierTypeOf(readModel) is not { } identifier)
        {
            return null;
        }

        var match = eventType.DeclaredProperties()
            .FirstOrDefault(_ =>
                SymbolEqualityComparer.Default.Equals(_.Type, identifier) &&
                !string.Equals(_.Name, key, StringComparison.OrdinalIgnoreCase));

        return match is null ? null : ProjectionPaths.Convert(match.Name);
    }

    /// <summary>
    /// Gets the kind of value a read model is identified by.
    /// </summary>
    /// <param name="readModel">The read model to read.</param>
    /// <returns>The type, or <see langword="null"/> when the read model carries no identifier.</returns>
    static ITypeSymbol? IdentifierTypeOf(ITypeSymbol readModel) =>
        readModel.DeclaredProperties()
            .FirstOrDefault(_ => string.Equals(_.Name, ModelBoundChildren.ConventionalKey, StringComparison.OrdinalIgnoreCase))
            ?.Type;
}
