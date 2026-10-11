// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Disposables;
using System.Reactive.Subjects;

namespace Cratis.Arc.Testing.for_QueryScenario;

public sealed class DisposingObservationSubject : ISubject<ObservableScenarioReadModel>, IDisposable
{
    readonly BehaviorSubject<ObservableScenarioReadModel> _subject = new(new("Initial"));

    public Action DisposeObservation { get; set; } = () => { };
    public TaskCompletionSource TeardownReturned { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public int SubscriptionDisposals { get; private set; }
    public bool HasObservers => _subject.HasObservers;

    public IDisposable Subscribe(IObserver<ObservableScenarioReadModel> observer)
    {
        var subscription = _subject.Subscribe(observer);

        return Disposable.Create(() =>
        {
            SubscriptionDisposals++;
            DisposeObservation();
            subscription.Dispose();
            TeardownReturned.TrySetResult();
        });
    }

    public void OnNext(ObservableScenarioReadModel value) => _subject.OnNext(value);
    public void OnError(Exception error) => _subject.OnError(error);
    public void OnCompleted() => _subject.OnCompleted();
    public void Dispose() => _subject.Dispose();
}
