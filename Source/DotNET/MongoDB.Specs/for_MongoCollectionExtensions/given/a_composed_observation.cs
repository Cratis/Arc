// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.for_MongoCollectionExtensions.given;

public class a_composed_observation : an_observed_collection
{
    protected IMongoCollection<ObservedDocument> _auxiliary;
    protected List<ObservedDocument> _auxiliaryDocuments;
    protected TaskCompletionSource _auxiliaryInitialQueryGate;
    protected FindOptions<ObservedDocument, ObservedDocument> _auxiliaryFindOptions;

    protected const string AuxiliaryCollectionName = "auxiliary";

    void Establish()
    {
        _queryContext = _queryContext with
        {
            Paging = new Paging(1, 1, true),
            Sorting = new Sorting(nameof(ObservedDocument.Name), Cratis.Arc.Queries.SortDirection.Descending)
        };
        _auxiliaryDocuments =
        [
            new(Guid.NewGuid(), "Auxiliary A"),
            new(Guid.NewGuid(), "Auxiliary B"),
            new(Guid.NewGuid(), "Auxiliary C")
        ];
        _auxiliaryInitialQueryGate = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _auxiliary = Substitute.For<IMongoCollection<ObservedDocument>>();
        _auxiliary.CollectionNamespace.Returns(new CollectionNamespace(DatabaseName, AuxiliaryCollectionName));
        var database = _collection.Database;
        _auxiliary.Database.Returns(database);
        _auxiliary.Settings.Returns(new MongoCollectionSettings());
        _auxiliary.DocumentSerializer.Returns(_ => BsonSerializer.LookupSerializer<ObservedDocument>());
        _auxiliary.CountDocumentsAsync(
            Arg.Any<FilterDefinition<ObservedDocument>>(),
            Arg.Any<CountOptions>(),
            Arg.Any<CancellationToken>()).Returns(_ => Task.FromResult((long)_auxiliaryDocuments.Count));
        _auxiliary.FindAsync(
            Arg.Any<FilterDefinition<ObservedDocument>>(),
            Arg.Any<FindOptions<ObservedDocument, ObservedDocument>>(),
            Arg.Any<CancellationToken>()).Returns(async call =>
            {
                _auxiliaryFindOptions = call.Arg<FindOptions<ObservedDocument, ObservedDocument>>();
                await _auxiliaryInitialQueryGate.Task;
                return CreateCursor(_auxiliaryDocuments);
            });
    }

    protected void ReleaseInitialQueries()
    {
        _initialQueryGate.SetResult();
        _auxiliaryInitialQueryGate.SetResult();
    }

    protected void PushAuxiliaryChange(string operationType, ObservedDocument document) =>
        PushChange(ChangeIn(AuxiliaryCollectionName, operationType, document));

    void Destroy()
    {
        _auxiliaryInitialQueryGate.TrySetResult();
    }
}
