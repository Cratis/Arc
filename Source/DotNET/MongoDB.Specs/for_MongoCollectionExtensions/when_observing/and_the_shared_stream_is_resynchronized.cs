// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;

namespace Cratis.Arc.MongoDB.for_MongoCollectionExtensions.when_observing;

public class and_the_shared_stream_is_resynchronized : given.a_server_backed_observation
{
    ObservedDocument _missed;
    Emission _initial;
    Emission _afterResynchronization;

    void Establish() => _missed = new(Guid.NewGuid(), "Missed");

    async Task Because()
    {
        _initial = await StartObserving();

        // The shared stream reopens after a failure and may have lost changes, so it says so instead of replaying
        // them. The observation has to read the collection again to pick up what it missed.
        _documents.Add(_missed);
        PushChange(ResynchronizationNotice());
        _afterResynchronization = await NextEmission();
    }

    [Fact] void should_not_have_known_the_missed_document() => _initial.Page.ShouldNotContain(_missed);
    [Fact] void should_read_the_missed_document() => _afterResynchronization.Page.ShouldContain(_missed);
    [Fact] void should_report_the_total_it_read() => _afterResynchronization.TotalItems.ShouldEqual(_initial.TotalItems + 1);
    [Fact] void should_report_the_document_as_added() => Assert.Equal([new(CollectionChangeKind.Added, _missed.Id)], _afterResynchronization.Changes);
}
