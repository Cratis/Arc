// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;

namespace Cratis.Arc.MongoDB.for_MongoCollectionExtensions.when_observing;

public class and_an_inserted_document_is_inside_the_filter : given.a_server_backed_observation
{
    ObservedDocument _inserted;
    Emission _initial;
    Emission _afterInsert;

    void Establish() => _inserted = new(Guid.NewGuid(), "C");

    async Task Because()
    {
        _initial = await StartObserving();
        _documents.Add(_inserted);
        PushChange(InsertOf(_inserted));
        _afterInsert = await NextEmission();
    }

    [Fact] void should_add_the_document() => _afterInsert.Page.ShouldContain(_inserted);
    [Fact] void should_report_the_insert() => Assert.Equal([new(CollectionChangeKind.Added, _inserted.Id)], _afterInsert.Changes);
    [Fact] void should_increase_the_total() => _afterInsert.TotalItems.ShouldEqual(_initial.TotalItems + 1);
}
