// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;

namespace Cratis.Arc.Queries;

/// <summary>
/// Represents a collection of query parameters.
/// </summary>
public class QueryParameters : IEnumerable<QueryParameter>
{
    /// <summary>
    /// Gets an empty collection of query parameters.
    /// </summary>
    public static readonly QueryParameters Empty = new();

    readonly List<QueryParameter> _parameters = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="QueryParameters"/> class.
    /// </summary>
    public QueryParameters()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="QueryParameters"/> class.
    /// </summary>
    /// <param name="parameters">The collection of parameters to initialize with.</param>
    public QueryParameters(IEnumerable<QueryParameter> parameters)
    {
        _parameters.AddRange(parameters);
    }

    /// <summary>
    /// Gets the number of parameters in the collection.
    /// </summary>
    public int Count => _parameters.Count;

    /// <summary>
    /// Adds a parameter to the collection.
    /// </summary>
    /// <param name="parameter">The parameter to add.</param>
    public void Add(QueryParameter parameter) => _parameters.Add(parameter);

    /// <summary>
    /// Adds a parameter to the collection.
    /// </summary>
    /// <param name="name">The name of the parameter.</param>
    /// <param name="type">The type of the parameter.</param>
    public void Add(string name, Type type) => _parameters.Add(new QueryParameter(name, type));

    /// <inheritdoc/>
    public IEnumerator<QueryParameter> GetEnumerator() => _parameters.GetEnumerator();

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}