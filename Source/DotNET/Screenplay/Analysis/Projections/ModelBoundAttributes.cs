// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Analysis.Projections;

/// <summary>
/// Reads the arguments the attributes of a model-bound projection carry.
/// </summary>
/// <remarks>
/// Every one of these attributes names the event it binds to through a type argument and everything else through
/// optional arguments, so the same two questions are asked of all of them.
/// </remarks>
public static class ModelBoundAttributes
{
    /// <summary>
    /// Gets the event type an attribute is bound to.
    /// </summary>
    /// <param name="attribute">The attribute to read.</param>
    /// <returns>The type, or <see langword="null"/> when the attribute names none.</returns>
    public static ITypeSymbol? EventTypeSymbolOf(AttributeData attribute) =>
        attribute.AttributeClass?.TypeArguments.FirstOrDefault();

    /// <summary>
    /// Gets the name of the event type an attribute is bound to.
    /// </summary>
    /// <param name="attribute">The attribute to read.</param>
    /// <returns>The name, or <see langword="null"/> when the attribute names none.</returns>
    public static string? EventTypeOf(AttributeData attribute) => EventTypeSymbolOf(attribute)?.Name;

    /// <summary>
    /// Gets an argument of an attribute, in either the named or the positional form.
    /// </summary>
    /// <param name="attribute">The attribute to read.</param>
    /// <param name="name">The name of the argument.</param>
    /// <param name="index">The position of the argument.</param>
    /// <returns>The value, or <see langword="null"/> when the argument carries nothing.</returns>
    public static string? Argument(AttributeData attribute, string name, int index) =>
        (attribute.GetNamedArgument(name) ?? attribute.GetArgument(index)) as string is { Length: > 0 } value ? value : null;

    /// <summary>
    /// Gets an argument naming a property path, in the casing a projection body references it by.
    /// </summary>
    /// <param name="attribute">The attribute to read.</param>
    /// <param name="name">The name of the argument.</param>
    /// <param name="index">The position of the argument.</param>
    /// <returns>The path, or <see langword="null"/> when the argument carries nothing.</returns>
    /// <remarks>
    /// A declaration names properties the way C# declares them, while a projection body references them the way they
    /// are serialized, so the casing is converted here rather than left for the printer, which writes an expression
    /// verbatim. That is also what lets the same declaration made fluently and made with attributes agree.
    /// </remarks>
    public static string? Path(AttributeData attribute, string name, int index) =>
        ProjectionPaths.Convert(Argument(attribute, name, index));
}
