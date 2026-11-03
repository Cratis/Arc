// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Tenancy;

namespace Cratis.Arc.Commands.for_CommandPipeline.when_executing;

public class and_the_handler_denies_tenant_membership : given.a_command_pipeline_and_a_handler_for_command
{
    CommandResult _result;
    void Establish() => _commandHandler.Handle(Arg.Any<CommandContext>()).Returns(_ => throw new TargetInvocationException(new TenantMembershipDenied()));
    async Task Because() => _result = await _commandPipeline.Execute(_command);
    [Fact] void should_report_unauthorized() => _result.IsAuthorized.ShouldBeFalse();
    [Fact] void should_not_report_exception_details() => _result.HasExceptions.ShouldBeFalse();
}
