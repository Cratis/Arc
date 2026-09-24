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

public class when_an_old_controller_performer_has_a_differently_configured_default_evaluator : Specification
{
    QueryResult _result;

    async Task Because()
    {
        var anonymous = Substitute.For<IInstancesOf<IAnonymousEvaluator>>();
        anonymous.GetEnumerator().Returns(_ => new IAnonymousEvaluator[] { new AnonymousEvaluator(), new AspNetAnonymousEvaluator() }.AsEnumerable().GetEnumerator());
        var attributes = Substitute.For<IInstancesOf<IAuthorizationAttributeEvaluator>>();
        attributes.GetEnumerator().Returns(_ => new IAuthorizationAttributeEvaluator[] { new AuthorizationAttributeEvaluator(), new AspNetAuthorizationAttributeEvaluator() }.AsEnumerable().GetEnumerator());
        var authenticatedAccessor = Substitute.For<ICurrentPrincipalAccessor>();
        authenticatedAccessor.Current.Returns(new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "caller")], "test")));
        var anonymousAccessor = Substitute.For<ICurrentPrincipalAccessor>();
        anonymousAccessor.Current.Returns(new ClaimsPrincipal(new ClaimsIdentity()));
        var declarations = new AuthorizationDeclarations(anonymous, attributes);
        var evaluation = new AuthorizationEvaluation(
            declarations,
            new AuthorizationEvaluator(authenticatedAccessor, anonymous, attributes),
            authenticatedAccessor,
            new ArcAuthorizationPolicyRuntime([]));
        await using var services = new ServiceCollection()
            .AddSingleton(declarations)
            .AddSingleton(evaluation)
            .BuildServiceProvider();
        var descriptor = new ControllerActionDescriptor
        {
            ActionName = nameof(ProtectedController.All),
            ControllerName = nameof(ProtectedController),
            ControllerTypeInfo = typeof(ProtectedController).GetTypeInfo(),
            MethodInfo = typeof(ProtectedController).GetMethod(nameof(ProtectedController.All))!
        };
        var performer = new ControllerQueryPerformer(
            descriptor,
            Substitute.For<IServiceProviderIsService>(),
            new AuthorizationEvaluator(anonymousAccessor, anonymous, attributes));
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

    [Fact] void should_preserve_the_captured_default_evaluators_denial() => _result.IsAuthorized.ShouldBeFalse();

    public class ProtectedController
    {
        [Microsoft.AspNetCore.Authorization.Authorize]
        public void All()
        {
        }
    }
}
