// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;

namespace Cratis.Arc.Commands.for_CommandPipeline.when_refusing_an_unbound_command;

public class and_the_caller_cancels_while_authorization_is_prepared : for_CommandPipeline.given.a_traced_command_pipeline
{
    CancellationTokenSource _cancellation;
    Exception _error;

    void Establish()
    {
        _cancellation = new();
        _cancellation.Cancel();
        var principal = Substitute.For<ICurrentPrincipalAccessor>();
        var evaluator = Substitute.For<IAuthorizationEvaluator>();
        evaluator.IsAuthorized(typeof(RegisterAuthor)).Returns(true);
        var declarations = new AuthorizationDeclarations(new KnownInstancesOf<IAnonymousEvaluator>([]), new KnownInstancesOf<IAuthorizationAttributeEvaluator>([]));
        _serviceProvider.GetService(typeof(IAuthorizationEvaluator)).Returns(evaluator);
        _serviceProvider.GetService(typeof(AuthorizationEvaluation)).Returns(new AuthorizationEvaluation(declarations, evaluator, principal, new ArcAuthorizationPolicyRuntime([])));
    }

    void Destroy() => _cancellation.Dispose();

    async Task Because() => _error = await Catch.Exception(() => _commandPipeline.RefuseUnboundHosted(new UnboundCommandRefusalRequest(typeof(RegisterAuthor), CommandResult.InvalidBody(_correlationId), _serviceProvider, false), _cancellation.Token));

    [Fact] void should_propagate_the_cancellation() => _error.ShouldBeAssignableFrom<OperationCanceledException>();
    [Fact] void should_add_the_cancelled_outcome() => CommandSpan.GetTagItem(WellKnownTelemetryNames.CommandOutcome).ShouldEqual(WellKnownOperationOutcomes.Cancelled);
    [Fact] void should_count_a_cancelled_outcome() => Outcomes.Single().Tags[WellKnownTelemetryNames.CommandOutcome].ShouldEqual(WellKnownOperationOutcomes.Cancelled);
}
