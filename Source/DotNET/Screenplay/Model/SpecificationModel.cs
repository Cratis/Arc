// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Model;

/// <summary>
/// Represents one scenario a slice is specified by - what had happened, the command that was issued and what
/// followed.
/// </summary>
/// <param name="Name">The name of the specification.</param>
/// <param name="Given">What had already happened when the command was issued.</param>
/// <param name="When">The command that was issued, or <see langword="null"/> when no command was.</param>
/// <param name="Then">The events and read model states that followed.</param>
/// <param name="Errors">The rejections that followed, each named by the reason the source gives for it.</param>
/// <remarks>
/// A rejection the source names no reason for carries an empty one. The scenario is named after the words the
/// source itself uses for it, so what the rejection was about is already said by the name and inventing a sentence
/// to repeat it would be describing an application nobody wrote.
/// <para>
/// A scenario about a read model issues no command - the events are the whole of what happened, and what followed is
/// the model they built. So the command is optional, which is what the language says too.
/// </para>
/// </remarks>
public record SpecificationModel(
    string Name,
    IEnumerable<SpecificationStateModel> Given,
    SpecificationStateModel? When,
    IEnumerable<SpecificationStateModel> Then,
    IEnumerable<string> Errors)
{
    /// <summary>
    /// Gets the name of the reactor the scenario delivers its appended event to, when it is a reactor scenario.
    /// </summary>
    /// <remarks>
    /// What follows the append of such a scenario is what that reactor's declarative reaction appends, and the
    /// document only keeps the scenario while that reaction is the one thing reacting to it.
    /// </remarks>
    public string? Reactor { get; init; }

    /// <summary>
    /// Gets whether the source asserts the command response.
    /// </summary>
    /// <remarks>
    /// When <see cref="Returns"/> is <see langword="null"/>, the assertion was not recovered.
    /// </remarks>
    public bool AssertsResponse { get; init; }

    /// <summary>
    /// Gets the command response the source asserts with concrete values, or <see langword="null"/> when none was recovered.
    /// </summary>
    public SpecificationReturnModel? Returns { get; init; }
}
