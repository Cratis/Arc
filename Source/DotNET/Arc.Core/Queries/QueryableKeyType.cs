// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;

namespace Cratis.Arc.Queries;

/// <summary>
/// Represents the default comparers of one key type, such as an id type or the type of a property to sort on.
/// </summary>
internal abstract class QueryableKeyType
{
    /// <summary>
    /// Gets the default <see cref="IEqualityComparer"/> of the key type.
    /// </summary>
    public abstract IEqualityComparer EqualityComparer { get; }

    /// <summary>
    /// Gets the default <see cref="IComparer"/> of the key type.
    /// </summary>
    public abstract IComparer Comparer { get; }
}
