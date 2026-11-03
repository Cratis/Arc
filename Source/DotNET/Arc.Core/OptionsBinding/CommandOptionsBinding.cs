// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Validation;

namespace Cratis.Arc.OptionsBinding;

/// <summary>
/// The binding table of <see cref="CommandOptions"/>.
/// </summary>
internal static class CommandOptionsBinding
{
    /// <summary>
    /// Gets how <see cref="CommandOptions"/> is bound.
    /// </summary>
    internal static readonly ObjectType Bound = new(typeof(CommandOptions), static () => new CommandOptions(), Members);

    static Member[] Members() =>
    [
        Member.Of<CommandOptions, ValidationResultSeverity>("MaximumCallerAllowedSeverity", Leaves.ValidationResultSeverity, static _ => _.MaximumCallerAllowedSeverity, static (o, v) => o.MaximumCallerAllowedSeverity = v),
        Member.Of<CommandOptions, bool>("IgnoreCallerAllowedSeverity", Leaves.Bool, static _ => _.IgnoreCallerAllowedSeverity, static (o, v) => o.IgnoreCallerAllowedSeverity = v)
    ];
}
