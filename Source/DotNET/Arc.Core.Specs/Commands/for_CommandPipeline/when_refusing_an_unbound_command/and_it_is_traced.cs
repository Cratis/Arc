// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;

namespace Cratis.Arc.Commands.for_CommandPipeline.when_refusing_an_unbound_command;

public class and_it_is_traced : for_CommandPipeline.given.a_traced_command_pipeline
{
    void Establish()
    {
        var principal = Substitute.For<ICurrentPrincipalAccessor>();
        var evaluator = Substitute.For<IAuthorizationEvaluator>();
        evaluator.IsAuthorized(typeof(RegisterAuthor)).Returns(true);
        var declarations = new AuthorizationDeclarations(new KnownInstancesOf<IAnonymousEvaluator>([]), new KnownInstancesOf<IAuthorizationAttributeEvaluator>([]));
        _serviceProvider.GetService(typeof(IAuthorizationEvaluator)).Returns(evaluator);
        _serviceProvider.GetService(typeof(AuthorizationEvaluation)).Returns(new AuthorizationEvaluation(declarations, evaluator, principal, new ArcAuthorizationPolicyRuntime([])));
    }

    async Task Because() => _result = await _commandPipeline.RefuseUnboundHosted(new UnboundCommandRefusalRequest(typeof(RegisterAuthor), CommandResult.InvalidBody(_correlationId), _serviceProvider, false), CancellationToken.None);

    [Fact] void should_report_validation() => _result.IsValid.ShouldBeFalse();
    [Fact] void should_add_the_validation_outcome() => CommandSpan.GetTagItem(WellKnownTelemetryNames.CommandOutcome).ShouldEqual(WellKnownOperationOutcomes.Validation);
    [Fact] void should_add_the_command_type() => CommandSpan.GetTagItem(WellKnownTelemetryNames.CommandType).ShouldEqual(typeof(RegisterAuthor).FullName);
    [Fact] void should_record_one_duration() => Durations.Count().ShouldEqual(1);
    [Fact] void should_count_a_validation_outcome() => Outcomes.Single().Tags[WellKnownTelemetryNames.CommandOutcome].ShouldEqual(WellKnownOperationOutcomes.Validation);
}
