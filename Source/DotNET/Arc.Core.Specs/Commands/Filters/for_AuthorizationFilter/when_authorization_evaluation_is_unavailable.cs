// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Commands.Filters.for_AuthorizationFilter;

public class when_authorization_evaluation_is_unavailable : Specification
{
    Exception _error;
    bool _legacyInvoked;

    async Task Because()
    {
        var allow = Substitute.For<IAuthorizationEvaluator>();
        allow.IsAuthorized(typeof(ProtectedCommand)).Returns(_ =>
        {
            _legacyInvoked = true;
            return true;
        });
        await using var services = new ServiceCollection()
            .AddSingleton(new AuthorizationDeclarations(
                new KnownInstancesOf<IAnonymousEvaluator>([]),
                new KnownInstancesOf<IAuthorizationAttributeEvaluator>([])))
            .BuildServiceProvider();
        var context = new CommandContext(CorrelationId.New(), typeof(ProtectedCommand), new ProtectedCommand(), [], new CommandContextValues(), null, null, services);
        _error = await Catch.Exception(() => new AuthorizationFilter(allow).OnExecution(context));
    }

    [Fact] void should_fail_with_an_authorization_configuration_error() => _error.ShouldBeOfExactType<InvalidAuthorizationConfiguration>();
    [Fact] void should_not_consult_the_legacy_evaluator() => _legacyInvoked.ShouldBeFalse();

    [Authorize(Policy = "Protected")]
    public record ProtectedCommand;
}
