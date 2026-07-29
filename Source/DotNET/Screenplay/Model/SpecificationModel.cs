// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Model;

/// <summary>
/// Represents one scenario a slice is specified by - what had happened, the command that was issued and what
/// followed.
/// </summary>
/// <param name="Name">The name of the specification.</param>
/// <param name="Given">What had already happened when the command was issued.</param>
/// <param name="When">The command that was issued.</param>
/// <param name="Then">The events and read model states that followed.</param>
/// <param name="Errors">The rejections that followed, each named by the reason the source gives for it.</param>
/// <remarks>
/// A rejection the source names no reason for carries an empty one. The scenario is named after the words the
/// source itself uses for it, so what the rejection was about is already said by the name and inventing a sentence
/// to repeat it would be describing an application nobody wrote.
/// </remarks>
public record SpecificationModel(
    string Name,
    IEnumerable<SpecificationStateModel> Given,
    SpecificationStateModel When,
    IEnumerable<SpecificationStateModel> Then,
    IEnumerable<string> Errors);
