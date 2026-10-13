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
    /// <returns>The compilable source.</returns>
    internal static string Create(string signature, string body) => $$"""
        using System;
        using System.Collections.Generic;
        using System.Linq;
        using Cratis.Arc.Queries.ModelBound;
        using Cratis.Concepts;
        using MongoDB.Driver;
        namespace Library.Items.Browsing
        {
            public enum Status { Open, Closed }
            public record Category(string Value) : ConceptAs<string>(Value);
            [ReadModel]
            public record Item(Guid Id, Status Status, Category Category, string? Name, int Count)
            {
                public static Item? ById(Guid id) => null;
                public static IEnumerable<Item> Filtered({{signature}}) {{body}}
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
        }
        """;
}
