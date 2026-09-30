// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Cratis.Arc.Commands.for_CommandPipeline.when_executing;

public class and_it_is_traced_and_authorization_denies : given.a_traced_command_pipeline
{
    void Establish() => _commandFilters.OnExecution(Arg.Any<CommandContext>()).Returns(CommandResult.Unauthorized(_correlationId));

    async Task Because() => _result = await _commandPipeline.Execute(_command, _serviceProvider);

    [Fact] void should_not_be_authorized() => _result.IsAuthorized.ShouldBeFalse();
    [Fact] void should_set_the_status_to_error() => CommandSpan.Status.ShouldEqual(ActivityStatusCode.Error);
    [Fact] void should_describe_the_status_as_authorization() => CommandSpan.StatusDescription.ShouldEqual("authorization");
    [Fact] void should_add_an_authorization_denied_event() => CommandSpan.Events.Count(_ => _.Name == "cratis.arc.authorization.denied").ShouldEqual(1);
    [Fact] void should_count_an_authorization_outcome() => Outcomes.Single().Tags["cratis.arc.command.outcome"].ShouldEqual("authorization");
}
