// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Authorization;
using Cratis.Arc.Queries.ModelBound;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries.Filters.for_AuthorizationFilter;

public class when_an_old_model_bound_performer_denies : Specification
{
    QueryResult _result;

    async Task Because()
    {
        var anonymous = Substitute.For<IInstancesOf<IAnonymousEvaluator>>();
        anonymous.GetEnumerator().Returns(_ => new IAnonymousEvaluator[] { new AnonymousEvaluator() }.AsEnumerable().GetEnumerator());
        var attributes = Substitute.For<IInstancesOf<IAuthorizationAttributeEvaluator>>();
        attributes.GetEnumerator().Returns(_ => new IAuthorizationAttributeEvaluator[] { new AuthorizationAttributeEvaluator() }.AsEnumerable().GetEnumerator());
        var accessor = Substitute.For<ICurrentPrincipalAccessor>();
        accessor.Current.Returns(new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "caller")], "test")));
        var declarations = new AuthorizationDeclarations(anonymous, attributes);
        var defaultEvaluator = new AuthorizationEvaluator(accessor, anonymous, attributes);
        var evaluation = new AuthorizationEvaluation(
            declarations,
            defaultEvaluator,
            accessor,
            new ArcAuthorizationPolicyRuntime([new AuthorizationPolicyRegistration("Allowed", typeof(AllowingPolicy))]));
        await using var services = new ServiceCollection()
            .AddSingleton(declarations)
            .AddSingleton(evaluation)
            .AddSingleton<AllowingPolicy>()
            .BuildServiceProvider();

        var deny = Substitute.For<IAuthorizationEvaluator>();
        deny.IsAuthorized(Arg.Any<System.Reflection.MethodInfo>()).Returns(false);
        var isService = Substitute.For<IServiceProviderIsService>();
        var method = typeof(ProtectedReadModel).GetMethod(nameof(ProtectedReadModel.All))!;
        var performer = new ModelBoundQueryPerformer(typeof(ProtectedReadModel), typeof(ProtectedReadModel).FullName!, method, isService, deny);
        var performers = Substitute.For<IQueryPerformerProviders>();
        var name = performer.FullyQualifiedName;
        performers.TryGetPerformersFor(name, out var _).Returns(call =>
        {
            call[1] = performer;
            return true;
        });
        var context = new QueryContext(name, CorrelationId.New(), Paging.NotPaged, Sorting.None, ServiceProvider: services);
        _result = await new AuthorizationFilter(performers).OnPerform(context);
    }

    [Fact] void should_preserve_the_performer_constructors_custom_denial() => _result.IsAuthorized.ShouldBeFalse();

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
