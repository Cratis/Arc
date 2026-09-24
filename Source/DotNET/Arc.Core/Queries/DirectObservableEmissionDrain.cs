// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.ExceptionServices;

namespace Cratis.Arc.Queries;

/// <summary>
/// Tracks direct subject callbacks so request-owned services outlive every accepted emission.
/// </summary>
/// <param name="cancellationSource">The connection cancellation source.</param>
internal sealed class DirectObservableEmissionDrain(CancellationTokenSource cancellationSource)
{
    readonly object _gate = new();
    readonly TaskCompletionSource _drained = new(TaskCreationOptions.RunContinuationsAsynchronously);
    int _active;
    bool _stopped;
    Task? _cancellation;

    /// <summary>Registers a callback synchronously before its first await.</summary>
    /// <returns>Whether the callback was accepted.</returns>
    internal bool TryEnter()
    {
        lock (_gate)
        {
            if (_stopped)
            {
                return false;
            }

            _active++;
            return true;
        }
    }

    /// <summary>Releases a completed callback.</summary>
    internal void Exit()
    {
        lock (_gate)
        {
            _active--;
            if (_stopped && _active == 0)
            {
                _drained.TrySetResult();
            }
        }
    }

    /// <summary>Starts cancellation once and retains the task so callback failures cannot be lost.</summary>
    /// <returns>The cancellation task.</returns>
    internal Task Cancel()
    {
        lock (_gate)
        {
            return _cancellation ??= cancellationSource.CancelAsync();
        }
    }

    /// <summary>Stops accepting new callbacks and waits for all accepted callbacks to finish.</summary>
    /// <returns>The drain task.</returns>
    internal Task StopAndDrain()
    {
        lock (_gate)
        {
            _stopped = true;
            if (_active == 0)
            {
                _drained.TrySetResult();
            }
        }

        return _drained.Task;
    }

    /// <summary>Releases transport resources only after every accepted callback has exited.</summary>
    /// <param name="subscription">The subscription to dispose after stopping acceptance.</param>
    /// <param name="failure">The original connection failure, if any.</param>
    /// <param name="stopReceiver">Optional cancellation for a separate receiver.</param>
    /// <returns>A task that completes after all work is drained and failures have been reported.</returns>
    /// <exception cref="ObservableQueryTeardownFailed">Multiple teardown operations failed.</exception>
    internal async Task TerminateAsync(IDisposable? subscription, Exception? failure = null, Func<Task>? stopReceiver = null)
    {
        var failures = new List<Exception>();
        if (failure is not null)
        {
            failures.Add(failure);
        }

        var pending = StopAndDrain();
        if (failure is not null)
        {
            _ = Cancel();
        }

        try
        {
            subscription?.Dispose();
        }
        catch (Exception error)
        {
            failures.Add(error);
            _ = Cancel();
        }

        // Even if cancellation or unsubscription fails, the request's provider and native context must stay
        // available to callbacks already inside an interceptor or guard.
        await pending;
        try
        {
            await Cancel();
        }
        catch (Exception error)
        {
            failures.Add(error);
        }

        if (stopReceiver is not null)
        {
            try
            {
                await stopReceiver();
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
        }

        if (failures.Count == 1)
        {
            ExceptionDispatchInfo.Capture(failures[0]).Throw();
        }

        if (failures.Count > 1)
        {
            throw new ObservableQueryTeardownFailed(failures);
        }
    }
}
