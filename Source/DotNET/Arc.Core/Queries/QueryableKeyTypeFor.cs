// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;

namespace Cratis.Arc.Queries;

/// <summary>
/// Represents the <see cref="QueryableKeyType"/> for a specific key type.
/// </summary>
/// <typeparam name="TKey">The key type.</typeparam>
internal sealed class QueryableKeyTypeFor<TKey> : QueryableKeyType
{
    /// <inheritdoc/>
    public override IEqualityComparer EqualityComparer => EqualityComparer<TKey>.Default;

    /// <inheritdoc/>
    public override IComparer Comparer => Comparer<TKey>.Default;
}
