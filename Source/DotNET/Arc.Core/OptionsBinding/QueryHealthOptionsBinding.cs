// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;

namespace Cratis.Arc.OptionsBinding;

/// <summary>
/// The binding table of <see cref="QueryHealthOptions"/>.
/// </summary>
internal static class QueryHealthOptionsBinding
{
    /// <summary>
    /// Gets how <see cref="QueryHealthOptions"/> is bound.
    /// </summary>
    internal static readonly ObjectType Bound = new(typeof(QueryHealthOptions), static () => new QueryHealthOptions(), Members);

    static Member[] Members() =>
    [
        Member.Of<QueryHealthOptions, bool>("RequireAuthentication", Leaves.Bool, static _ => _.RequireAuthentication, static (o, v) => o.RequireAuthentication = v),
        Member.Of<QueryHealthOptions, string?>("Roles", Leaves.String, static _ => _.Roles, static (o, v) => o.Roles = v)
    ];
}
