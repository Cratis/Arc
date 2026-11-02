// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity;

namespace Cratis.Arc.OptionsBinding;

/// <summary>
/// The binding table of <see cref="OnBehalfOfOptions"/>.
/// </summary>
internal static class OnBehalfOfOptionsBinding
{
    /// <summary>
    /// Gets how <see cref="OnBehalfOfOptions"/> is bound.
    /// </summary>
    internal static readonly ObjectType Bound = new(typeof(OnBehalfOfOptions), static () => new OnBehalfOfOptions(), Members);

    static Member[] Members() =>
    [
        Member.Of<OnBehalfOfOptions, bool>("Enabled", Leaves.Bool, static _ => _.Enabled, static (o, v) => o.Enabled = v),
        Member.Of<OnBehalfOfOptions, string>("Issuer", Leaves.String, static _ => _.Issuer, static (o, v) => o.Issuer = v),
        Member.Of<OnBehalfOfOptions, string>("Audience", Leaves.String, static _ => _.Audience, static (o, v) => o.Audience = v),
        Member.Of<OnBehalfOfOptions, string>("Service", Leaves.String, static _ => _.Service, static (o, v) => o.Service = v),
        Member.Of<OnBehalfOfOptions, IList<OnBehalfOfSigningKey>>("SigningKeys", new ListType<OnBehalfOfSigningKey>(OnBehalfOfSigningKeyBinding.Bound), static _ => _.SigningKeys, static (o, v) => o.SigningKeys = v),
        Member.Of<OnBehalfOfOptions, TimeSpan>("ClockSkew", Leaves.TimeSpan, static _ => _.ClockSkew, static (o, v) => o.ClockSkew = v)
    ];
}
