// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Channels;
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

    readonly Channel<ChangeStreamDocument<ObservedDocument>> _auxiliaryChanges = Channel.CreateUnbounded<ChangeStreamDocument<ObservedDocument>>();

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
        _auxiliary.CollectionNamespace.Returns(new CollectionNamespace("testdb", "auxiliary"));
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
        _auxiliary.WatchAsync(
            Arg.Any<PipelineDefinition<ChangeStreamDocument<ObservedDocument>, ChangeStreamDocument<ObservedDocument>>>(),
            Arg.Any<ChangeStreamOptions>(),
            Arg.Any<CancellationToken>()).Returns(_ => Task.FromResult(CreateAuxiliaryCursor()));
    }

    protected void ReleaseInitialQueries()
    {
        _initialQueryGate.SetResult();
        _auxiliaryInitialQueryGate.SetResult();
    }

    protected void PushAuxiliaryChange(ChangeStreamDocument<ObservedDocument> change) => _auxiliaryChanges.Writer.TryWrite(change);

    void Destroy()
    {
        _auxiliaryInitialQueryGate.TrySetResult();
        _auxiliaryChanges.Writer.TryComplete();
    }

    IChangeStreamCursor<ChangeStreamDocument<ObservedDocument>> CreateAuxiliaryCursor()
    {
        var cursor = Substitute.For<IChangeStreamCursor<ChangeStreamDocument<ObservedDocument>>>();
        IEnumerable<ChangeStreamDocument<ObservedDocument>> batch = [];
        cursor.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(async call =>
        {
            if (!await _auxiliaryChanges.Reader.WaitToReadAsync(call.Arg<CancellationToken>()))
            {
                return false;
            }
            var changes = new List<ChangeStreamDocument<ObservedDocument>>();
            while (_auxiliaryChanges.Reader.TryRead(out var change))
            {
                changes.Add(change);
            }
            batch = changes;
            return true;
        });
        cursor.Current.Returns(_ => batch);
        return cursor;
    }
}
