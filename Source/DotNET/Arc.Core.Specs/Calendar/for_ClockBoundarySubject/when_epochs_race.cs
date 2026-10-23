// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Calendar.for_ClockBoundarySubject;

/// <summary>
/// A candidate arriving from another thread while a timer epoch evaluates is queued behind it; evaluations never overlap.
/// </summary>
public class when_epochs_race : given.a_clock_boundary_subject
{
    readonly ManualResetEventSlim _entered = new(false);
    readonly ManualResetEventSlim _release = new(false);
    int _blockNext;
    int _concurrent;
    int _maxConcurrent;

    void Establish()
    {
        _nextBoundary = reading => reading.Instant < Start.AddHours(1) ? Start.AddHours(1) : null;
        _compute = (candidate, _) =>
        {
            RecordConcurrency(Interlocked.Increment(ref _concurrent));
            if (Interlocked.Exchange(ref _blockNext, 0) == 1)
            {
                _entered.Set();
                _release.Wait(TimeSpan.FromSeconds(10));
            }

            Interlocked.Decrement(ref _concurrent);
            return candidate;
        };
        Subscribe();
        _candidates.OnNext(1);
        _blockNext = 1;
    }

    async Task Because()
    {
        var timerEpoch = Task.Run(() => _time.Advance(TimeSpan.FromHours(1)));
        _entered.Wait(TimeSpan.FromSeconds(10));
        var dataEpoch = Task.Run(() => _candidates.OnNext(2));
        await Task.Delay(100);
        _release.Set();
        await Task.WhenAll(timerEpoch, dataEpoch);
    }

    void Destroy()
    {
        _entered.Dispose();
        _release.Dispose();
    }

    [Fact] void should_never_evaluate_concurrently() => _maxConcurrent.ShouldEqual(1);
    [Fact] void should_evaluate_every_epoch() => _evaluations.ShouldEqual(3);
    [Fact] void should_emit_the_data_answer() => Results.ShouldContainOnly(1, 2);

    void RecordConcurrency(int value)
    {
        int current;
        while (value > (current = Volatile.Read(ref _maxConcurrent)))
        {
            Interlocked.CompareExchange(ref _maxConcurrent, value, current);
        }
    }
}
