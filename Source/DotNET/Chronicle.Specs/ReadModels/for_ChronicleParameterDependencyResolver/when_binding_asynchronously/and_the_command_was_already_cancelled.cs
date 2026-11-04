// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.DependencyInjection;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Arc.Chronicle.ReadModels.for_ChronicleParameterDependencyResolver.when_binding_asynchronously;

public class and_the_command_was_already_cancelled : given.protected_plain_models
{
    Exception _failure;

    async Task Because()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        _context = _context with { CancellationToken = cancellation.Token };
        using var policy = for_CommandDecisionReads.DecisionPolicyForSpecs.Begin(typeof(Transfer));
        CommandTransaction.Current = _unit;
        using var frame = _unit.EnforceReadProtection();
        CommandDecisionReads.Begin(typeof(Transfer));
        try
        {
            _failure = await Catch.Exception(async () => await ParameterDependencyResolver.ResolveAsync(_services, HandleParameters[0], _ => new InvalidOperationException()));
        }
        finally
        {
            CommandDecisionReads.End();
        }
    }

    [Fact] void should_preserve_cancellation() => _failure.ShouldBeOfExactType<OperationCanceledException>();
    [Fact] void should_not_start_acquisition() => _inner.ReceivedCalls().Any(call => call.GetMethodInfo().Name == nameof(IDecisionReads.GetFor)).ShouldBeFalse();
    [Fact] void should_not_read_a_stored_model() => _stored.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_enroll_a_token() => _tokens.ShouldBeEmpty();
}
