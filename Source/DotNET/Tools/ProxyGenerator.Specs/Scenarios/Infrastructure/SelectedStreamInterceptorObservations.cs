// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;

/// <summary>Signals the next tenant-bound interceptor invocation on a streamed read model.</summary>
public class SelectedStreamInterceptorObservations
{
    TaskCompletionSource<EmissionObservation> _next = NewSignal();

    /// <summary>Resets observations before emitting an item.</summary>
    public void Reset() => _next = NewSignal();

    /// <summary>Records the intercepted item's identity and tenant.</summary>
    /// <param name="observation">The observed execution identity.</param>
    public void Record(EmissionObservation observation) => _next.TrySetResult(observation);

    /// <summary>Waits for the interceptor's next invocation.</summary>
    /// <returns>The bounded observation.</returns>
    public Task<EmissionObservation> Next() => _next.Task.WaitAsync(TimeSpan.FromSeconds(10));

    static TaskCompletionSource<EmissionObservation> NewSignal() => new(TaskCreationOptions.RunContinuationsAsynchronously);
}
