// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Authorization;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries.Filters.for_AuthorizationFilter;

public class when_a_custom_target_performer_denies : Specification
{
    QueryResult _result;
    IQueryPerformer _performer;
    QueryContext _context;
    AuthorizationFilter _filter;

    void Establish()
    {
        var anonymous = Substitute.For<IInstancesOf<IAnonymousEvaluator>>();
        anonymous.GetEnumerator().Returns(_ => new IAnonymousEvaluator[] { new AnonymousEvaluator() }.AsEnumerable().GetEnumerator());
        var attributes = Substitute.For<IInstancesOf<IAuthorizationAttributeEvaluator>>();
        attributes.GetEnumerator().Returns(_ => new IAuthorizationAttributeEvaluator[] { new AuthorizationAttributeEvaluator() }.AsEnumerable().GetEnumerator());
        var declarations = new AuthorizationDeclarations(anonymous, attributes);
        var accessor = Substitute.For<ICurrentPrincipalAccessor>();
        accessor.Current.Returns(new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "caller")], "test")));
        var evaluator = Substitute.For<IAuthorizationEvaluator>();
        evaluator.IsAuthorized(typeof(ProtectedReadModel).GetMethod(nameof(ProtectedReadModel.All))!).Returns(true);
        var evaluation = new AuthorizationEvaluation(
            declarations,
            evaluator,
            accessor,
            new ArcAuthorizationPolicyRuntime([new AuthorizationPolicyRegistration("Allowed", typeof(AllowingPolicy))]));
        var services = new ServiceCollection()
            .AddSingleton(declarations)
            .AddSingleton(evaluation)
            .AddSingleton<AllowingPolicy>()
            .BuildServiceProvider();

        _performer = Substitute.For<IQueryPerformer, IAuthorizationQueryTarget>();
        _performer.Type.Returns(typeof(ProtectedReadModel));
        ((IAuthorizationQueryTarget)_performer).AuthorizationMethod.Returns(typeof(ProtectedReadModel).GetMethod(nameof(ProtectedReadModel.All))!);
        _performer.IsAuthorized(Arg.Any<QueryContext>()).Returns(false);
        var name = new FullyQualifiedQueryName("ProtectedReadModel.All");
        _context = new QueryContext(name, CorrelationId.New(), Paging.NotPaged, Sorting.None, ServiceProvider: services);
        var performers = Substitute.For<IQueryPerformerProviders>();
        performers.TryGetPerformersFor(name, out var _).Returns(call =>
        {
            call[1] = _performer;
            return true;
        });
        _filter = new AuthorizationFilter(performers);
    }

    async Task Because() => _result = await _filter.OnPerform(_context);

    [Fact] void should_preserve_the_custom_performers_denial() => _result.IsAuthorized.ShouldBeFalse();

    public static class ProtectedReadModel
    {
        [Authorize(Policy = "Allowed")]
        public static void All()
        {
        }
    }

    public class AllowingPolicy : IAuthorizationPolicy
    {
        public ValueTask<bool> IsAuthorized(AuthorizationPolicyContext context, CancellationToken cancellationToken) => ValueTask.FromResult(true);
    }
}
