// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;

namespace Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;

/// <summary>
/// A subject that signals once its first observer has subscribed, so a spec can publish only after someone is listening.
/// </summary>
/// <typeparam name="T">Type of item the subject publishes.</typeparam>
public sealed class SubscriptionSignalingSubject<T> : ISubject<T>
{
    readonly Subject<T> _subject = new();
    readonly TaskCompletionSource _subscribed = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    /// Gets the signal completed once an observer has subscribed.
    /// </summary>
    public Task Subscribed => _subscribed.Task;

    /// <inheritdoc/>
    public IDisposable Subscribe(IObserver<T> observer)
    {
        var subscription = _subject.Subscribe(observer);
        _subscribed.TrySetResult();
        return subscription;
    }

    /// <inheritdoc/>
    public void OnNext(T value) => _subject.OnNext(value);

    /// <inheritdoc/>
    public void OnError(Exception error) => _subject.OnError(error);

    /// <inheritdoc/>
    public void OnCompleted() => _subject.OnCompleted();
}
