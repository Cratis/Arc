// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;

namespace Cratis.Arc.Screenplay.Embedded.Hosting.Board;

/// <summary>
/// Derives the identities a board document is built from, out of the path to the thing being identified.
/// </summary>
/// <remarks>
/// The board requires identities to be GUIDs, and Screenplay has none to give. Deriving them from the path
/// keeps the document reproducible - the same source served twice is the same document, byte for byte - and
/// lets a slice reference an event another slice produces without a shared mutable registry.
/// </remarks>
internal static class DeterministicId
{
    const char Separator = '\u001f';

    /// <summary>
    /// Gets the identity for a path.
    /// </summary>
    /// <param name="parts">The parts of the path to the thing being identified.</param>
    /// <returns>The identity.</returns>
    public static Guid From(params string?[] parts)
    {
        var input = string.Join(Separator, parts.Select(part => part ?? string.Empty));
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        var bytes = hash.AsSpan(0, 16).ToArray();

        // Shaped as a name-based UUID so readers that validate the version and variant accept it.
        bytes[6] = (byte)((bytes[6] & 0x0F) | 0x50);
        bytes[8] = (byte)((bytes[8] & 0x3F) | 0x80);
        return new Guid(bytes);
    }
}
