// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Authorization;
using Cratis.Arc.Queries.ModelBound;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries.Filters.for_AuthorizationFilter;

public class when_an_old_model_bound_performer_shares_the_di_evaluator : Specification
{
    QueryResult _result;
    IAuthorizationEvaluator _evaluator;
    System.Reflection.MethodInfo _method;

    async Task Because()
    {
        var anonymous = Substitute.For<IInstancesOf<IAnonymousEvaluator>>();
        anonymous.GetEnumerator().Returns(_ => new IAnonymousEvaluator[] { new AnonymousEvaluator() }.AsEnumerable().GetEnumerator());
        var attributes = Substitute.For<IInstancesOf<IAuthorizationAttributeEvaluator>>();
        attributes.GetEnumerator().Returns(_ => new IAuthorizationAttributeEvaluator[] { new AuthorizationAttributeEvaluator() }.AsEnumerable().GetEnumerator());
        var accessor = Substitute.For<ICurrentPrincipalAccessor>();
        accessor.Current.Returns(new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "caller")], "test")));
        var declarations = new AuthorizationDeclarations(anonymous, attributes);
        _evaluator = Substitute.For<IAuthorizationEvaluator>();
        _evaluator.IsAuthorized(Arg.Any<System.Reflection.MethodInfo>()).Returns(true);
        var evaluation = new AuthorizationEvaluation(declarations, _evaluator, accessor, new ArcAuthorizationPolicyRuntime([]));
        await using var services = new ServiceCollection()
            .AddSingleton(declarations)
            .AddSingleton(evaluation)
            .BuildServiceProvider();
        _method = typeof(ProtectedReadModel).GetMethod(nameof(ProtectedReadModel.All))!;
        var performer = new ModelBoundQueryPerformer(
            typeof(ProtectedReadModel),
            typeof(ProtectedReadModel).FullName!,
            _method,
            Substitute.For<IServiceProviderIsService>(),
            _evaluator);
        var performers = Substitute.For<IQueryPerformerProviders>();
        var name = performer.FullyQualifiedName;
        performers.TryGetPerformersFor(name, out var _).Returns(call =>
        {
            call[1] = performer;
            return true;
        });
        _result = await new AuthorizationFilter(performers).OnPerform(
            new QueryContext(name, CorrelationId.New(), Paging.NotPaged, Sorting.None, ServiceProvider: services));
    }

    [Fact] void should_allow_the_query() => _result.IsAuthorized.ShouldBeTrue();
    [Fact] void should_only_invoke_the_shared_evaluator_once() => _evaluator.Received(1).IsAuthorized(_method);

    public static class ProtectedReadModel
    {
        [Authorize]
        public static void All()
        {
        }
    }
}
