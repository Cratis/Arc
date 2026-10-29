// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.Analysis;

/// <summary>
/// Infers the Event Modeling kind of a slice from what it contains.
/// </summary>
/// <remarks>
/// A reactor implies <see cref="SliceKind.Automation"/>, including when it produces events or invokes commands.
/// These effects do not prove translation from external vocabulary. The reaction decides the kind even when the
/// slice also holds a command.
/// </remarks>
public static class SliceKindInference
{
    /// <summary>
    /// Infers the kind of a slice, first match wins.
    /// </summary>
    /// <param name="commands">The commands the slice declares.</param>
    /// <param name="reactors">The reactors the slice declares.</param>
    /// <param name="hasAggregateRoot">Whether the slice declares an aggregate root.</param>
    /// <returns>The inferred <see cref="SliceKind"/>.</returns>
    /// <remarks>
    /// An aggregate root governs a change to the system just as a command does, so a slice holding one is a state
    /// change whether or not a command sits beside it. Calling such a slice a state view would say the opposite of
    /// what it is.
    /// </remarks>
    public static SliceKind Infer(
        IEnumerable<CommandModel> commands,
        IEnumerable<ReactorModel> reactors,
        bool hasAggregateRoot = false)
    {
        var declaredReactors = reactors as IReadOnlyCollection<ReactorModel> ?? [.. reactors];

        if (declaredReactors.Count > 0)
        {
            return SliceKind.Automation;
        }

        return commands.Any() || hasAggregateRoot ? SliceKind.StateChange : SliceKind.StateView;
    }

    /// <summary>
    /// Infers the kind of a slice that several projects each declare a part of, from the kind of each part.
    /// </summary>
    /// <param name="kinds">The kind each part was inferred to be.</param>
    /// <returns>The inferred <see cref="SliceKind"/>.</returns>
    /// <remarks>
    /// Explicit translations retain precedence. Otherwise a part containing a reactor makes the combined slice an
    /// automation, and a command or aggregate root makes it a state change only when no part contains a reactor.
    /// </remarks>
    public static SliceKind Combine(IEnumerable<SliceKind> kinds)
    {
        var declared = kinds as IReadOnlyCollection<SliceKind> ?? [.. kinds];

        if (declared.Contains(SliceKind.Translate))
        {
            return SliceKind.Translate;
        }

        if (declared.Contains(SliceKind.Automation))
        {
            return SliceKind.Automation;
        }

        return declared.Contains(SliceKind.StateChange) ? SliceKind.StateChange : SliceKind.StateView;
    }
}
