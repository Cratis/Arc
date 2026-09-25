// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Authorization;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Commands.Filters.for_AuthorizationFilter;

public class when_custom_filter_evaluator_denies_with_default_di : Specification
{
    CommandResult _result;

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
        var evaluation = new AuthorizationEvaluation(declarations, defaultEvaluator, accessor, new ArcAuthorizationPolicyRuntime([]));
        await using var services = new ServiceCollection()
            .AddSingleton(declarations)
            .AddSingleton(evaluation)
            .BuildServiceProvider();
        var deny = Substitute.For<IAuthorizationEvaluator>();
        deny.IsAuthorized(typeof(ProtectedCommand)).Returns(false);
        var context = new CommandContext(CorrelationId.New(), typeof(ProtectedCommand), new ProtectedCommand(), [], new CommandContextValues(), ServiceProvider: services);
        _result = await new AuthorizationFilter(deny).OnExecution(context);
    }

    [Fact] void should_keep_the_filter_constructors_denial_authoritative() => _result.IsAuthorized.ShouldBeFalse();

    [Authorize]
    public record ProtectedCommand;
}
