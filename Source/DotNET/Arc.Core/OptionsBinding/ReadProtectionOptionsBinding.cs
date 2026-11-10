// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.OptionsBinding;

/// <summary>
/// The binding table of <see cref="ReadProtectionOptions"/>.
/// </summary>
internal static class ReadProtectionOptionsBinding
{
    /// <summary>
    /// Gets how <see cref="ReadProtectionOptions"/> is bound.
    /// </summary>
    internal static readonly ObjectType Bound = new(typeof(ReadProtectionOptions), static () => new ReadProtectionOptions(), Members);

    static Member[] Members() =>
    [
        Member.Of<ReadProtectionOptions, string?>("MaximumMode", Leaves.String, static _ => _.MaximumMode, static (o, v) => o.MaximumMode = v),
        Member.Of<ReadProtectionOptions, string?>("RequireGeneratedMetadata", Leaves.String, static _ => _.RequireGeneratedMetadata, static (o, v) => o.RequireGeneratedMetadata = v),
        Member.Of<ReadProtectionOptions, string?>("StartupAdmissionCheck", Leaves.String, static _ => _.StartupAdmissionCheck, static (o, v) => o.StartupAdmissionCheck = v)
    ];
}
