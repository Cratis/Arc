// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries;

/// <summary>
/// Represents a query request parsed from an incoming HTTP request, independent of the transport it arrived on.
/// </summary>
/// <param name="Arguments">The arguments for the query.</param>
/// <param name="Paging">The <see cref="Queries.Paging"/> for the query.</param>
/// <param name="Sorting">The <see cref="Queries.Sorting"/> for the query.</param>
public record QueryRequest(QueryArguments Arguments, Paging Paging, Sorting Sorting);
