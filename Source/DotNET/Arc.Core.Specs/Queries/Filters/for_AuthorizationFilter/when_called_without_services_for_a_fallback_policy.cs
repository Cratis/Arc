// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Execution;

namespace Cratis.Arc.Queries.Filters.for_AuthorizationFilter;

public class when_called_without_services_for_a_fallback_policy : Specification
{
    QueryResult _result;

    async Task Because()
    {
        var performer = Substitute.For<IQueryPerformer>();
        performer.Type.Returns(typeof(UnannotatedQuery));
        performer.IsAuthorized(Arg.Any<QueryContext>()).Returns(_ => throw new AsynchronousAuthorizationRequired());
        var providers = Substitute.For<IQueryPerformerProviders>();
        var name = new FullyQualifiedQueryName("UnannotatedQuery.All");
        providers.TryGetPerformersFor(name, out var _).Returns(call =>
        {
            call[1] = performer;
            return true;
        });
        _result = await new AuthorizationFilter(providers).OnPerform(new QueryContext(name, CorrelationId.New(), Paging.NotPaged, Sorting.None));
    }

    [Fact] void should_fail_closed_when_the_fallback_requires_async_authorization() => _result.IsAuthorized.ShouldBeFalse();
    [Fact] void should_return_an_authorization_result_without_exceptions() => _result.ExceptionMessages.ShouldBeEmpty();

    public class UnannotatedQuery;
}
