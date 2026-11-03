// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.OptionsBinding;

/// <summary>
/// The binding table of <see cref="HostingOptions"/>.
/// </summary>
internal static class HostingOptionsBinding
{
    /// <summary>
    /// Gets how <see cref="HostingOptions"/> is bound.
    /// </summary>
    internal static readonly ObjectType Bound = new(typeof(HostingOptions), static () => new HostingOptions(), Members);

    static Member[] Members() =>
    [
        Member.Of<HostingOptions, string>("ApplicationUrl", Leaves.String, static _ => _.ApplicationUrl, static (o, v) => o.ApplicationUrl = v)
    ];
}
