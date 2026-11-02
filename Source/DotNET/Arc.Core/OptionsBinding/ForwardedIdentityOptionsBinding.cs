// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity;

namespace Cratis.Arc.OptionsBinding;

/// <summary>
/// The binding table of <see cref="ForwardedIdentityOptions"/>.
/// </summary>
internal static class ForwardedIdentityOptionsBinding
{
    /// <summary>
    /// Gets how <see cref="ForwardedIdentityOptions"/> is bound.
    /// </summary>
    internal static readonly ObjectType Bound = new(typeof(ForwardedIdentityOptions), static () => new ForwardedIdentityOptions(), Members);

    static Member[] Members() =>
    [
        Member.Of<ForwardedIdentityOptions, bool>("Enabled", Leaves.Bool, static _ => _.Enabled, static (o, v) => o.Enabled = v),
        Member.Of<ForwardedIdentityOptions, string>("Audience", Leaves.String, static _ => _.Audience, static (o, v) => o.Audience = v),
        Member.Of<ForwardedIdentityOptions, IList<ForwardedIdentityKey>>("Keys", new ListType<ForwardedIdentityKey>(ForwardedIdentityKeyBinding.Bound), static _ => _.Keys, static (o, v) => o.Keys = v)
    ];
}
