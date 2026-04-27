// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;
using Cratis.Arc.Queries;

namespace TestApps.Features.CrossCuttingAuthorization;

/// <summary>
/// Represents a query filter that applies cross-cutting authorization to a feature namespace.
/// </summary>
/// <param name="httpRequestContextAccessor">The <see cref="IHttpRequestContextAccessor"/> for resolving the current user principal.</param>
public class CrossCuttingAuthorizationQueryFilter(IHttpRequestContextAccessor httpRequestContextAccessor) : IQueryFilter
{
    const string ProtectedNamespace = "TestApps.Features.CrossCuttingAuthorization";
    const string RequiredRole = "CrossCuttingAuthorization";

    /// <inheritdoc/>
    public Task<QueryResult> OnPerform(QueryContext context)
    {
        if (!IsProtectedQuery(context.Name.Value))
        {
            return Task.FromResult(QueryResult.Success(context.CorrelationId));
        }

        if (httpRequestContextAccessor.Current?.User.IsInRole(RequiredRole) ?? false)
        {
            return Task.FromResult(QueryResult.Success(context.CorrelationId));
        }

        return Task.FromResult(QueryResult.Unauthorized(context.CorrelationId));
    }

    static bool IsProtectedQuery(string queryName) =>
        queryName.StartsWith(ProtectedNamespace, StringComparison.Ordinal);
}
