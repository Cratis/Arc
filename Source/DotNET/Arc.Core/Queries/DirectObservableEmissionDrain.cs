// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries;

/// <summary>
/// Tracks direct subject callbacks so request-owned services outlive every accepted emission.
/// </summary>
internal sealed class DirectObservableEmissionDrain
{
    readonly object _gate = new();
    readonly TaskCompletionSource _drained = new(TaskCreationOptions.RunContinuationsAsynchronously);
    int _active;
    bool _stopped;

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
}
