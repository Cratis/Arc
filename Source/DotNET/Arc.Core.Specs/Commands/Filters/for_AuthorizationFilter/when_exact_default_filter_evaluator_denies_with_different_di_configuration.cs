// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Authorization;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Commands.Filters.for_AuthorizationFilter;

public class when_exact_default_filter_evaluator_denies_with_different_di_configuration : Specification
{
    CommandResult _result;

    async Task Because()
    {
        var anonymous = Substitute.For<IInstancesOf<IAnonymousEvaluator>>();
        anonymous.GetEnumerator().Returns(_ => new IAnonymousEvaluator[] { new AnonymousEvaluator() }.AsEnumerable().GetEnumerator());
        var attributes = Substitute.For<IInstancesOf<IAuthorizationAttributeEvaluator>>();
        attributes.GetEnumerator().Returns(_ => new IAuthorizationAttributeEvaluator[] { new AuthorizationAttributeEvaluator() }.AsEnumerable().GetEnumerator());
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
        var filter = new AuthorizationFilter(new AuthorizationEvaluator(anonymousAccessor, anonymous, attributes));
        var context = new CommandContext(CorrelationId.New(), typeof(ProtectedCommand), new ProtectedCommand(), [], new CommandContextValues(), ServiceProvider: services);
        _result = await filter.OnExecution(context);
    }

    [Fact] void should_preserve_the_filter_constructors_default_denial() => _result.IsAuthorized.ShouldBeFalse();

    [Authorize]
    public record ProtectedCommand;
}
