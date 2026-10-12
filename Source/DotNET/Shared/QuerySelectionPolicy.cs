// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.ModelBound;

/// <summary>
/// The additive selection policy shared by reflection and Roslyn adapters.
/// </summary>
internal static class QuerySelectionPolicy
{
    /// <summary>
    /// The full metadata name of the owner declaration.
    /// </summary>
    internal const string ReadModelName = "Cratis.Arc.Queries.ModelBound.ReadModelAttribute";

    /// <summary>
    /// The full metadata name of the Arc method marker (not Chronicle's gRPC marker).
    /// </summary>
    internal const string QueryName = "Cratis.Arc.Queries.ModelBound.QueryAttribute";

    /// <summary>
    /// Applies selection without changing the legacy candidate or return rules.
    /// </summary>
    /// <param name="requireAttribute">Whether the nearest read-model declaration opts in.</param>
    /// <param name="marked">Whether the method carries the Arc marker.</param>
    /// <param name="eligible">Whether the method has the explicit query shape.</param>
    /// <returns>Whether selection permits the method.</returns>
    internal static bool IsSelected(bool requireAttribute, Func<bool> marked, Func<bool> eligible) =>
        !requireAttribute || (marked() && eligible());
}
