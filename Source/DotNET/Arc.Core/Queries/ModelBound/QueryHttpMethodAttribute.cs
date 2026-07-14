// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.ModelBound;

/// <summary>
/// Specifies the HTTP method the generated client proxy should use to perform a query.
/// </summary>
/// <remarks>
/// Apply to a read model to set the default for all of its queries, or to a specific static query
/// method to set it for that query; a method-level attribute overrides a read-model-level one. The
/// server always accepts both GET and QUERY regardless of this attribute — it only sets the generated
/// client proxy's default transport, which callers can still override at runtime via <c>setHttpMethod</c>.
/// </remarks>
/// <param name="method">The <see cref="QueryHttpMethod"/> the query should use.</param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class QueryHttpMethodAttribute(QueryHttpMethod method) : Attribute
{
    /// <summary>
    /// Gets the <see cref="QueryHttpMethod"/> the query should use.
    /// </summary>
    public QueryHttpMethod Method { get; } = method;
}
