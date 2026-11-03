// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Tenancy;

namespace Cratis.Arc.Queries.for_QueryPipeline.when_performing;

public class and_the_performer_denies_tenant_membership : given.a_query_pipeline
{
    QueryResult _result;
    void Establish()
    {
        _queryPerformerProviders.TryGetPerformersFor(Arg.Any<FullyQualifiedQueryName>(), out Arg.Any<IQueryPerformer>())
            .Returns(call =>
            {
                call[1] = _queryPerformer;
                return true;
            });
        query_filters.OnPerform(Arg.Any<QueryContext>()).Returns(QueryResult.Success(_correlationId));
        _queryPerformer.Perform(Arg.Any<QueryContext>()).Returns(_ => throw new TargetInvocationException(new TenantMembershipDenied()));
    }
    async Task Because() => _result = await _pipeline.Perform("test", new QueryArguments(), Paging.NotPaged, Sorting.None, _serviceProvider);
    [Fact] void should_report_unauthorized() => _result.IsAuthorized.ShouldBeFalse();
    [Fact] void should_not_report_exception_details() => _result.HasExceptions.ShouldBeFalse();
}
