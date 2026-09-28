// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Commands.Filters.for_AuthorizationFilter;

public class when_legacy_evaluation_requires_asynchronous_authorization_with_services : given.an_authorization_filter
{
    CommandResult _result;

    async Task Because()
    {
        _authorizationHelper.IsAuthorized(typeof(object)).Returns(_ => throw new AsynchronousAuthorizationRequired());
        await using var services = new ServiceCollection()
            .AddSingleton(new AuthorizationDeclarations(
                new KnownInstancesOf<IAnonymousEvaluator>([]),
                new KnownInstancesOf<IAuthorizationAttributeEvaluator>([])))
            .AddSingleton(provider => new AuthorizationEvaluation(
                provider.GetRequiredService<AuthorizationDeclarations>(),
                _authorizationHelper,
                Substitute.For<ICurrentPrincipalAccessor>(),
                new ArcAuthorizationPolicyRuntime([])))
            .BuildServiceProvider();
        _context = new CommandContext(_correlationId, typeof(object), new object(), [], new CommandContextValues(), null, null, services);
        _result = await _filter.OnExecution(_context);
    }

    [Fact] void should_report_unauthorized() => _result.IsAuthorized.ShouldBeFalse();
    [Fact] void should_not_report_an_error() => _result.HasExceptions.ShouldBeFalse();
}
