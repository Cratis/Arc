// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Introspection;

namespace Cratis.Arc.OptionsBinding;

/// <summary>
/// The binding table of <see cref="IntrospectionOptions"/>.
/// </summary>
internal static class IntrospectionOptionsBinding
{
    /// <summary>
    /// Gets how <see cref="IntrospectionOptions"/> is bound.
    /// </summary>
    internal static readonly ObjectType Bound = new(typeof(IntrospectionOptions), static () => new IntrospectionOptions(), Members);

    static Member[] Members() =>
    [
        Member.Of<IntrospectionOptions, bool>("Enabled", Leaves.Bool, static _ => _.Enabled, static (o, v) => o.Enabled = v),
        Member.Of<IntrospectionOptions, bool>("IdentityDiscovery", Leaves.Bool, static _ => _.IdentityDiscovery, static (o, v) => o.IdentityDiscovery = v),
        Member.Of<IntrospectionOptions, bool>("RequireAuthentication", Leaves.Bool, static _ => _.RequireAuthentication, static (o, v) => o.RequireAuthentication = v),
        Member.Of<IntrospectionOptions, string?>("Roles", Leaves.String, static _ => _.Roles, static (o, v) => o.Roles = v),
#pragma warning disable CS0618 // The obsolete setting is still bound from configuration, as the reflection binder does.
        Member.Of<IntrospectionOptions, bool>("TrustForwardedIdentityHeaders", Leaves.Bool, static _ => _.TrustForwardedIdentityHeaders, static (o, v) => o.TrustForwardedIdentityHeaders = v)
#pragma warning restore CS0618
    ];
}
