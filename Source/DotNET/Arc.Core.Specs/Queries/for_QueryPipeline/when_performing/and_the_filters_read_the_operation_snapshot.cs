// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Authorization;
using Cratis.Arc.Tenancy;

namespace Cratis.Arc.Queries.for_QueryPipeline.when_performing;

public class and_the_filters_read_the_operation_snapshot : given.a_query_pipeline
{
    TenantId _tenant;
    string _subject;
    IDisposable _boundary;

    void Establish()
    {
        _boundary = TenantIdAccessor.Independent.BeginRequest();
        var resolver = Substitute.For<ITenantIdResolver>();
        resolver.Resolve().Returns("tenant-1");
        var accessor = new TenantIdAccessor(resolver);
        var principals = Substitute.For<ICurrentPrincipalAccessor>();
        principals.Current.Returns(new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "user-1")], "test")));
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
        query_filters.OnPerform(Arg.Any<QueryContext>()).Returns(_ =>
        {
            _tenant = OperationSnapshot.Current!.Tenant;
            _subject = OperationSnapshot.Current.CausedBy.Subject.Value;
            return QueryResult.Success(_correlationId);
        });
    }

    void Destroy() => _boundary.Dispose();

    async Task Because() => await _pipeline.Perform("test", new QueryArguments(), Paging.NotPaged, Sorting.None, _serviceProvider);

    [Fact] void should_give_the_filters_the_tenant() => _tenant.ShouldEqual(new TenantId("tenant-1"));
    [Fact] void should_give_the_filters_the_caller() => _subject.ShouldEqual("user-1");
    [Fact] void should_leave_no_snapshot_behind() => OperationSnapshot.Current.ShouldBeNull();
}
