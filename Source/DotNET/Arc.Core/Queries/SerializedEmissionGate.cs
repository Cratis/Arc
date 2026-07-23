// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries;

/// <summary>
/// Serializes asynchronous observable-query emissions and stays safe across subscription teardown.
/// </summary>
/// <remarks>
/// Emissions are gated one at a time so the per-emission read-model interception and the change-set's
/// previous/current bookkeeping stay ordered even when the underlying subject delivers the next value before the
/// previous one has finished. The emissions are driven from an <c>async void</c> observer callback on the
/// change-stream thread, so an exception escaping an emission would be unobserved and would terminate the process.
/// Once the gate is disposed or the subscription's token is cancelled — both of which happen as a connection is
/// torn down, and in either order — further emissions are dropped rather than throwing.
/// </remarks>
internal sealed class SerializedEmissionGate : IDisposable
{
    readonly SemaphoreSlim _gate = new(1, 1);
    volatile bool _disposed;

    /// <summary>
    /// Runs an emission serialized against every other emission through this gate, dropping it (without throwing)
    /// once the gate is disposed or the token is cancelled.
    /// </summary>
    /// <param name="emission">The emission to run while holding the gate.</param>
    /// <param name="cancellationToken">The subscription's <see cref="CancellationToken"/>.</param>
    /// <returns>A <see cref="Task"/> that completes when the emission has run or been dropped.</returns>
    public async Task Emit(Func<Task> emission, CancellationToken cancellationToken)
    {
        if (_disposed || cancellationToken.IsCancellationRequested)
        {
            return;
        }

        try
        {
            await _gate.WaitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // The subscription was torn down while waiting for the gate — nothing to emit.
            return;
        }
        catch (ObjectDisposedException)
        {
            // The gate was disposed while waiting — nothing to emit.
            return;
        }

        try
        {
            // Disposal can win the race between the guard above and acquiring the gate; a parked emission that
            // wakes up after teardown is dropped rather than pushed to a dead connection.
            if (_disposed)
            {
                return;
            }

            await emission();
        }
        finally
        {
            try
            {
                _gate.Release();
            }
            catch (ObjectDisposedException)
            {
                // The subscription was disposed mid-emission; the gate is gone and nothing waits on it.
            }
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _disposed = true;
        _gate.Dispose();
    }
}
