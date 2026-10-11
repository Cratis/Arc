// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Arc.Commands;
using Cratis.Arc.Http;
using Cratis.Execution;

namespace Cratis.Arc.Chronicle.Reactors.for_CommandSideEffectExecutor.when_executing;

public class and_an_inherited_system_command_fails : given.a_command_side_effect_executor
{
    CurrentPrincipalAccessor _principal;
    bool _authenticatedDuringCommand;
    string[] _rolesDuringCommand;
    bool _authenticatedAfterFailure;

    void Establish()
    {
        var requestAccessor = Substitute.For<IHttpRequestContextAccessor>();
        requestAccessor.Current.Returns((IHttpRequestContext)null!);
        _principal = new(requestAccessor);
        _executor = new(_serviceScopeFactory, new SystemExecution(_principal));
        _commandPipeline.Execute(Arg.Any<object>(), _serviceProvider).Returns(_ =>
        {
            _authenticatedDuringCommand = _principal.Current?.Identity?.IsAuthenticated == true;
            _rolesDuringCommand = _principal.Current!.FindAll(System.Security.Claims.ClaimTypes.Role).Select(claim => claim.Value).ToArray();
            return new CommandResult { CorrelationId = CorrelationId.New(), IsAuthorized = false };
        });
    }

    async Task Because()
    {
        await _executor.Execute([new TestCommand("failed")], typeof(InheritedSystemReactor));
        _authenticatedAfterFailure = _principal.Current?.Identity?.IsAuthenticated == true;
    }

    [Fact] void should_establish_an_authenticated_actor_from_the_inherited_attribute() => _authenticatedDuringCommand.ShouldBeTrue();
    [Fact] void should_keep_the_empty_role_set() => _rolesDuringCommand.ShouldBeEmpty();
    [Fact] void should_remove_the_system_actor_after_failure() => _authenticatedAfterFailure.ShouldBeFalse();

    [ExecuteCommandsAsSystem]
    public class ParentSystemReactor;

    public class InheritedSystemReactor : ParentSystemReactor;
}
