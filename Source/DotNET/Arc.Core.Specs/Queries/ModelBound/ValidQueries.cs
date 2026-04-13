// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1649 // File name should match first type name

namespace Cratis.Arc.Queries.ModelBound;

/// <summary>
/// Public read model with a valid public static query method, for testing.
/// </summary>
[ReadModel]
public class PublicReadModelWithValidQuery
{
    /// <summary>Gets or sets the ID.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the value.</summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>Returns a single instance by ID.</summary>
    /// <param name="id">The ID to look up.</param>
    /// <returns>A matching read model.</returns>
    public static PublicReadModelWithValidQuery GetById(Guid id) =>
        new() { Id = id, Value = "Valid result" };
}
