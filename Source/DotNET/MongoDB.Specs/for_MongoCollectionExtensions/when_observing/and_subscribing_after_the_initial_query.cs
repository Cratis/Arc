// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.for_MongoCollectionExtensions.when_observing;

public class and_subscribing_after_the_initial_query : given.an_observed_collection
{
    ISubject<IEnumerable<ObservedDocument>> _subject;
    IEnumerable<ObservedDocument> _firstEmission;

    async Task Because()
    {
        _subject = _collection.Observe();
        var initialEmission = FirstEmission(_subject, TimeSpan.FromSeconds(5));
        _initialQueryGate.SetResult();
        await initialEmission;

        _firstEmission = await FirstEmission(_subject, TimeSpan.FromSeconds(5));
    }

    [Fact] void should_replay_the_documents_from_the_initial_query() => _firstEmission.ShouldContainOnly(_documents);
}
