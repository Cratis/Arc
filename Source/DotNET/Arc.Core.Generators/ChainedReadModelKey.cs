// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Generators;

/// <summary>
/// Describes a relationship property referenced by one command's members.
/// </summary>
/// <param name="command">The command whose binding references the relationship.</param>
/// <param name="model">The source model as declared by the sibling parameter.</param>
/// <param name="property">The readable relationship property.</param>
internal sealed class ChainedReadModelKey(INamedTypeSymbol command, INamedTypeSymbol model, IPropertySymbol property)
{
    /// <summary>
    /// Gets the command whose binding references the relationship.
    /// </summary>
    public INamedTypeSymbol Command { get; } = command;

    /// <summary>
    /// Gets the declared source model.
    /// </summary>
    public INamedTypeSymbol Model { get; } = model;

    /// <summary>
    /// Gets the readable relationship property.
    /// </summary>
    public IPropertySymbol Property { get; } = property;
}
