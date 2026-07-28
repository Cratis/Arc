// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.Analysis;

/// <summary>
/// Infers the Event Modeling kind of a slice from what it contains.
/// </summary>
/// <remarks>
/// A reactor that turns an event into further events or commands is adapting one part of the model into another, so
/// it yields <see cref="SliceKind.Translate"/> rather than <see cref="SliceKind.Automation"/>. A reactor that only
/// causes an effect outside the system is an automation, and it decides the kind even when the slice also holds a
/// command - the reaction is what the slice is about.
/// </remarks>
public static class SliceKindInference
{
    /// <summary>
    /// Infers the kind of a slice, first match wins.
    /// </summary>
    /// <param name="commands">The commands the slice declares.</param>
    /// <param name="reactors">The reactors the slice declares.</param>
    /// <returns>The inferred <see cref="SliceKind"/>.</returns>
    public static SliceKind Infer(IEnumerable<CommandModel> commands, IEnumerable<ReactorModel> reactors)
    {
        var declaredReactors = reactors as IReadOnlyCollection<ReactorModel> ?? [.. reactors];

        if (declaredReactors.Any(_ => _.IsTranslating))
        {
            return SliceKind.Translate;
        }

        if (declaredReactors.Count > 0)
        {
            return SliceKind.Automation;
        }

        return commands.Any() ? SliceKind.StateChange : SliceKind.StateView;
    }
}
