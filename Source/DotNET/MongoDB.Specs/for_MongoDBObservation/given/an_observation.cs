// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.for_MongoDBObservation.given;

public class an_observation : Specification
{
    protected Subject<ChangeStreamDocument<BsonDocument>> _changes;
    protected IObservable<ChangeStreamDocument<BsonDocument>> _source;
    protected ISubject<int> _subject;
    protected Func<CancellationToken, Task<int>> _snapshot;
    protected Func<TimeSpan, CancellationToken, Task> _delay;
    protected List<int> _values;
    protected List<TimeSpan> _delays;
    protected Exception? _error;
    protected TaskCompletionSource _emitted;
    protected TaskCompletionSource _detached;
    protected int _attempts;

    void Establish()
    {
        _changes = new();
        _values = [];
        _delays = [];
        _emitted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _detached = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _source = Observable.Create<ChangeStreamDocument<BsonDocument>>(observer =>
        {
            var subscription = _changes.Subscribe(observer);
            return Disposable.Create(() =>
            {
                subscription.Dispose();
                _detached.TrySetResult();
            });
        });
        _snapshot = _ => Task.FromResult(Interlocked.Increment(ref _attempts));
        _delay = (delay, _) =>
        {
            _delays.Add(delay);
            return Task.CompletedTask;
        };
    }

    protected void Start()
    {
        _subject = MongoDBObservation.Create(
            _source,
            new HashSet<CollectionNamespace> { new("testdb", "items") },
            _snapshot,
            value => value,
            NullLogger<MongoDBWatcher>.Instance,
            _delay);
        _subject.Subscribe(
            value =>
            {
                _values.Add(value);
                _emitted.TrySetResult();
            },
            error => _error = error);
    }

    protected void Notify() => _changes.OnNext(new ChangeStreamDocument<BsonDocument>(new BsonDocument("operationType", "invalidate"), BsonDocumentSerializer.Instance));

    async Task Destroy()
    {
        if (_subject is IDisposable disposable)
        {
            disposable.Dispose();
            await _detached.Task.WaitAsync(TimeSpan.FromSeconds(5));
        }
        _changes.Dispose();
    }
}
