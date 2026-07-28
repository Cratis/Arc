// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Emission.Naming;

/// <summary>
/// Decides whether a name can be written where it is going, and reports every one that cannot.
/// </summary>
/// <param name="naming">The <see cref="IScreenplayNaming"/> the name is written through.</param>
/// <param name="diagnostics">The <see cref="ScreenplayDiagnostics"/> anything left out is reported to.</param>
/// <remarks>
/// A line whose first word is a word the enclosing block reserves is read as that directive, so writing it produces
/// a document that does not compile - or worse, one that compiles as something else entirely. The name cannot be
/// escaped and cannot be changed without describing a member the application does not have, which leaves saying so
/// and moving on as the only honest answer.
/// </remarks>
public class NameAvailability(IScreenplayNaming naming, ScreenplayDiagnostics diagnostics)
{
    /// <summary>
    /// Gets whether a name can be written in a block, reporting it when it cannot.
    /// </summary>
    /// <param name="name">The name as the application declares it.</param>
    /// <param name="reserved">The <see cref="ReservedWords"/> of the block the name is written in.</param>
    /// <param name="declaringType">The type declaring the name, for use in diagnostics.</param>
    /// <param name="location">Where the declaring type lives, for use in diagnostics.</param>
    /// <returns>True when the name can be written, false when it was left out.</returns>
    public bool Allows(string name, ReservedWords reserved, string declaringType, string? location)
    {
        var written = naming.ToPropertyName(name);
        if (!reserved.Reserve(written))
        {
            return true;
        }

        diagnostics.Warning(
            ScreenplayDiagnosticCodes.NameReservedByGrammar,
            $"'{name}' on '{declaringType}' is written as '{written}', which a {reserved.Block} block reads as its own '{written}' directive rather than as a name, so it was left out",
            location);

        return false;
    }
}
