// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Authorization;
using Cratis.Arc.Tenancy;

namespace Cratis.Arc.Queries.for_QueryPipeline.when_performing;

public class and_the_filters_find_the_caller_mutated : given.a_query_pipeline
{
    QueryResult _result;
    IDisposable _boundary;

    void Establish()
    {
        _boundary = TenantIdAccessor.Independent.BeginRequest();
        var resolver = Substitute.For<ITenantIdResolver>();
        resolver.Resolve().Returns("tenant-1");
        var accessor = new TenantIdAccessor(resolver);
        var user = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "user-1")], "test"));
        var principals = Substitute.For<ICurrentPrincipalAccessor>();
        principals.Current.Returns(user);
        _serviceProvider.GetService(typeof(ICurrentPrincipalAccessor)).Returns(principals);
        _serviceProvider.GetService(typeof(TenantIdAccessor)).Returns(accessor);
        _serviceProvider.GetService(typeof(ITenantIdAccessor)).Returns(accessor);
        _serviceProvider.GetService(typeof(ITenantIdResolver)).Returns(resolver);
        _queryPerformerProviders.TryGetPerformersFor(Arg.Any<FullyQualifiedQueryName>(), out Arg.Any<IQueryPerformer>())
            .Returns(call =>
            {
                call[1] = _queryPerformer;
                return true;
            });
        query_filters.OnPerform(Arg.Any<QueryContext>()).Returns(call =>
        {
            ((ClaimsIdentity)user.Identity!).AddClaim(new Claim(ClaimTypes.Role, "admin"));
            try
            {
                _ = OperationSnapshot.Current!.Tenant;
            }
            catch
            {
                // Swallowed on purpose; the pipeline must still reject the query.
            }

            return QueryResult.Success(_correlationId);
        });
    }

    void Destroy() => _boundary.Dispose();

    async Task Because() => _result = await _pipeline.Perform("test", new QueryArguments(), Paging.NotPaged, Sorting.None, _serviceProvider);

    [Fact] void should_be_unauthorized() => _result.IsAuthorized.ShouldBeFalse();
}
