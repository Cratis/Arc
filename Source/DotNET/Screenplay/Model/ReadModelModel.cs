// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Model;

/// <summary>
/// Represents the shape of a read model - what it holds, standing on its own.
/// </summary>
/// <param name="Name">The name of the read model.</param>
/// <param name="Properties">The values the read model holds, in the order the source declares them.</param>
/// <remarks>
/// A read model says what it is and nothing else. What builds it names it - a projection or a reducer - and what
/// reads it names it - a query - so neither is carried here. Neither is an identity: the executable model takes the
/// identity of an instance from the read model's keyed query, never from the declaration.
/// </remarks>
public record ReadModelModel(string Name, IEnumerable<PropertyModel> Properties)
{
    /// <summary>
    /// Gets the namespace the read model is declared in.
    /// </summary>
    public string Namespace { get; init; } = string.Empty;

    /// <summary>
    /// Gets the full name of the read model type, when it was read from code.
    /// </summary>
    public string? FullName { get; init; }

    /// <summary>
    /// Gets the summary describing the read model.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets the repository relative path of the file declaring the read model, when one can be written portably.
    /// </summary>
    public string? File { get; init; }
}
