// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Analysis.Specifications;

/// <summary>
/// Keeps specification events whole rather than stating a partial assertion as an exact fact.
/// </summary>
static class SpecificationEventCompleteness
{
    /// <summary>
    /// Determines whether a state gives every property of its event a value.
    /// </summary>
    /// <param name="state">The state.</param>
    /// <param name="event">The event it is of.</param>
    /// <param name="draft">The scenario collected so far.</param>
    /// <returns>True when every value is stated.</returns>
    /// <remarks>
    /// A specification states an event whole. The pinned Screenplay binder requires optional values too, so
    /// an assertion of only part of an event cannot be written as an exact event expectation.
    /// </remarks>
    public static bool StatesEveryValue(SpecificationStateModel state, ITypeSymbol @event, SpecificationDraft draft)
    {
        var stated = state.Values.Select(_ => _.Property).ToHashSet(StringComparer.Ordinal);
        var missing = @event.DeclaredProperties().Where(property => !stated.Contains(property.Name)).Select(_ => _.Name).ToList();
        if (missing.Count == 0)
        {
            return true;
        }

        draft.CannotRead($"it does not state every value of '{@event.Name}': properties '{string.Join(", ", missing)}' are missing, and a specification states an event whole");
        return false;
    }
}
