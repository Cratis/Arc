// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.for_MongoCollectionExtensions.when_observing_single;

public class and_no_document_matches_the_initial_query : given.an_observed_collection
{
    ISubject<ObservedDocument> _subject;
    ObservedDocument? _firstEmission;

    void Establish() => _documents = [];

    async Task Because()
    {
        _subject = _collection.ObserveSingle();
        var emission = FirstSingleEmission(_subject, TimeSpan.FromSeconds(5));
        _initialQueryGate.SetResult();
        _firstEmission = await emission;
    }

    [Fact] void should_emit_the_default_value() => _firstEmission.ShouldBeNull();
}
