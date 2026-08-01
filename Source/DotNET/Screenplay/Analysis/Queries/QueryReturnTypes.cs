// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Types;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Analysis.Queries;

/// <summary>
/// Strips the wrappers a query's return type is dressed in.
/// </summary>
/// <remarks>
/// Awaiting, streaming, observing and querying are all how a result arrives, never what it is. A document says what
/// a query returns and whether there is one or many of it, so every wrapper is peeled away until that is all left.
/// </remarks>
public static class QueryReturnTypes
{
    static readonly string[] _wrappers =
    [
        "System.Threading.Tasks.Task`1",
        "System.Threading.Tasks.ValueTask`1",
        "System.Reactive.Subjects.ISubject`1",
        "Cratis.Arc.Queries.IQueryable`1",
        WellKnownTypeNames.ActionResultOfT
    ];

    static readonly string[] _sequences =
    [
        "System.Collections.Generic.IAsyncEnumerable`1",
        "System.Linq.IQueryable`1"
    ];

    static readonly string[] _queryables =
    [
        "Cratis.Arc.Queries.IQueryable`1",
        "System.Linq.IQueryable`1"
    ];

    /// <summary>
    /// Determines whether the host pages and sorts a query's result on the caller's behalf.
    /// </summary>
    /// <param name="type">The return type to check.</param>
    /// <returns>True when the query hands back a queryable.</returns>
    /// <remarks>
    /// Handing back a queryable rather than a materialized result is how a query tells Arc it may take the page and the
    /// order off the request and apply them - the caller never passes either as an argument. That is the whole of what
    /// makes a query paged, so it is what the return type is read for rather than any parameter.
    /// </remarks>
    public static bool IsPagedByTheHost(ITypeSymbol type)
    {
        var current = type;

        while (current is INamedTypeSymbol named && named.TypeArguments.Length == 1)
        {
            if (Matches(named, _queryables))
            {
                return true;
            }

            current = named.TypeArguments[0];
        }

        return false;
    }

    /// <summary>
    /// Determines whether a type says only how a result was transported, and nothing about what it is.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True when the type is a transport level result.</returns>
    /// <remarks>
    /// A controller method returning a bare action result has thrown away the read model at the type level, and no
    /// amount of reading recovers it. Every derived result - the ok, the json, the file - is caught by the base type
    /// and the interface rather than by listing them, because that list is the web framework's to grow, not ours.
    /// </remarks>
    public static bool IsTransport(ITypeSymbol type) =>
        type.Is(WellKnownTypeNames.ActionResultInterface) ||
        type.Is(WellKnownTypeNames.ActionResult) ||
        type.FindInterface(WellKnownTypeNames.ActionResultInterface) is not null ||
        type.FindBase(WellKnownTypeNames.ActionResult) is not null;

    /// <summary>
    /// Strips every wrapper from a return type.
    /// </summary>
    /// <param name="type">The type to strip.</param>
    /// <param name="collection">Set when a wrapper said the query returns many.</param>
    /// <returns>The type the query returns.</returns>
    public static ITypeSymbol Unwrap(ITypeSymbol type, ref bool collection)
    {
        var current = type;

        while (true)
        {
            if (current is INamedTypeSymbol named && Matches(named, _sequences))
            {
                collection = true;
                current = named.TypeArguments[0];
                continue;
            }

            if (current is INamedTypeSymbol wrapper && Matches(wrapper, _wrappers))
            {
                current = wrapper.TypeArguments[0];
                continue;
            }

            if (CollectionElements.ElementOf(current) is { } element)
            {
                collection = true;
                current = element;
                continue;
            }

            return current;
        }
    }

    /// <summary>
    /// Determines whether a type is one of a set of wrappers, by name and by the interfaces it implements.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <param name="names">The fully qualified metadata names to match.</param>
    /// <returns>True when the type matches.</returns>
    static bool Matches(INamedTypeSymbol type, string[] names)
    {
        if (type.TypeArguments.Length != 1)
        {
            return false;
        }

        var name = type.FullMetadataName();

        return names.Contains(name, StringComparer.Ordinal) ||
            type.AllInterfaces.Any(_ => _.TypeArguments.Length == 1 && names.Contains(_.FullMetadataName(), StringComparer.Ordinal));
    }
}
