// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.for_MongoCollectionExtensions.when_observing;

public class and_the_initial_query_has_not_completed : given.an_observed_collection
{
    ISubject<IEnumerable<ObservedDocument>> _subject;
    List<IEnumerable<ObservedDocument>> _emissions = [];

    async Task Because()
    {
        _subject = _collection.Observe();
        using var subscription = _subject.Subscribe(_emissions.Add);
        await Task.Delay(100);
    }

    [Fact] void should_not_emit_anything() => _emissions.ShouldBeEmpty();
}
