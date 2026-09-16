// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using System.Threading.Channels;
using Cratis.Arc.Queries;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.for_MongoCollectionExtensions.given;

public class an_observed_collection : Specification
{
    protected IMongoCollection<ObservedDocument> _collection;
    protected List<ObservedDocument> _documents;
    protected TaskCompletionSource _initialQueryGate;
    protected CancellationTokenSource _changeStreamLifetime;

    Channel<ChangeStreamDocument<ObservedDocument>> _changes;

    void Establish()
    {
        _documents = [new(Guid.NewGuid(), "First"), new(Guid.NewGuid(), "Second")];
        _initialQueryGate = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _changeStreamLifetime = new();
        _changes = Channel.CreateUnbounded<ChangeStreamDocument<ObservedDocument>>();

        var queryContextManager = Substitute.For<IQueryContextManager>();
        queryContextManager.Current.Returns(new QueryContext("ObservedDocuments", CorrelationId.New(), Paging.NotPaged, Sorting.None));

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(queryContextManager);
        Internals.ServiceProvider = services.BuildServiceProvider();

        _collection = Substitute.For<IMongoCollection<ObservedDocument>>();
        _collection.CollectionNamespace.Returns(new CollectionNamespace("testdb", "observeddocument"));
        _collection.Settings.Returns(new MongoCollectionSettings());

        // Needed to render a real filter expression (rather than the trivial "_ => true" default) into the
        // change stream's $match stage — required by the tests that observe with a genuine predicate. Deferred
        // to first access rather than resolved here: BsonClassMap registration for a type is a one-time,
        // process-global operation, and resolving it eagerly during Establish (on the test-runner thread)
        // raced a parallel spec's naming-policy substitution. Deferring it lets the lookup happen lazily on
        // the SUT's own background Watch() thread, at the same point every other class-map touch in this SUT
        // already happens.
        _collection.DocumentSerializer.Returns(_ => BsonSerializer.LookupSerializer<ObservedDocument>());

        _collection
            .CountDocumentsAsync(
                Arg.Any<FilterDefinition<ObservedDocument>>(),
                Arg.Any<CountOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult((long)_documents.Count));

        // The initial find is gated so a spec can observe the subject both before and after the query completes —
        // the whole point being that nothing is emitted until it has.
        _collection
            .FindAsync(
                Arg.Any<FilterDefinition<ObservedDocument>>(),
                Arg.Any<FindOptions<ObservedDocument, ObservedDocument>>(),
                Arg.Any<CancellationToken>())
            .Returns(async _ =>
            {
                await _initialQueryGate.Task;
                return CreateCursor(_documents);
            });

        // The change stream stays open for the lifetime of the spec, so the observable is never completed and
        // disposed underneath a subscriber. Changes pushed through PushChange() are delivered to the SUT's
        // ForEachAsync loop the next time it drains the channel, in push order.
        _collection
            .WatchAsync(
                Arg.Any<PipelineDefinition<ChangeStreamDocument<ObservedDocument>, ChangeStreamDocument<ObservedDocument>>>(),
                Arg.Any<ChangeStreamOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(CreateChangeStreamCursor()));
    }

    void Destroy()
    {
        _initialQueryGate.TrySetResult();
        _changes.Writer.TryComplete();
        _changeStreamLifetime.Cancel();
        _changeStreamLifetime.Dispose();
    }

    /// <summary>
    /// Pushes a change-stream document into the observed collection's change stream, as if the server had
    /// emitted it. Delivered to the SUT's change-stream loop the next time it reads the channel.
    /// </summary>
    /// <param name="change">The change to push.</param>
    protected void PushChange(ChangeStreamDocument<ObservedDocument> change) => _changes.Writer.TryWrite(change);

    /// <summary>
    /// Stubs the "does the updated document still belong" re-check <c>HandleChange</c> performs after an
    /// update or replace — <c>Find(filter &amp;&amp; _id == id).AnyAsync()</c>, which projects to
    /// <see cref="BsonDocument"/> rather than <see cref="ObservedDocument"/> and is therefore a distinct
    /// generic <c>FindAsync</c> instantiation from the one backing the initial query.
    /// </summary>
    /// <param name="belongs">Whether the re-check should report the document as still matching the filter.</param>
    protected void StubUpdateBelongsCheck(bool belongs) =>
        _collection
            .FindAsync(
                Arg.Any<FilterDefinition<ObservedDocument>>(),
                Arg.Any<FindOptions<ObservedDocument, BsonDocument>>(),
                Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(CreateCursor(belongs ? [new BsonDocument()] : (IEnumerable<BsonDocument>)[])));

    /// <summary>
    /// Builds a change-stream document representing a hard delete of the document with the given id.
    /// </summary>
    /// <param name="id">The identifier of the deleted document.</param>
    /// <returns>The <see cref="ChangeStreamDocument{TDocument}"/> for the delete.</returns>
    protected static ChangeStreamDocument<ObservedDocument> DeleteOf(Guid id) =>
        new(
            new BsonDocument
            {
                { "operationType", "delete" },
                { "documentKey", new BsonDocument("_id", IdValue(id)) }
            },
            BsonSerializer.LookupSerializer<ObservedDocument>());

    /// <summary>
    /// Builds a change-stream document representing an insert of the given document.
    /// </summary>
    /// <param name="document">The inserted document.</param>
    /// <returns>The <see cref="ChangeStreamDocument{TDocument}"/> for the insert.</returns>
    protected static ChangeStreamDocument<ObservedDocument> InsertOf(ObservedDocument document) => ChangeOf("insert", document);

    /// <summary>
    /// Builds a change-stream document representing an update or replace of the given document — the shape
    /// <c>HandleChange</c> uses to decide whether the document still belongs to the observed filter.
    /// </summary>
    /// <param name="document">The updated document.</param>
    /// <returns>The <see cref="ChangeStreamDocument{TDocument}"/> for the update.</returns>
    protected static ChangeStreamDocument<ObservedDocument> UpdateOf(ObservedDocument document) => ChangeOf("update", document);

    protected static async Task<IEnumerable<ObservedDocument>> FirstEmission(
        ISubject<IEnumerable<ObservedDocument>> subject,
        TimeSpan timeout)
    {
        var emission = new TaskCompletionSource<IEnumerable<ObservedDocument>>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var subscription = subject.Subscribe(documents => emission.TrySetResult(documents));
        return await emission.Task.WaitAsync(timeout);
    }

    /// <summary>
    /// Awaits the first value a single-document subject emits — including <see langword="default"/>, which is
    /// the "no such document" value emitted after a delete, a filter-narrowing update, or an empty initial query.
    /// </summary>
    /// <param name="subject">The subject to observe.</param>
    /// <param name="timeout">The maximum time to wait.</param>
    /// <returns>The first emitted value.</returns>
    protected static async Task<ObservedDocument?> FirstSingleEmission(
        ISubject<ObservedDocument> subject,
        TimeSpan timeout)
    {
        var emission = new TaskCompletionSource<ObservedDocument?>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var subscription = subject.Subscribe(document => emission.TrySetResult(document));
        return await emission.Task.WaitAsync(timeout);
    }

    /// <summary>
    /// Starts recording every value a single-document subject emits, in order. A fresh subscription per
    /// awaited value would race the subject's replay buffer — it would immediately see whatever was last
    /// emitted rather than waiting for the next one — so a spec that needs to observe more than one emission
    /// subscribes once through this and awaits them in sequence.
    /// </summary>
    /// <param name="subject">The subject to observe.</param>
    /// <returns>The <see cref="EmissionSequence"/> recording emissions.</returns>
    protected static EmissionSequence RecordEmissions(ISubject<ObservedDocument> subject) => new(subject);

    static ChangeStreamDocument<ObservedDocument> ChangeOf(string operationType, ObservedDocument document) =>
        new(
            new BsonDocument
            {
                { "operationType", operationType },
                { "documentKey", new BsonDocument("_id", IdValue(document.Id)) },
                { "fullDocument", document.ToBsonDocument() }
            },
            BsonSerializer.LookupSerializer<ObservedDocument>());

    /// <summary>
    /// Wraps a document id the way the server would encode it on the wire. <see cref="BsonValue.Create(object)"/>
    /// throws for a raw <see cref="Guid"/> — the driver requires an explicit GUID representation rather than
    /// guessing one.
    /// </summary>
    /// <param name="id">The id to wrap.</param>
    /// <returns>The <see cref="BsonValue"/> representing the id.</returns>
    static BsonValue IdValue(Guid id) => new BsonBinaryData(id, GuidRepresentation.Standard);

    IChangeStreamCursor<ChangeStreamDocument<ObservedDocument>> CreateChangeStreamCursor()
    {
        var cursor = Substitute.For<IChangeStreamCursor<ChangeStreamDocument<ObservedDocument>>>();
        var currentBatch = (IEnumerable<ChangeStreamDocument<ObservedDocument>>)[];

        cursor.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(async callInfo =>
        {
            var sutToken = callInfo.Arg<CancellationToken>();
            using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(sutToken, _changeStreamLifetime.Token);
            if (!await _changes.Reader.WaitToReadAsync(linkedTokenSource.Token))
            {
                return false;
            }

            var batch = new List<ChangeStreamDocument<ObservedDocument>>();
            while (_changes.Reader.TryRead(out var change))
            {
                batch.Add(change);
            }
            currentBatch = batch;
            return true;
        });
        cursor.Current.Returns(_ => currentBatch);
        return cursor;
    }

    static IAsyncCursor<T> CreateCursor<T>(IEnumerable<T> documents)
    {
        var list = documents.ToList();
        var cursor = Substitute.For<IAsyncCursor<T>>();
        cursor.Current.Returns(list);
        cursor.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(true), Task.FromResult(false));
        return cursor;
    }

    /// <summary>
    /// Sequentially records emissions from a single-document subject and lets a spec await them one at a
    /// time, each backed by its own pending <see cref="TaskCompletionSource"/> rather than a poll loop.
    /// </summary>
    protected sealed class EmissionSequence : IDisposable
    {
        readonly object _gate = new();
        readonly List<ObservedDocument?> _received = [];
        readonly IDisposable _subscription;
        TaskCompletionSource _signal = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int _consumed;

        public EmissionSequence(ISubject<ObservedDocument> subject) => _subscription = subject.Subscribe(OnNext);

        /// <summary>
        /// Awaits the next emission not yet consumed by a previous call, in the order it was received.
        /// </summary>
        /// <param name="timeout">The maximum time to wait for it to arrive.</param>
        /// <returns>The emitted value.</returns>
        public async Task<ObservedDocument?> Next(TimeSpan timeout)
        {
            while (true)
            {
                Task signalTask;
                lock (_gate)
                {
                    if (_consumed < _received.Count)
                    {
                        return _received[_consumed++];
                    }
                    signalTask = _signal.Task;
                }
                await signalTask.WaitAsync(timeout);
            }
        }

        /// <inheritdoc/>
        public void Dispose() => _subscription.Dispose();

        void OnNext(ObservedDocument? value)
        {
            lock (_gate)
            {
                _received.Add(value);
                _signal.TrySetResult();
                _signal = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            }
        }
    }
}
