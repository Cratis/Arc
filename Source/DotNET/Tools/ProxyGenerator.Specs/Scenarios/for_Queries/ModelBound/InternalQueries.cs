// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries.ModelBound;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_Queries.ModelBound;

/// <summary>
/// An internal read model for testing internal type support.
/// </summary>
[ReadModel]
internal class InternalReadModel
{
    /// <summary>
    /// Gets or sets the identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the data.
    /// </summary>
    public string Data { get; set; } = string.Empty;

    /// <summary>
    /// Internal query method.
    /// </summary>
    /// <param name="id">The identifier to query.</param>
    /// <returns>The read model result.</returns>
    internal static InternalReadModel GetById(Guid id)
    {
        return new InternalReadModel { Id = id, Data = "Internal data" };
    }
}

/// <summary>
/// A public read model with internal query method for testing.
/// </summary>
[ReadModel]
public class ReadModelWithInternalQuery
{
    /// <summary>
    /// Gets or sets the identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the value.
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Internal query method.
    /// </summary>
    /// <param name="id">The identifier to query.</param>
    /// <returns>The read model result.</returns>
    internal static ReadModelWithInternalQuery Query(Guid id)
    {
        return new ReadModelWithInternalQuery { Id = id, Value = "Internal query result" };
    }
}
