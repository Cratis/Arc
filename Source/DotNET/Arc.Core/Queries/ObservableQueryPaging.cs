// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;

namespace Cratis.Arc.Queries;

/// <summary>
/// Identifies observable collection results that can report paging metadata.
/// </summary>
internal static class ObservableQueryPaging
{
    /// <summary>
    /// Determines whether an observable result type represents a collection rather than a single value.
    /// </summary>
    /// <param name="elementType">The declared or runtime result type, or null for an absent value.</param>
    /// <returns>True for enumerable types other than strings.</returns>
    internal static bool ReportsPaging(Type? elementType) =>
        elementType is not null && elementType != typeof(string) && typeof(IEnumerable).IsAssignableFrom(elementType);
}
