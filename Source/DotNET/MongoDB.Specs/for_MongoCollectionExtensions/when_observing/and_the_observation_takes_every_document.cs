// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.for_MongoCollectionExtensions.when_observing;

public class and_the_observation_takes_every_document : given.an_observed_collection
{
    ObservedDocument _inserted;
    IEnumerable<ObservedDocument> _afterInsert;

    void Establish() => _inserted = new(Guid.NewGuid(), "Inserted");

    async Task Because()
    {
        var subject = _collection.Observe(FilterDefinition<ObservedDocument>.Empty);
        var emissions = new List<IEnumerable<ObservedDocument>>();
        var second = new TaskCompletionSource<IEnumerable<ObservedDocument>>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var subscription = subject.Subscribe(documents =>
        {
            emissions.Add(documents);
            if (emissions.Count == 2)
            {
                second.TrySetResult(documents);
            }
        });
        _initialQueryGate.SetResult();
        await WaitForSubscribers(1);

        // Were the document looked up it would be reported as outside the filter; an observation of everything
        // has no filter to ask about, so the insert has to be taken without a lookup.
        StubBelongsCheck(false);
        PushChange(InsertOf(_inserted));
        _afterInsert = await second.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact] void should_add_the_inserted_document() => _afterInsert.ShouldContain(_inserted);
    [Fact] async Task should_not_look_the_document_up() => await _collection.DidNotReceive().FindAsync(
        Arg.Any<FilterDefinition<ObservedDocument>>(),
        Arg.Any<FindOptions<ObservedDocument, BsonDocument>>(),
        Arg.Any<CancellationToken>());
}
