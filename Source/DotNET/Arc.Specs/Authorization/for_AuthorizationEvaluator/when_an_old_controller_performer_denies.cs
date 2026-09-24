// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Security.Claims;
using Cratis.Arc.Queries;
using Cratis.Arc.Queries.ControllerBased;
using Cratis.Arc.Queries.Filters;
using Cratis.Execution;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Authorization.for_AuthorizationEvaluator;

public class when_an_old_controller_performer_denies : Specification
{
    QueryResult _result;

    async Task Because()
    {
        var anonymous = Substitute.For<IInstancesOf<IAnonymousEvaluator>>();
        anonymous.GetEnumerator().Returns(_ => new IAnonymousEvaluator[] { new AnonymousEvaluator(), new AspNetAnonymousEvaluator() }.AsEnumerable().GetEnumerator());
        var attributes = Substitute.For<IInstancesOf<IAuthorizationAttributeEvaluator>>();
        attributes.GetEnumerator().Returns(_ => new IAuthorizationAttributeEvaluator[] { new AuthorizationAttributeEvaluator(), new AspNetAuthorizationAttributeEvaluator() }.AsEnumerable().GetEnumerator());
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
        deny.IsAuthorized(Arg.Any<MethodInfo>()).Returns(false);
        var descriptor = new ControllerActionDescriptor
        {
            ActionName = nameof(ProtectedController.All),
            ControllerName = nameof(ProtectedController),
            ControllerTypeInfo = typeof(ProtectedController).GetTypeInfo(),
            MethodInfo = typeof(ProtectedController).GetMethod(nameof(ProtectedController.All))!
        };
        var performer = new ControllerQueryPerformer(descriptor, Substitute.For<IServiceProviderIsService>(), deny);
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

    [Fact] void should_preserve_the_old_controller_performers_denial() => _result.IsAuthorized.ShouldBeFalse();

    public class ProtectedController
    {
        [Microsoft.AspNetCore.Authorization.Authorize(Policy = "Allowed")]
        public void All()
        {
        }
    }

    public class AllowingPolicy : IAuthorizationPolicy
    {
        public ValueTask<bool> IsAuthorized(AuthorizationPolicyContext context, CancellationToken cancellationToken) => ValueTask.FromResult(true);
    }
}
