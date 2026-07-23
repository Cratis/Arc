// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_SerializedEmissionGate.when_emitting;

/// <summary>
/// Emissions must not overlap: the per-emission interception and the change-set bookkeeping the gate protects rely
/// on one emission finishing before the next begins.
/// </summary>
public class while_another_emission_is_in_flight : Specification
{
    SerializedEmissionGate _gate;
    TaskCompletionSource _firstStarted;
    TaskCompletionSource _releaseFirst;
    Task _first;
    Task _second;
    bool _secondRan;
    bool _secondRanWhileFirstHeldTheGate;

    void Establish()
    {
        _gate = new SerializedEmissionGate();
        _firstStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _releaseFirst = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    async Task Because()
    {
        _first = _gate.Emit(FirstEmission, CancellationToken.None);

        // The first emission is now holding the gate.
        await _firstStarted.Task;

        _second = _gate.Emit(SecondEmission, CancellationToken.None);

        // Give the second emission ample opportunity to run if the gate were not serializing.
        await Task.Delay(100);
        _secondRanWhileFirstHeldTheGate = _secondRan;

        _releaseFirst.SetResult();
        await Task.WhenAll(_first, _second);
    }

    async Task FirstEmission()
    {
        _firstStarted.SetResult();
        await _releaseFirst.Task;
    }

    Task SecondEmission()
    {
        _secondRan = true;
        return Task.CompletedTask;
    }

    [Fact] void should_hold_the_second_emission_until_the_first_releases() => _secondRanWhileFirstHeldTheGate.ShouldBeFalse();

    void Destroy() => _gate.Dispose();
}
