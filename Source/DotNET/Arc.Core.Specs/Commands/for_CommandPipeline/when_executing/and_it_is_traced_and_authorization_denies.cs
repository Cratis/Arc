// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Cratis.Arc.Commands.for_CommandPipeline.when_executing;

public class and_it_is_traced_and_authorization_denies : given.a_traced_command_pipeline
{
    void Establish() => _commandFilters.OnExecution(Arg.Any<CommandContext>()).Returns(CommandResult.Unauthorized(_correlationId));

    async Task Because() => _result = await _commandPipeline.Execute(_command, _serviceProvider);

    [Fact] void should_not_be_authorized() => _result.IsAuthorized.ShouldBeFalse();
    [Fact] void should_leave_the_status_unset() => CommandSpan.Status.ShouldEqual(ActivityStatusCode.Unset);
    [Fact] void should_add_the_outcome() => CommandSpan.GetTagItem(WellKnownTelemetryNames.CommandOutcome).ShouldEqual(WellKnownOperationOutcomes.Authorization);
    [Fact] void should_add_an_authorization_denied_event() => CommandSpan.Events.Count(_ => _.Name == WellKnownTelemetryNames.AuthorizationDeniedEvent).ShouldEqual(1);
    [Fact] void should_count_an_authorization_outcome() => Outcomes.Single().Tags[WellKnownTelemetryNames.CommandOutcome].ShouldEqual(WellKnownOperationOutcomes.Authorization);
}
