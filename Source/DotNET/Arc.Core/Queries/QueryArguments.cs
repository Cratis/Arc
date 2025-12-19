// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries;

/// <summary>
/// Represents arguments for a query, providing a strongly-typed wrapper around dictionary functionality.
/// </summary>
public class QueryArguments : Dictionary<string, object>
{
    /// <summary>
    /// Represents empty query arguments.
    /// </summary>
    public static readonly QueryArguments Empty = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="QueryArguments"/> class.
    /// </summary>
    public QueryArguments() : base(StringComparer.OrdinalIgnoreCase)
    {
    }
}