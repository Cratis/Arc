// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries;

/// <summary>
/// Exception that gets thrown when a query is narrowed but its result cannot be narrowed.
/// </summary>
/// <param name="queryName">The name of the query.</param>
/// <param name="resultType">The type the query returned.</param>
public class UnsupportedQueryRowNarrowing(FullyQualifiedQueryName queryName, Type resultType)
    : Exception($"Query '{queryName}' is narrowed by a policy, but it returns '{resultType}', which cannot be narrowed before paging and counting. Return an IQueryable or a materialized collection so the rows are never returned unnarrowed.");
