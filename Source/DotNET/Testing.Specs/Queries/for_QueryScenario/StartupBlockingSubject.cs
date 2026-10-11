// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Disposables;
using System.Reactive.Subjects;

namespace Cratis.Arc.Testing.for_QueryScenario;

public sealed class StartupBlockingSubject : ISubject<ObservableScenarioReadModel>, IDisposable
{
    readonly BehaviorSubject<ObservableScenarioReadModel> _subject = new(new("Initial"));
    int _subscriptionDisposals;

    public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public bool HasObservers => _subject.HasObservers;
    public int SubscriptionDisposals => _subscriptionDisposals;

    public IDisposable Subscribe(IObserver<ObservableScenarioReadModel> observer)
    {
        var subscription = _subject.Subscribe(observer);
        Entered.TrySetResult();
        Release.Task.GetAwaiter().GetResult();

        return Disposable.Create(() =>
        {
            subscription.Dispose();
            Interlocked.Increment(ref _subscriptionDisposals);
        });
    }

    public void OnNext(ObservableScenarioReadModel value) => _subject.OnNext(value);
    public void OnError(Exception error) => _subject.OnError(error);
    public void OnCompleted() => _subject.OnCompleted();
    public void Dispose() => _subject.Dispose();
}
