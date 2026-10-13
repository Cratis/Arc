// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

/// <summary>
/// Provides compilable model-bound queries for equality filter recovery.
/// </summary>
internal static class QueryFilterSources
{
    /// <summary>
    /// Declares a query with the supplied signature and implementation.
    /// </summary>
    /// <param name="signature">The query parameters.</param>
    /// <param name="body">The query body.</param>
    /// <param name="returnType">The collection or observable return type.</param>
    /// <param name="isAsync">Whether the method awaits its result.</param>
    /// <returns>The compilable source.</returns>
    internal static string Create(string signature, string body, string returnType = "IEnumerable<Item>", bool isAsync = false) => $$"""
        using System;
        using System.Collections.Generic;
        using System.Linq;
        using System.Reactive.Subjects;
        using System.Threading.Tasks;
        using Cratis.Arc.Queries.ModelBound;
        using Cratis.Concepts;
        using Microsoft.EntityFrameworkCore;
        using MongoDB.Driver;
        namespace Library.Items.Browsing
        {
            public enum Status { Open, Closed }
            public record Category(string Value) : ConceptAs<string>(Value);
            [ReadModel]
            public record Item(Guid Id, Status Status, Category Category, string? Name, int Count)
            {
                public static Item? ById(Guid id) => null;
                public static {{(isAsync ? "async " : string.Empty)}}{{returnType}} Filtered({{signature}}) {{body}}
            }
        }
        // The reader recognizes the resolved namespace and method shape, not a driver assembly identity.
        namespace MongoDB.Driver
        {
            public interface IMongoCollection<T> {}
            public interface IFindFluent<TDocument, TProjection> {}
            public static class IMongoCollectionExtensions
            {
                public static IFindFluent<T, T> Find<T>(this IMongoCollection<T> collection, System.Linq.Expressions.Expression<Func<T, bool>> predicate) => default!;
            }
            public static class IFindFluentExtensions
            {
                public static List<TProjection> ToList<TDocument, TProjection>(this IFindFluent<TDocument, TProjection> query) => [];
            }
            public static class MongoCollectionExtensions
            {
                public static ISubject<IEnumerable<T>> Observe<T>(this IMongoCollection<T> collection, System.Linq.Expressions.Expression<Func<T, bool>>? filter) => default!;
                public static ISubject<IEnumerable<T>> Observe<T>(this IMongoCollection<T> collection, System.Linq.Expressions.Expression<Func<T, bool>>? filter, bool ignoreQueryContext) => default!;
                public static ISubject<T?> ObserveSingle<T>(this IMongoCollection<T> collection, System.Linq.Expressions.Expression<Func<T, bool>>? filter) where T : class => default!;
            }
            public static class OtherObservations
            {
                public static ISubject<IEnumerable<T>> Observe<T>(IMongoCollection<T> collection, System.Linq.Expressions.Expression<Func<T, bool>> filter) => default!;
            }
        }
        namespace Microsoft.EntityFrameworkCore
        {
            public abstract class DbSet<T> where T : class {}
            public static class DbSetObserveExtensions
            {
                public static ISubject<IEnumerable<T>> Observe<T>(this DbSet<T> items, System.Linq.Expressions.Expression<Func<T, bool>>? filter = null, Func<DbSet<T>, IQueryable<T>>? configure = null) where T : class => default!;
                public static ISubject<IEnumerable<T>> Observe<T>(this DbSet<T> items, System.Linq.Expressions.Expression<Func<T, bool>>? filter, bool ignoreQueryContext, Func<DbSet<T>, IQueryable<T>>? configure = null) where T : class => default!;
            }
            public static class EntityFrameworkQueryableExtensions
            {
                public static Task<List<T>> ToListAsync<T>(this IQueryable<T> query) => Task.FromResult(new List<T>());
                public static Task<T[]> ToArrayAsync<T>(this IQueryable<T> query) => Task.FromResult(Array.Empty<T>());
                public static IAsyncEnumerable<T> AsAsyncEnumerable<T>(this IQueryable<T> query) => default!;
            }
            public static class OtherTerminals
            {
                public static Task<List<T>> ToListAsync<T>(IQueryable<T> query) => Task.FromResult(new List<T>());
            }
        }
        namespace System.Reactive.Subjects
        {
            public interface ISubject<T> {}
        }
        """;
}
