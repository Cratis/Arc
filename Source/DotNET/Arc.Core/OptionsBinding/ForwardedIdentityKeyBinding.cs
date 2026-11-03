// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity;

namespace Cratis.Arc.OptionsBinding;

/// <summary>
/// The binding table of <see cref="ForwardedIdentityKey"/>.
/// </summary>
internal static class ForwardedIdentityKeyBinding
{
    /// <summary>
    /// Gets how <see cref="ForwardedIdentityKey"/> is bound.
    /// </summary>
    internal static readonly ObjectType Bound = new(typeof(ForwardedIdentityKey), static () => new ForwardedIdentityKey(), Members);

    static Member[] Members() =>
    [
        Member.Of<ForwardedIdentityKey, string>("KeyId", Leaves.String, static _ => _.KeyId, static (o, v) => o.KeyId = v),
        Member.Of<ForwardedIdentityKey, string>("SecretBase64", Leaves.String, static _ => _.SecretBase64, static (o, v) => o.SecretBase64 = v)
    ];
}
