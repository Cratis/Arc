// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Emission.Naming;

/// <summary>
/// Decides whether a name can be written where it is going, and records names Screenplay must escape.
/// </summary>
/// <param name="naming">The <see cref="IScreenplayNaming"/> the name is written through.</param>
/// <param name="diagnostics">The <see cref="ScreenplayDiagnostics"/> escaped names are reported to.</param>
/// <remarks>
/// Screenplay supports escaping a property, mapping, enumeration value, or projection mapping whose name is a word the
/// enclosing block reserves. The printer adds that escape when the syntax tree needs it, so generation preserves the
/// application's actual names instead of leaving them out.
/// </remarks>
public class NameAvailability(IScreenplayNaming naming, ScreenplayDiagnostics diagnostics)
{
    /// <summary>
    /// Gets whether a name can be written in a block, reporting it when the printer must escape it.
    /// </summary>
    /// <param name="name">The name as the application declares it.</param>
    /// <param name="reserved">The <see cref="ReservedWords"/> of the block the name is written in.</param>
    /// <param name="declaringType">The type declaring the name, for use in diagnostics.</param>
    /// <param name="location">Where the declaring type lives, for use in diagnostics.</param>
    /// <returns>True, because Screenplay can escape every reserved name this generator writes.</returns>
    public bool Allows(string name, ReservedWords reserved, string declaringType, string? location)
    {
        var written = naming.ToPropertyName(name);
        if (reserved.Reserve(written))
        {
            diagnostics.Information(
                ScreenplayDiagnosticCodes.NameReservedByGrammar,
                $"'{name}' on '{declaringType}' is written as '@{written}' because a {reserved.Block} block reserves '{written}' as a directive",
                location);
        }

        return true;
    }
}
