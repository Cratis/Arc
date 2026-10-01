// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Arc.Authorization;

namespace Cratis.Arc.Commands.for_CommandPipeline.when_executing;

/// <summary>
/// Validating a hosted command can be turned away before the pipeline proper runs. The validate span still says why.
/// </summary>
public class and_it_is_traced_and_validating_hosted_authorization_cannot_be_resolved : given.a_traced_command_pipeline
{
    void Establish() => _serviceProvider.GetService(typeof(AuthorizationDeclarations)).Returns((object?)null);

    async Task Because() => _result = await _commandPipeline.ValidateHosted(_command, _serviceProvider, null, CancellationToken.None);

    Activity ValidateSpan => _telemetry.Span(WellKnownTelemetryNames.CommandValidateSpan);

    [Fact] void should_report_unauthorized() => _result.IsAuthorized.ShouldBeFalse();
    [Fact] void should_add_the_authorization_outcome() => ValidateSpan.GetTagItem(WellKnownTelemetryNames.CommandOutcome).ShouldEqual(WellKnownOperationOutcomes.Authorization);
    [Fact] void should_record_the_exception_type() => ValidateSpan.Events.Single(_ => _.Name == WellKnownTelemetryNames.ExceptionEvent).Tags.Single().Value.ShouldEqual(typeof(InvalidAuthorizationConfiguration).FullName);
}
