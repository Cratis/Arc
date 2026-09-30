// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Types;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Analysis.Specifications;

/// <summary>
/// Decides what a scenario can state for a constant it gives a property.
/// </summary>
/// <remarks>
/// A document checks every value a scenario states against the type of its property: a member of an enumeration by
/// the name it is declared with, and an absent value only where the property may be absent or holds a single value.
/// A constant the document would reject - a number no member is declared with, or <see langword="null"/> handed to a
/// property carrying a record, a list or an enumeration - has no form worth writing. What the scenario does about it
/// is the reader's decision: a scenario missing a value it issues a command with, or started from, is a different
/// example than the one written, so the reader leaves the whole scenario out and says so.
/// </remarks>
static class StatableValues
{
    /// <summary>
    /// Gets the value a scenario states for a constant given a property.
    /// </summary>
    /// <param name="propertyType">The type of the property.</param>
    /// <param name="constant">The value the compiler handed over, which for an enumeration is the number behind a member.</param>
    /// <param name="value">The value to state, which for an enumeration is the member the constant names.</param>
    /// <returns><see langword="false"/> when the document cannot hold the constant for the property.</returns>
    public static bool TryState(ITypeSymbol propertyType, object? constant, out object? value)
    {
        value = constant;
        var optional = false;
        var collection = false;
        var underlying = UnderlyingTypes.Of(propertyType, ref optional, ref collection);
        if (constant is null)
        {
            return optional || (!collection && !EnumConstants.IsEnumeration(underlying) && !CarriedTypes.IsRecord(underlying));
        }

        if (collection || !EnumConstants.IsEnumeration(underlying))
        {
            return true;
        }

        if (!EnumConstants.TryResolve(underlying, constant, out var member))
        {
            return false;
        }

        value = member;
        return true;
    }

    /// <summary>
    /// Says which property a constant cannot be stated for, with what value, and why.
    /// </summary>
    /// <param name="owner">The type declaring the property.</param>
    /// <param name="property">The property the constant is given.</param>
    /// <param name="constant">The value the compiler handed over.</param>
    /// <returns>The property, the value and the reason it cannot be stated, to follow "states".</returns>
    public static string WhyNot(ITypeSymbol owner, IPropertySymbol property, object? constant)
    {
        var optional = false;
        var collection = false;
        var underlying = UnderlyingTypes.Of(property.Type, ref optional, ref collection);

        return constant is null
            ? $"'{owner.Name}.{property.Name}' as null, which a required property of type '{underlying.Name}' cannot hold"
            : $"'{owner.Name}.{property.Name}' as {constant}, which no member of the enumeration '{underlying.Name}' is declared with";
    }
}
