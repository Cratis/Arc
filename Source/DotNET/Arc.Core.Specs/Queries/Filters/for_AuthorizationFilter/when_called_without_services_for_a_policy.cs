// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Execution;

namespace Cratis.Arc.Queries.Filters.for_AuthorizationFilter;

public class when_called_without_services_for_a_policy : Specification
{
    QueryResult _result;

    async Task Because()
    {
        var performer = Substitute.For<IQueryPerformer, IAuthorizationQueryTarget>();
        performer.Type.Returns(typeof(ProtectedReadModel));
        performer.IsAuthorized(Arg.Any<QueryContext>()).Returns(true);
        ((IAuthorizationQueryTarget)performer).AuthorizationMethod.Returns(typeof(ProtectedReadModel).GetMethod(nameof(ProtectedReadModel.All)));
        var providers = Substitute.For<IQueryPerformerProviders>();
        var name = new FullyQualifiedQueryName("ProtectedReadModel.All");
        providers.TryGetPerformersFor(name, out var _).Returns(call =>
        {
            call[1] = performer;
            return true;
        });
        _result = await new AuthorizationFilter(providers).OnPerform(new QueryContext(name, CorrelationId.New(), Paging.NotPaged, Sorting.None));
    }

    [Fact] void should_not_accept_the_performers_legacy_allow_without_a_policy_runtime() => _result.IsAuthorized.ShouldBeFalse();

    public static class ProtectedReadModel
    {
        [Authorize(Policy = "Protected")]
        public static void All()
        {
        }
    }
}
