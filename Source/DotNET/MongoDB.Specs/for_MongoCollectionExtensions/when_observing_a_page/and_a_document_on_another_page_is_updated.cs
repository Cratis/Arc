// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;

namespace Cratis.Arc.MongoDB.for_MongoCollectionExtensions.when_observing_a_page;

public class and_a_document_on_another_page_is_updated : given.a_server_backed_observation
{
    ObservedDocument _offPage;
    ObservedDocument _onPage;
    Emission _initial;
    Emission _afterChanges;

    void Establish()
    {
        PageByName(1, 2);
        _offPage = new(Guid.NewGuid(), "A");
        _onPage = new(Guid.NewGuid(), "C");
        _documents = [_offPage, new(Guid.NewGuid(), "B"), _onPage];
        StubUpdateBelongsCheck(belongs: true);
    }

    async Task Because()
    {
        _initial = await StartObserving();
        Update(_offPage with { Name = "A2" });
        Update(_onPage with { Name = "C2" });
        _afterChanges = await NextEmission();
    }

    void Update(ObservedDocument document)
    {
        _documents[_documents.FindIndex(_ => _.Id == document.Id)] = document;
        PushChange(UpdateOf(document));
    }

    [Fact] void should_start_with_the_last_page() => Assert.Equal(["C"], _initial.Page.Select(_ => _.Name));
    [Fact] void should_keep_the_total() => _afterChanges.TotalItems.ShouldEqual(3);
    [Fact] void should_not_pull_the_document_onto_the_page() => Assert.Equal(["C2"], _afterChanges.Page.Select(_ => _.Name));
    [Fact] void should_report_only_the_document_on_the_page_as_replaced() => Assert.Equal([new(CollectionChangeKind.Replaced, _onPage.Id)], _afterChanges.Changes);
}
