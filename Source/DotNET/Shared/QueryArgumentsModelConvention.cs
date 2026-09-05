// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.Queries;

/// <summary>
/// The convention that identifies the type modelling a query's argument set.
/// </summary>
/// <remarks>
/// This file is compiled into both the framework and the proxy generator from one source, rather than referenced:
/// the generator loads target assemblies through a <c>MetadataLoadContext</c> and deliberately takes no dependency
/// on the framework, so sharing has to happen at the source level.
/// <para>
/// It is shared at all because the client and the server must resolve the same type for the same query. Two
/// implementations of one convention drift, and when they do the client validates rules the server does not — or the
/// reverse, which is worse. Both sides call <see cref="Resolve"/> and nothing else.
/// </para>
/// </remarks>
static class QueryArgumentsModelConvention
{
    /// <summary>
    /// Gets the type names that can model a query's argument set, most specific first.
    /// </summary>
    /// <param name="readModelName">The name of the read model owning the query.</param>
    /// <param name="queryName">The name of the query.</param>
    /// <returns>The candidate names, in the order they should be tried.</returns>
    /// <remarks>
    /// The read-model-prefixed form exists to disambiguate: two read models can each expose a query of the same
    /// name, which makes the bare form ambiguous. It is therefore tried first.
    /// </remarks>
    public static IEnumerable<string> CandidateNamesFor(string readModelName, string queryName) =>
    [
        $"{readModelName}{queryName}Parameters",
        $"{queryName}Parameters"
    ];

    /// <summary>
    /// Determines whether a candidate type has a property of matching name and type for every query argument.
    /// </summary>
    /// <param name="candidate">The candidate <see cref="Type"/>.</param>
    /// <param name="arguments">The query's arguments, excluding injected dependencies.</param>
    /// <returns>True when every argument is covered; otherwise false.</returns>
    /// <remarks>
    /// The type is matched as well as the name: a name-only match would accept a model whose members cannot hold the
    /// arguments, which then fails while being materialized rather than simply not being the argument model.
    /// </remarks>
    public static bool CoversEveryArgument(Type candidate, IReadOnlyList<QueryArgumentDescriptor> arguments)
    {
        var properties = candidate.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        return arguments.All(argument =>
            properties.Any(property =>
                property.Name.Equals(argument.Name, StringComparison.OrdinalIgnoreCase) &&
                property.PropertyType == argument.Type));
    }

    /// <summary>
    /// Resolves the type modelling a query's argument set, or null when there is none.
    /// </summary>
    /// <param name="readModelName">The name of the read model owning the query.</param>
    /// <param name="queryName">The name of the query.</param>
    /// <param name="arguments">The query's arguments, excluding injected dependencies.</param>
    /// <param name="candidates">The types to consider, typically every type in the read model's assembly.</param>
    /// <returns>The resolved <see cref="Type"/>, or null.</returns>
    /// <remarks>
    /// Every type carrying a candidate name is considered rather than only the first found: two unrelated types can
    /// share a simple name, and settling for whichever the runtime reports first would resolve differently from run
    /// to run and silently skip validation whenever the wrong one won.
    /// <para>
    /// A query with no arguments resolves nothing. "Covers every argument" is vacuously true for an empty set, which
    /// would otherwise let a parameterless query bind any same-named type that happens to exist.
    /// </para>
    /// </remarks>
    public static Type? Resolve(
        string readModelName,
        string queryName,
        IReadOnlyList<QueryArgumentDescriptor> arguments,
        IEnumerable<Type> candidates)
    {
        if (arguments.Count == 0)
        {
            return null;
        }

        var allCandidates = candidates as Type[] ?? [.. candidates];

        foreach (var candidateName in CandidateNamesFor(readModelName, queryName))
        {
            var match = allCandidates.FirstOrDefault(type =>
                type.Name.Equals(candidateName, StringComparison.OrdinalIgnoreCase) &&
                CoversEveryArgument(type, arguments));

            if (match is not null)
            {
                return match;
            }
        }

        return null;
    }
}
