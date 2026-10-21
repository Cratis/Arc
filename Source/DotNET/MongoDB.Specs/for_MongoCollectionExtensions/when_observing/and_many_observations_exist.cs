// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.for_MongoCollectionExtensions.when_observing;

public class and_many_observations_exist : given.an_observed_collection
{
    const int Observations = 30;

    async Task Because()
    {
        var subscriptions = new List<IDisposable>();
        var initialEmissions = new List<Task>();
        for (var i = 0; i < Observations; i++)
        {
            var subject = i % 2 == 0 ? (IObservable<object>)_collection.Observe() : _collection.ObserveSingle();
            initialEmissions.Add(FirstValue(subject));
        }

        _initialQueryGate.SetResult();
        await Task.WhenAll(initialEmissions).WaitAsync(TimeSpan.FromSeconds(10));
        await WaitForSubscribers(Observations);
    }

    [Fact] void should_attach_every_observation_to_the_shared_stream() => _databaseChanges.Subscribers.ShouldEqual(Observations);

    [Fact] async Task should_not_open_a_change_stream_of_its_own() => await _collection.DidNotReceive().WatchAsync(
        Arg.Any<PipelineDefinition<ChangeStreamDocument<ObservedDocument>, ChangeStreamDocument<ObservedDocument>>>(),
        Arg.Any<ChangeStreamOptions>(),
        Arg.Any<CancellationToken>());

    [Fact] void should_ask_the_watcher_for_the_collections_database() =>
        _watcher.Received(Observations).GetChanges(DatabaseName);

    static Task FirstValue(IObservable<object> observable)
    {
        var first = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        observable.Subscribe(_ => first.TrySetResult());
        return first.Task;
    }
}
