// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;

namespace Cratis.Arc.OptionsBinding;

/// <summary>
/// The binding table of <see cref="QueryOptions"/>.
/// </summary>
internal static class QueryOptionsBinding
{
    /// <summary>
    /// Gets how <see cref="QueryOptions"/> is bound.
    /// </summary>
    internal static readonly ObjectType Bound = new(typeof(QueryOptions), static () => new QueryOptions(), Members);

    static Member[] Members() =>
    [
        Member.Of<QueryOptions, TimeSpan>("KeepAliveInterval", Leaves.TimeSpan, static _ => _.KeepAliveInterval, static (o, v) => o.KeepAliveInterval = v)
    ];
}
