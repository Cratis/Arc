// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;

namespace Cratis.Arc.MongoDB.for_MongoDBJoinedObserveBuilder.when_selecting;

public class and_subscribing_after_the_initial_emission : given.a_joined_observe_builder
{
    ISubject<(IEnumerable<DocumentA> A, IEnumerable<DocumentB> B)> _subject;
    (IEnumerable<DocumentA> A, IEnumerable<DocumentB> B) _emission;

    async Task Because()
    {
        var firstEmit = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _subject = _builder.Select((a, b) => (a, b));

        // The initial subscription stays alive — the joined observable tears itself down once its last subscriber
        // leaves, so this specifies a second subscriber arriving late, not one arriving after everyone left.
        using var initialSubscription = _subject.Subscribe(_ => firstEmit.TrySetResult());
        await firstEmit.Task.WaitAsync(TimeSpan.FromSeconds(5));

        var lateEmit = new TaskCompletionSource<(IEnumerable<DocumentA> A, IEnumerable<DocumentB> B)>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var lateSubscription = _subject.Subscribe(result => lateEmit.TrySetResult(result));
        _emission = await lateEmit.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact] void should_replay_first_collection_documents() => _emission.A.ShouldContainOnly(_docs1);
    [Fact] void should_replay_second_collection_documents() => _emission.B.ShouldContainOnly(_docs2);
}
