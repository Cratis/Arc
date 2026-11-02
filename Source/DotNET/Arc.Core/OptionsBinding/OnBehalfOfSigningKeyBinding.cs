// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity;

namespace Cratis.Arc.OptionsBinding;

/// <summary>
/// The binding table of <see cref="OnBehalfOfSigningKey"/>.
/// </summary>
internal static class OnBehalfOfSigningKeyBinding
{
    /// <summary>
    /// Gets how <see cref="OnBehalfOfSigningKey"/> is bound.
    /// </summary>
    internal static readonly ObjectType Bound = new(typeof(OnBehalfOfSigningKey), static () => new OnBehalfOfSigningKey(), Members);

    static Member[] Members() =>
    [
        Member.Of<OnBehalfOfSigningKey, string>("KeyId", Leaves.String, static _ => _.KeyId, static (o, v) => o.KeyId = v),
        Member.Of<OnBehalfOfSigningKey, string>("PublicKeyPem", Leaves.String, static _ => _.PublicKeyPem, static (o, v) => o.PublicKeyPem = v)
    ];
}
