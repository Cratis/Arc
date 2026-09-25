// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;

/// <summary>
/// Signals the next real hub emission guard invocation without retaining request contexts or scopes.
/// </summary>
public class SelectedEmissionObservations
{
    TaskCompletionSource<EmissionObservation> _next = NewSignal();
    TaskCompletionSource? _paused;
    TaskCompletionSource? _release;

    /// <summary>
    /// Starts a new observation window after an initial subscription snapshot has been delivered.
    /// </summary>
    public void Reset()
    {
        _next = NewSignal();
        _paused = null;
        _release = null;
    }

    /// <summary>Pauses the next emission guard across an await.</summary>
    public void PauseNext()
    {
        _paused = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    /// <summary>Waits for an emission to reach the asynchronous gate.</summary>
    /// <returns>The bounded gate entry.</returns>
    public Task WaitForPause() => (_paused?.Task ?? Task.CompletedTask).WaitAsync(TimeSpan.FromSeconds(10));

    /// <summary>Releases the gated emission.</summary>
    public void Release() => _release?.TrySetResult();

    /// <summary>Suspends the guard when a test requests an in-flight emission.</summary>
    /// <returns>The suspension.</returns>
    public async Task WaitIfPaused()
    {
        var paused = _paused;
        var release = _release;
        if (paused is not null && release is not null)
        {
            paused.TrySetResult();
            await release.Task.WaitAsync(TimeSpan.FromSeconds(10));
        }
    }

    /// <summary>
    /// Records the emission's explicit principal and scoped tenant.
    /// </summary>
    /// <param name="observation">The guard observation.</param>
    public void Record(EmissionObservation observation) => _next.TrySetResult(observation);

    /// <summary>
    /// Waits for the next emission guard callback.
    /// </summary>
    /// <returns>The recorded identity and tenant.</returns>
    public Task<EmissionObservation> Next() => _next.Task.WaitAsync(TimeSpan.FromSeconds(10));

    static TaskCompletionSource<EmissionObservation> NewSignal() => new(TaskCreationOptions.RunContinuationsAsynchronously);
}
