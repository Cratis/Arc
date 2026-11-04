// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Authorization;

namespace Cratis.Arc.Commands.for_CommandPipeline.when_validating;

public class and_a_validator_reads_the_caller : given.a_command_pipeline_and_a_handler_for_command
{
    bool _success;
    CommandCaller _caller;

    void Establish()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "represented")], "test"));
        var accessor = Substitute.For<ICurrentPrincipalAccessor>();
        accessor.Current.Returns(principal);
        _serviceProvider.GetService(typeof(ICurrentPrincipalAccessor)).Returns(accessor);
        _commandFilters.OnExecution(Arg.Any<CommandContext>()).Returns(call =>
        {
            _success = AuthorizationCommandIdentity.TryGetCaller(principal, out _caller, out _);
            return CommandResult.Success(_correlationId);
        });
    }

    async Task Because() => await _commandPipeline.Validate(_command, _serviceProvider);

    [Fact] void should_have_a_caller_frame() => _success.ShouldBeTrue();
    [Fact] void should_read_the_captured_caller() => _caller.Single("sub").Key.ShouldEqual("represented");
}
