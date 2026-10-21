// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;

namespace Cratis.Arc.MongoDB.for_MongoCollectionExtensions.when_observing;

public class and_an_operation_without_a_document_is_delivered : given.a_server_backed_observation
{
    ObservedDocument _inserted;
    Emission _afterChanges;

    void Establish() => _inserted = new(Guid.NewGuid(), "C");

    async Task Because()
    {
        await StartObserving();
        PushChange(OperationOf("createIndexes"));
        PushChange(OperationOf("modify"));
        _documents.Add(_inserted);
        PushChange(InsertOf(_inserted));
        _afterChanges = await NextEmission();
    }

    [Fact] void should_ignore_the_operations_that_are_not_changes_to_documents() => Assert.Equal([new(CollectionChangeKind.Added, _inserted.Id)], _afterChanges.Changes);
}
