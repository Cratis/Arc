// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Arc.Screenplay.Emission.Policies;

/// <summary>
/// Converts what an application calls a policy into what the document can refer to it as.
/// </summary>
/// <remarks>
/// The grammar accepts a policy reference only as a Pascal cased identifier, while an application is free to name a
/// role or a policy anything at all - <c>can-reserve</c>, <c>reader</c>, <c>Reservations.Approve</c>. Everything an
/// identifier cannot hold is therefore treated as the boundary between two words, so the name reads the way it was
/// written rather than running together. Every reference and every declaration goes through here, so the two always
/// agree and the document never refers to a policy it does not declare.
/// </remarks>
public static class PolicyNames
{
    /// <summary>
    /// The shortest name the grammar accepts as a policy reference.
    /// </summary>
    public const int MinimumLength = 2;

    /// <summary>
    /// Converts a name into the form a policy reference takes.
    /// </summary>
    /// <param name="name">The name to convert.</param>
    /// <returns>The converted name, empty when nothing the grammar accepts is left.</returns>
    public static string For(string? name)
    {
        var identifier = new StringBuilder();
        var startsWord = true;

        foreach (var character in name ?? string.Empty)
        {
            if (!char.IsLetterOrDigit(character))
            {
                startsWord = true;
                continue;
            }

            identifier.Append(startsWord ? char.ToUpperInvariant(character) : character);
            startsWord = false;
        }

        var converted = identifier.ToString().Normalize(NormalizationForm.FormC);

        return converted.Length < MinimumLength || !char.IsAsciiLetterUpper(converted[0]) ? string.Empty : converted;
    }
}
