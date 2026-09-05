// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.DependencyInjection;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
namespace Cratis.Arc.MongoDB.for_QueryContextAwareSet;

[IgnoreConvention]
public class InMemoryFluentFind<TDocument>(IEnumerable<TDocument> collection, int? limit = null) : FindFluentBase<TDocument, TDocument>
{
    public override FilterDefinition<TDocument> Filter { get; set; }
    public override FindOptions<TDocument, TDocument> Options { get; } = new FindOptions<TDocument>();

    public override IFindFluent<TDocument, TResult> As<TResult>(IBsonSerializer<TResult> resultSerializer) => throw new NotImplementedException();
#pragma warning disable CS0672 // Member overrides obsolete member
    public override Task<long> CountAsync(CancellationToken cancellationToken = default) => Task.FromResult<long>(collection.Count());
#pragma warning restore CS0672 // Member overrides obsolete member
#pragma warning disable CS0618 // Type or member is obsolete
    public override Task<long> CountDocumentsAsync(CancellationToken cancellationToken = default) => CountAsync(cancellationToken);
#pragma warning restore CS0618 // Type or member is obsolete
    public override IFindFluent<TDocument, TDocument> Limit(int? limit) => new InMemoryFluentFind<TDocument>(collection, limit);
    public override IFindFluent<TDocument, TNewProjection> Project<TNewProjection>(ProjectionDefinition<TDocument, TNewProjection> projection) => throw new NotImplementedException();
    public override IFindFluent<TDocument, TDocument> Skip(int? skip) => new InMemoryFluentFind<TDocument>(collection.Skip(skip ?? 0));
    public override IFindFluent<TDocument, TDocument> Sort(SortDefinition<TDocument> sort) => throw new NotImplementedException();

    public override Task<IAsyncCursor<TDocument>> ToCursorAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IAsyncCursor<TDocument>>(new Cursor(collection.GetEnumerator(), limit));

    sealed class Cursor(IEnumerator<TDocument> enumerator, int? limit) : IAsyncCursor<TDocument>
    {
        public void Dispose() { }
        public bool MoveNext(CancellationToken cancellationToken = default) => enumerator.MoveNext();
        public Task<bool> MoveNextAsync(CancellationToken cancellationToken = default) => Task.FromResult(enumerator.MoveNext());
        public IEnumerable<TDocument> Current
        {
            get
            {
                var result = new List<TDocument>();
                do
                {
                    if (enumerator.Current is null)
                    {
                        break;
                    }
                    result.Add(enumerator.Current);
                    if (limit.HasValue && result.Count == limit)
                    {
                        return result;
                    }
                } while (enumerator.MoveNext());
                return result;
            }
        }
    }
}
