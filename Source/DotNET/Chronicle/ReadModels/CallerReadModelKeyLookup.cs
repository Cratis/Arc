// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Arc.Chronicle.ReadModels;

/// <summary>
/// Validates caller-derived keys without exposing their values in refusal results.
/// </summary>
internal static class CallerReadModelKeyLookup
{
    /// <summary>Converts and validates a caller-derived key before lookup.</summary>
    /// <param name="lookup">The provider value and requested model.</param>
    /// <param name="traversal">The protected key traversal.</param>
    /// <param name="protect">Whether the raw key must be validated for a protected read.</param>
    /// <param name="convert">The key conversion.</param>
    /// <returns>The validated read-model key.</returns>
    /// <exception cref="ReadModelKeyResolutionDenied">The provider key is invalid.</exception>
    internal static ReadModelKey Resolve((object Value, Type ModelType) lookup, ProtectedReadModelKeyTraversal? traversal, bool protect, Func<object, ReadModelKey> convert)
    {
        var key = Convert(lookup.Value, convert);
        if (!ProtectedReadModelKeyTraversal.IsValidWireText(key.Value) || key.Value == EventSourceId.Unspecified.Value)
        {
            throw new ReadModelKeyResolutionDenied(ReadModelKeyRefusal.RefusedKeyText);
        }

        if (protect) ValidateRawKey(lookup.Value, lookup.ModelType, traversal);
        return key;
    }

    static ReadModelKey Convert(object value, Func<object, ReadModelKey> convert)
    {
        try
        {
            return convert(value);
        }
        catch (Exception)
        {
            throw new ReadModelKeyResolutionDenied(ReadModelKeyRefusal.RefusedKeyText);
        }
    }

    static void ValidateRawKey(object value, Type readModelType, ProtectedReadModelKeyTraversal? traversal)
    {
        try
        {
            ProtectedReadModelKeys.ValidateRawKey(traversal, readModelType, value, string.Empty);
        }
        catch (ProtectedReadModelDoesNotExist)
        {
            throw new ReadModelKeyResolutionDenied(ReadModelKeyRefusal.RefusedKeyText);
        }
    }
}
