// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries.Filters.for_AuthorizationFilter;

public class when_legacy_evaluation_requires_asynchronous_authorization_with_services : given.an_authorization_filter
{
    QueryResult _result;

    async Task Because()
    {
        var name = (FullyQualifiedQueryName)"TestQuery.Load";
        _queryPerformer.Type.Returns(typeof(object));
        _queryPerformer.Name.Returns((QueryName)"Load");
        _queryPerformerProviders.TryGetPerformersFor(name, out var _).Returns(call =>
        {
            call[1] = _queryPerformer;
            return true;
        });
        _queryPerformer.IsAuthorized(Arg.Any<QueryContext>()).Returns(_ => throw new AsynchronousAuthorizationRequired());
        await using var services = new ServiceCollection()
            .AddSingleton(new AuthorizationDeclarations(
                new KnownInstancesOf<IAnonymousEvaluator>([]),
                new KnownInstancesOf<IAuthorizationAttributeEvaluator>([])))
            .AddSingleton(provider => new AuthorizationEvaluation(
                provider.GetRequiredService<AuthorizationDeclarations>(),
                _authorizationEvaluator,
                Substitute.For<ICurrentPrincipalAccessor>(),
                new ArcAuthorizationPolicyRuntime([])))
            .BuildServiceProvider();
        _context = new QueryContext(name, _correlationId, Paging.NotPaged, Sorting.None, null, [], ServiceProvider: services);
        _result = await _filter.OnPerform(_context);
    }

    [Fact] void should_report_unauthorized() => _result.IsAuthorized.ShouldBeFalse();
    [Fact] void should_not_report_an_error() => _result.HasExceptions.ShouldBeFalse();
}
