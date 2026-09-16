// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.for_MongoCollectionExtensions.when_observing;

/// <summary>
/// A subscriber that arrives after the initial query completed still sees its result, because the subject
/// replays the latest value.
/// </summary>
/// <remarks>
/// The first subscription is deliberately held open for the whole spec rather than taken through
/// <c>FirstEmission</c>, which disposes its own. Letting it go would drop the subscriber count to zero, and
/// the subject stops and disposes itself once the last subscriber leaves — so the second subscribe would
/// race that teardown rather than exercise the replay this spec is about.
/// </remarks>
public class and_subscribing_after_the_initial_query : given.an_observed_collection
{
    ISubject<IEnumerable<ObservedDocument>> _subject;
    IEnumerable<ObservedDocument> _firstEmission;
    IDisposable _firstSubscription;

    async Task Because()
    {
        _subject = _collection.Observe();

        var initialEmission = new TaskCompletionSource<IEnumerable<ObservedDocument>>(TaskCreationOptions.RunContinuationsAsynchronously);
        _firstSubscription = _subject.Subscribe(documents => initialEmission.TrySetResult(documents));
        _initialQueryGate.SetResult();
        await initialEmission.Task.WaitAsync(TimeSpan.FromSeconds(5));

        _firstEmission = await FirstEmission(_subject, TimeSpan.FromSeconds(5));
    }

    void Destroy() => _firstSubscription?.Dispose();

    [Fact] void should_replay_the_documents_from_the_initial_query() => _firstEmission.ShouldContainOnly(_documents);
}
