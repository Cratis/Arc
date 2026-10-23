// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Arc.Queries;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.for_MongoCollectionExtensions.given;

public class an_observed_collection : Specification
{
    protected const string DatabaseName = "testdb";
    protected const string CollectionName = "observeddocument";

    protected IMongoCollection<ObservedDocument> _collection;
    protected ControlledDatabaseChanges _databaseChanges;
    protected IMongoDBWatcher _watcher;
    protected IMongoDatabase _database;
    protected List<ObservedDocument> _documents;
    protected TaskCompletionSource _initialQueryGate;
    protected QueryContext _queryContext;
    protected FindOptions<ObservedDocument, ObservedDocument> _primaryFindOptions;

    bool _belongs;
    bool _belongsCheckStubbed;
    int _belongsChecks;

    void Establish()
    {
        _documents = [new(Guid.NewGuid(), "First"), new(Guid.NewGuid(), "Second")];
        _initialQueryGate = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _databaseChanges = new();
        _database = Substitute.For<IMongoDatabase>();
        _database.DatabaseNamespace.Returns(new DatabaseNamespace(DatabaseName));
        _watcher = Substitute.For<IMongoDBWatcher>();
        _watcher.GetChanges(_database).Returns(_databaseChanges);

        var queryContextManager = Substitute.For<IQueryContextManager>();
        _queryContext = new QueryContext("ObservedDocuments", CorrelationId.New(), Paging.NotPaged, Sorting.None);
        queryContextManager.Current.Returns(_ => _queryContext);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(queryContextManager);
        services.AddSingleton(_watcher);
        Internals.ServiceProvider = services.BuildServiceProvider();

        _collection = Substitute.For<IMongoCollection<ObservedDocument>>();
        _collection.CollectionNamespace.Returns(new CollectionNamespace(DatabaseName, CollectionName));
        _collection.Database.Returns(_database);
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
            .Returns(async call =>
            {
                _primaryFindOptions = call.Arg<FindOptions<ObservedDocument, ObservedDocument>>();
                await _initialQueryGate.Task;
                return CreateCursor(_documents);
            });

        // Whether a changed document still belongs to the observation is decided by a lookup on the document's
        // own key; unless a spec says otherwise it does.
        StubBelongsCheck(true);
    }

    void Destroy()
    {
        _initialQueryGate.TrySetResult();
        _databaseChanges.Complete();
    }

    /// <summary>
    /// Pushes a change-stream document into the shared database change stream, as if the server had emitted it.
    /// Delivered to every observation of the database in push order.
    /// </summary>
    /// <param name="change">The change to push.</param>
    protected void PushChange(ChangeStreamDocument<BsonDocument> change) => _databaseChanges.Push(change);

    /// <summary>
    /// Stubs the "does the updated document still belong" re-check <c>HandleChange</c> performs after an
    /// update or replace — <c>Find(filter &amp;&amp; _id == id).AnyAsync()</c>, which projects to
    /// <see cref="BsonDocument"/> rather than <see cref="ObservedDocument"/> and is therefore a distinct
    /// generic <c>FindAsync</c> instantiation from the one backing the initial query.
    /// </summary>
    /// <param name="belongs">Whether the re-check should report the document as still matching the filter.</param>
    protected void StubUpdateBelongsCheck(bool belongs) => StubBelongsCheck(belongs);

    /// <summary>
    /// Stubs the "does the changed document belong" re-check performed after an insert, update or replace.
    /// </summary>
    /// <param name="belongs">Whether the re-check should report the document as matching the filter.</param>
    protected void StubBelongsCheck(bool belongs)
    {
        _belongs = belongs;
        if (_belongsCheckStubbed)
        {
            return;
        }

        _belongsCheckStubbed = true;
        _collection
            .FindAsync(
                Arg.Any<FilterDefinition<ObservedDocument>>(),
                Arg.Any<FindOptions<ObservedDocument, BsonDocument>>(),
                Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                var answer = _belongs;
                Interlocked.Increment(ref _belongsChecks);
                return Task.FromResult(CreateCursor(answer ? [new BsonDocument()] : (IEnumerable<BsonDocument>)[]));
            });
    }

    /// <summary>
    /// Waits until the observation has asked whether a changed document belongs the given number of times.
    /// </summary>
    /// <param name="checks">The number of checks to wait for.</param>
    /// <returns>A <see cref="Task"/> that completes when they have been made.</returns>
    protected async Task WaitForBelongsChecks(int checks)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(5);
        while (Volatile.Read(ref _belongsChecks) < checks && DateTime.UtcNow < deadline)
        {
            await Task.Delay(5);
        }

        Volatile.Read(ref _belongsChecks).ShouldBeGreaterThanOrEqual(checks);
    }

    /// <summary>
    /// Builds a change-stream document representing a hard delete of the document with the given id.
    /// </summary>
    /// <param name="id">The identifier of the deleted document.</param>
    /// <returns>The <see cref="ChangeStreamDocument{TDocument}"/> for the delete.</returns>
    protected static ChangeStreamDocument<BsonDocument> DeleteOf(Guid id) =>
        new(
            new BsonDocument
            {
                { "operationType", "delete" },
                { "ns", new BsonDocument { { "db", DatabaseName }, { "coll", CollectionName } } },
                { "documentKey", new BsonDocument("_id", IdValue(id)) }
            },
            BsonSerializer.LookupSerializer<BsonDocument>());

    /// <summary>
    /// Builds a change-stream document representing an insert of the given document.
    /// </summary>
    /// <param name="document">The inserted document.</param>
    /// <returns>The <see cref="ChangeStreamDocument{TDocument}"/> for the insert.</returns>
    protected static ChangeStreamDocument<BsonDocument> InsertOf(ObservedDocument document) => ChangeOf("insert", document);

    /// <summary>
    /// Builds a change-stream document representing an update or replace of the given document — the shape
    /// <c>HandleChange</c> uses to decide whether the document still belongs to the observed filter.
    /// </summary>
    /// <param name="document">The updated document.</param>
    /// <returns>The <see cref="ChangeStreamDocument{TDocument}"/> for the update.</returns>
    protected static ChangeStreamDocument<BsonDocument> UpdateOf(ObservedDocument document) => ChangeOf("update", document);

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

    /// <summary>
    /// Builds a change-stream document for any collection of the database, the way the shared stream delivers it.
    /// </summary>
    /// <param name="collectionName">The collection the change happened in.</param>
    /// <param name="operationType">The operation type.</param>
    /// <param name="document">The document the change concerns.</param>
    /// <returns>The <see cref="ChangeStreamDocument{TDocument}"/> for the change.</returns>
    protected static ChangeStreamDocument<BsonDocument> ChangeIn(string collectionName, string operationType, ObservedDocument document) =>
        new(
            new BsonDocument
            {
                { "operationType", operationType },
                { "ns", new BsonDocument { { "db", DatabaseName }, { "coll", collectionName } } },
                { "documentKey", new BsonDocument("_id", IdValue(document.Id)) },
                { "fullDocument", document.ToBsonDocument() }
            },
            BsonSerializer.LookupSerializer<BsonDocument>());

    /// <summary>
    /// Builds a change-stream document for an operation that carries no document, such as <c>drop</c> or <c>invalidate</c>.
    /// </summary>
    /// <param name="operationType">The operation type.</param>
    /// <returns>The <see cref="ChangeStreamDocument{TDocument}"/> for the operation.</returns>
    protected static ChangeStreamDocument<BsonDocument> OperationOf(string operationType) =>
        new(
            new BsonDocument
            {
                { "operationType", operationType },
                { "ns", new BsonDocument { { "db", DatabaseName }, { "coll", CollectionName } } }
            },
            BsonSerializer.LookupSerializer<BsonDocument>());

    /// <summary>
    /// Builds the notification the shared stream publishes after it was reopened, when changes may have been missed.
    /// It names no collection.
    /// </summary>
    /// <returns>The <see cref="ChangeStreamDocument{TDocument}"/> for the resynchronization.</returns>
    protected static ChangeStreamDocument<BsonDocument> ResynchronizationNotice() =>
        new(new BsonDocument("operationType", "invalidate"), BsonSerializer.LookupSerializer<BsonDocument>());

    /// <summary>
    /// Waits until the given number of observations have attached to the shared database change stream.
    /// </summary>
    /// <param name="subscribers">The number of subscribers to wait for.</param>
    /// <returns>A <see cref="Task"/> that completes when they have attached.</returns>
    protected async Task WaitForSubscribers(int subscribers)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(5);
        while (_databaseChanges.Subscribers < subscribers && DateTime.UtcNow < deadline)
        {
            await Task.Delay(5);
        }

        _databaseChanges.Subscribers.ShouldEqual(subscribers);
    }

    static ChangeStreamDocument<BsonDocument> ChangeOf(string operationType, ObservedDocument document) =>
        ChangeIn(CollectionName, operationType, document);

    /// <summary>
    /// Wraps a document id the way the server would encode it on the wire. <see cref="BsonValue.Create(object)"/>
    /// throws for a raw <see cref="Guid"/> — the driver requires an explicit GUID representation rather than
    /// guessing one.
    /// </summary>
    /// <param name="id">The id to wrap.</param>
    /// <returns>The <see cref="BsonValue"/> representing the id.</returns>
    static BsonValue IdValue(Guid id) => new BsonBinaryData(id, GuidRepresentation.Standard);

    protected static IAsyncCursor<T> CreateCursor<T>(IEnumerable<T> documents)
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

    /// <summary>
    /// The shared database change stream, driven by a spec: it delivers pushed changes to every subscriber, counts
    /// the subscribers still attached and can be completed or failed.
    /// </summary>
    protected sealed class ControlledDatabaseChanges : IObservable<ChangeStreamDocument<BsonDocument>>
    {
        readonly Subject<ChangeStreamDocument<BsonDocument>> _subject = new();
        int _subscribers;

        /// <summary>
        /// Gets the number of subscriptions that have not been disposed.
        /// </summary>
        public int Subscribers => Volatile.Read(ref _subscribers);

        /// <inheritdoc/>
        public IDisposable Subscribe(IObserver<ChangeStreamDocument<BsonDocument>> observer)
        {
            Interlocked.Increment(ref _subscribers);
            var subscription = _subject.Subscribe(observer);
            return System.Reactive.Disposables.Disposable.Create(() =>
            {
                subscription.Dispose();
                Interlocked.Decrement(ref _subscribers);
            });
        }

        /// <summary>
        /// Delivers a change to every subscriber.
        /// </summary>
        /// <param name="change">The change to deliver.</param>
        public void Push(ChangeStreamDocument<BsonDocument> change) => _subject.OnNext(change);

        /// <summary>
        /// Completes the stream, as the watcher does when it is disposed.
        /// </summary>
        public void Complete() => _subject.OnCompleted();

        /// <summary>
        /// Fails the stream.
        /// </summary>
        /// <param name="error">The error to fail with.</param>
        public void Fail(Exception error) => _subject.OnError(error);
    }
}
