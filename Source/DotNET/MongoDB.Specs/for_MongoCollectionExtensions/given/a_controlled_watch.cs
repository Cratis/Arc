// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.for_MongoCollectionExtensions.given;

public class a_controlled_watch : an_observed_collection
{
    protected static readonly TimeSpan Deadline = TimeSpan.FromSeconds(5);
    protected TaskCompletionSource _completed;
    protected IObservable<object> _observable;
    protected IDisposable _subscription;

    TaskCompletionSource _initialEmission;

    void Establish()
    {
        _completed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _initialEmission = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    protected async Task StartObserving(string kind)
    {
        _observable = kind switch
        {
            "single" => _collection.ObserveSingle(),
            "by_id" => _collection.ObserveById(_documents[0].Id),
            _ => _collection.Observe()
        };
        _subscription = _observable.Subscribe(
            _ => _initialEmission.TrySetResult(),
            error => _completed.TrySetException(error),
            () => _completed.TrySetResult());
        _initialQueryGate.SetResult();
        await _initialEmission.Task.WaitAsync(Deadline);
    }

    void Destroy()
    {
        _subscription?.Dispose();
    }
}
