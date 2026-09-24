// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.given;

/// <summary>
/// Signals changes to conditions observed by the demultiplexer specs. Capture <see cref="Next"/> before reading
/// the state, so a callback racing with the read cannot leave a waiter asleep after its condition became true.
/// </summary>
public sealed class condition_pulse
{
    readonly object _gate = new();
    TaskCompletionSource _next = NewPulse();

    /// <summary>
    /// Gets the task completed by the next state change.
    /// </summary>
    public Task Next
    {
        get
        {
            lock (_gate)
            {
                return _next.Task;
            }
        }
    }

    /// <summary>
    /// Signals that state changed after a callback updated it.
    /// </summary>
    public void Signal()
    {
        TaskCompletionSource previous;
        lock (_gate)
        {
            previous = _next;
            _next = NewPulse();
        }

        previous.TrySetResult();
    }

    static TaskCompletionSource NewPulse() => new(TaskCreationOptions.RunContinuationsAsynchronously);
}
