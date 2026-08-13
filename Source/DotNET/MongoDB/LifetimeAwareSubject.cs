// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;

namespace Cratis.Arc.MongoDB;

/// <summary>
/// Represents an <see cref="ISubject{T}"/> that invokes a callback when the last external subscriber unsubscribes.
/// </summary>
/// <param name="inner">The inner <see cref="ISubject{T}"/> to delegate to.</param>
/// <param name="onNoSubscribers">The callback to invoke when the last subscriber unsubscribes.</param>
/// <typeparam name="T">The type of elements in the subject.</typeparam>
internal sealed class LifetimeAwareSubject<T>(ISubject<T> inner, Action onNoSubscribers) : ISubject<T>, IDisposable
{
    int _subscriberCount;
    int _stopped;

    /// <summary>
    /// Creates a new <see cref="LifetimeAwareSubject{T}"/> backed by a <see cref="ReplaySubject{T}"/> holding the
    /// single latest value.
    /// </summary>
    /// <param name="onNoSubscribers">The callback to invoke when the last subscriber unsubscribes.</param>
    /// <returns>A new <see cref="LifetimeAwareSubject{T}"/> instance.</returns>
    /// <remarks>
    /// The producers behind this subject run their initial query asynchronously. Replaying the latest value means a
    /// subscriber arriving after that query completed still sees its result, without the subject carrying a value that
    /// predates the query.
    /// </remarks>
    public static LifetimeAwareSubject<T> Create(Action onNoSubscribers)
    {
#pragma warning disable CA2000 // Dispose objects before losing scope
        return new LifetimeAwareSubject<T>(new ReplaySubject<T>(1), onNoSubscribers);
#pragma warning restore CA2000 // Dispose objects before losing scope
    }

    /// <inheritdoc/>
    public void OnCompleted()
    {
        try
        {
            inner.OnCompleted();
        }
        finally
        {
            Stop();
        }
    }

    /// <inheritdoc/>
    public void OnError(Exception error)
    {
        try
        {
            inner.OnError(error);
        }
        finally
        {
            Stop();
        }
    }

    /// <inheritdoc/>
    public void OnNext(T value) => inner.OnNext(value);

    /// <inheritdoc/>
    public IDisposable Subscribe(IObserver<T> observer)
    {
        var subscription = inner.Subscribe(observer);
        Interlocked.Increment(ref _subscriberCount);
        return new Subscription(this, subscription);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Stop();
        if (inner is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    void Release()
    {
        if (Interlocked.Decrement(ref _subscriberCount) == 0)
        {
            Stop();
        }
    }

    void Stop()
    {
        if (Interlocked.Exchange(ref _stopped, 1) == 0)
        {
            onNoSubscribers();
        }
    }

    sealed class Subscription(LifetimeAwareSubject<T> owner, IDisposable innerSubscription) : IDisposable
    {
        int _disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 1)
            {
                return;
            }

            innerSubscription.Dispose();
            owner.Release();
        }
    }
}