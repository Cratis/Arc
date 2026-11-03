// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;

namespace Cratis.Arc.Chronicle.ReadModels;

/// <summary>
/// A one-thread <see cref="SynchronizationContext"/> that models a scheduler (such as a grain) which must be yielded to.
/// </summary>
/// <remarks>
/// Work blocked on a continuation that needs this thread never completes while the thread is blocked. <see cref="Dispose"/>
/// is the teardown-only escape: it releases the queue and runs whatever is still posted, so a deliberately
/// deadlocked worker can exit and not leak into later specs.
/// </remarks>
public sealed class SingleThreadScheduler : SynchronizationContext, IDisposable
{
    static readonly TimeSpan _joinTimeout = TimeSpan.FromSeconds(5);
    readonly BlockingCollection<(SendOrPostCallback Callback, object? State)> _queue = new();
    readonly Thread _worker;

    /// <summary>
    /// Initializes a new instance of the <see cref="SingleThreadScheduler"/> class.
    /// </summary>
    public SingleThreadScheduler()
    {
        _worker = new Thread(Pump) { IsBackground = true, Name = nameof(SingleThreadScheduler) };
        _worker.Start();
    }

    /// <summary>
    /// Gets whether the worker thread exited.
    /// </summary>
    public bool HasExited => !_worker.IsAlive;

    /// <inheritdoc/>
    public override void Post(SendOrPostCallback d, object? state)
    {
        if (!_queue.IsAddingCompleted)
        {
            _queue.Add((d, state));
        }
    }

    /// <summary>
    /// Starts work on the scheduler thread.
    /// </summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="work">The work, started on the scheduler thread.</param>
    /// <returns>A task completing with the work.</returns>
    public Task<T> Run<T>(Func<Task<T>> work)
    {
        var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        Post(
            _ => _ = Execute(work, completion),
            null);
        return completion.Task;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _queue.CompleteAdding();
        var previous = Current;
        SetSynchronizationContext(this);
        try
        {
            while (_queue.TryTake(out var item))
            {
                item.Callback(item.State);
            }
        }
        finally
        {
            SetSynchronizationContext(previous);
        }

        _worker.Join(_joinTimeout);
    }

    static async Task Execute<T>(Func<Task<T>> work, TaskCompletionSource<T> completion)
    {
        try
        {
            completion.TrySetResult(await work());
        }
        catch (Exception exception)
        {
            completion.TrySetException(exception);
        }
    }

    void Pump()
    {
        SetSynchronizationContext(this);
        foreach (var (callback, state) in _queue.GetConsumingEnumerable())
        {
            callback(state);
        }
    }
}
