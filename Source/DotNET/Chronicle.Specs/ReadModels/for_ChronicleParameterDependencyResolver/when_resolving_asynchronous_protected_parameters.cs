// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.DependencyInjection;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Arc.Chronicle.ReadModels.for_ChronicleParameterDependencyResolver;

public class when_resolving_asynchronous_protected_parameters : given.protected_plain_models
{
    readonly TaskCompletionSource<DecisionRead<Balance>> _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    Exception? _failure;
    object? _optional;
    bool _yielded;

    void Establish()
    {
        _inner.GetFor<Balance>(_unit, (ReadModelKey)"from", Arg.Any<CancellationToken>()).Returns(_completion.Task);
    }

    async Task Because()
    {
        using var policy = for_CommandDecisionReads.DecisionPolicyForSpecs.Begin(typeof(Transfer));
        CommandTransaction.Current = _unit;
        using var frame = _unit.EnforceReadProtection();
        CommandDecisionReads.Begin(typeof(Transfer));
        try
        {
            var pending = ParameterDependencyResolver.ResolveAsync(_services, HandleParameters[0], _ => new InvalidOperationException());
            _yielded = !pending.IsCompleted;
            var token = for_CommandDecisionReads.DecisionFixtures.Protected<Balance>("from");
            _unit.AddDecisionRead(token);
            _completion.SetResult(token);
            try
            {
                await pending;
            }
            catch (Exception failure)
            {
                _failure = failure;
            }
            var optional = typeof(Optional).GetMethod(nameof(Optional.Handle))!.GetParameters()[0];
            _optional = await ParameterDependencyResolver.ResolveAsync(_services, optional, _ => new InvalidOperationException());
        }
        finally
        {
            CommandDecisionReads.End();
        }
    }

    [Fact] void should_yield_before_the_missing_read_completes() => _yielded.ShouldBeTrue();
    [Fact] void should_preserve_the_required_missing_model_failure() => _failure.ShouldBeOfExactType<ProtectedReadModelDoesNotExist>();
    [Fact] void should_preserve_optional_absence() => _optional.ShouldBeNull();
    [Fact] void should_share_the_missing_read() => _inner.Received(1).GetFor<Balance>(_unit, (ReadModelKey)"from", Arg.Any<CancellationToken>());

    public class Optional
    {
        public void Handle([LookUpBy(nameof(Transfer.From))] Balance? balance) { }
    }
}
