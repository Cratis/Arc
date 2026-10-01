// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;

namespace Cratis.Arc.Commands.for_CommandPipeline.when_executing;

/// <summary>
/// A hosted command can be turned away before the pipeline proper runs. It is still traced and measured.
/// </summary>
public class and_it_is_traced_and_hosted_authorization_cannot_be_resolved : given.a_traced_command_pipeline
{
    void Establish() => _serviceProvider.GetService(typeof(AuthorizationDeclarations)).Returns((object?)null);

    async Task Because() => _result = await _commandPipeline.ExecuteHosted(_command, _serviceProvider, null, CancellationToken.None);

    [Fact] void should_report_unauthorized() => _result.IsAuthorized.ShouldBeFalse();
    [Fact] void should_add_the_authorization_outcome() => CommandSpan.GetTagItem(WellKnownTelemetryNames.CommandOutcome).ShouldEqual(WellKnownOperationOutcomes.Authorization);
    [Fact] void should_add_the_command_type() => CommandSpan.GetTagItem(WellKnownTelemetryNames.CommandType).ShouldEqual(typeof(RegisterAuthor).FullName);
    [Fact] void should_record_one_duration() => Durations.Count().ShouldEqual(1);
    [Fact] void should_count_an_authorization_outcome() => Outcomes.Single().Tags[WellKnownTelemetryNames.CommandOutcome].ShouldEqual(WellKnownOperationOutcomes.Authorization);
}
