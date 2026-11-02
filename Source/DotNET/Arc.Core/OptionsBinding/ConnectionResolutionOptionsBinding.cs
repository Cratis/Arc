// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;

namespace Cratis.Arc.OptionsBinding;

/// <summary>
/// The binding table of <see cref="ConnectionResolutionOptions"/>.
/// </summary>
internal static class ConnectionResolutionOptionsBinding
{
    /// <summary>
    /// Gets how <see cref="ConnectionResolutionOptions"/> is bound.
    /// </summary>
    internal static readonly ObjectType Bound = new(typeof(ConnectionResolutionOptions), static () => new ConnectionResolutionOptions(), Members);

    static Member[] Members() =>
    [
        Member.Of<ConnectionResolutionOptions, ConnectionResolverOptions>("Connection", ConnectionResolverOptionsBinding.Bound, static _ => _.Connection, static (o, v) => o.Connection = v),
        Member.Of<ConnectionResolutionOptions, ConnectionResolverOptions>("Identity", ConnectionResolverOptionsBinding.Bound, static _ => _.Identity, static (o, v) => o.Identity = v),
        Member.Of<ConnectionResolutionOptions, TimeSpan>("CacheDuration", Leaves.TimeSpan, static _ => _.CacheDuration, static (o, v) => o.CacheDuration = v),
        Member.Of<ConnectionResolutionOptions, bool>("IsConfigured", Leaves.Bool, static _ => _.IsConfigured)
    ];
}
