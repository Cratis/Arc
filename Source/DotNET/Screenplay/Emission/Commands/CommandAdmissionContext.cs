// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.Emission.Commands;

/// <summary>
/// The command that is being admitted, and what is known about it so far.
/// </summary>
/// <param name="Command">The command being admitted.</param>
/// <param name="Scope">What every command of the application is admitted against.</param>
/// <param name="Location">Where the command's slice lives, for use in diagnostics.</param>
/// <param name="Authoring">The authoring the command declares.</param>
/// <param name="Blocked">The generated values that were not admitted.</param>
/// <param name="Capped">Whether the executable model version cap requires legacy values.</param>
internal sealed record CommandAdmissionContext(
    CommandModel Command,
    CommandAdmissionScope Scope,
    string Location,
    CommandAuthoringModel Authoring,
    HashSet<string> Blocked,
    bool Capped)
{
    /// <summary>
    /// Gets a value indicating whether the command keeps its legacy productions or handler reference.
    /// </summary>
    public bool Legacy { get; init; }
}
