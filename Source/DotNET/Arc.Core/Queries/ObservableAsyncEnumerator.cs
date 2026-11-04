// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;

namespace Cratis.Arc.Queries;

/// <summary>
/// Represents an <see cref="IAsyncEnumerator{T}"/> for a client observables.
/// </summary>
/// <typeparam name="T">Type the enumerator is for.</typeparam>
public class ObservableAsyncEnumerator<T> : IAsyncEnumerator<T>
{
    readonly object _lock = new();
    readonly IDisposable _subscriber;
    readonly CancellationToken _cancellationToken;
    readonly ConcurrentQueue<T> _items = new();
    readonly CancellationTokenRegistration _cancellationRegistration;
    TaskCompletionSource _taskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
    bool _completed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ObservableAsyncEnumerator{T}"/> class.
    /// </summary>
    /// <param name="observable">The observable to return from.</param>
    /// <param name="cancellationToken">Cancellation token that causes enumeration to throw when cancelled before or while waiting for the next item.</param>
    public ObservableAsyncEnumerator(IObservable<T> observable, CancellationToken cancellationToken)
    {
        Current = default!;
        _subscriber = observable.Subscribe(
            onNext: _ =>
            {
                lock (_lock)
                {
                    _items.Enqueue(_);
                    _taskCompletionSource.TrySetResult();
                }
            },
            onError: _ => SignalCompletion(),
            onCompleted: SignalCompletion);
        _cancellationToken = cancellationToken;
        _cancellationRegistration = cancellationToken.Register(static state => ((ObservableAsyncEnumerator<T>)state!).WakeWaiter(), this);
    }

    /// <inheritdoc/>
    public T Current { get; private set; }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        await _cancellationRegistration.DisposeAsync();
        _subscriber.Dispose();
        SignalCompletion();
    }

    /// <summary>
    /// Advances the enumerator to the next item from the observable.
    /// </summary>
    /// <returns>Whether an item is available; false means the observable completed or failed, or the enumerator was disposed.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the enumeration's cancellation token is cancelled before or while waiting for the next item.</exception>
    public async ValueTask<bool> MoveNextAsync()
    {
        _cancellationToken.ThrowIfCancellationRequested();
        Task pending;
        lock (_lock)
        {
            pending = _taskCompletionSource.Task;
        }

        await pending;
        _cancellationToken.ThrowIfCancellationRequested();

        lock (_lock)
        {
            if (_completed && _items.IsEmpty) return false;
            _items.TryDequeue(out var item);
            Current = item!;
            _taskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

            // Check if new items arrived while we were updating TCS
            if (!_items.IsEmpty || _completed)
            {
                _taskCompletionSource.TrySetResult();
            }
        }

        return true;
    }

    void WakeWaiter()
    {
        lock (_lock)
        {
            _taskCompletionSource.TrySetResult();
        }
    }

    void SignalCompletion()
    {
        lock (_lock)
        {
            _completed = true;
            _taskCompletionSource.TrySetResult();
        }
    }
}
