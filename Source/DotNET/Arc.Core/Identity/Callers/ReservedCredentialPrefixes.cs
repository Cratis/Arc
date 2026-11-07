// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers;

/// <summary>
/// An explicit, immutable installation boundary for retired credential namespaces.
/// This runs before source selection, including when an envelope or another header is present.
/// </summary>
/// <param name="prefixes">Validated ASCII prefixes owned by this installation.</param>
internal sealed class ReservedCredentialPrefixes(IEnumerable<string> prefixes)
{
    readonly string[] _prefixes = prefixes.ToArray();

    /// <summary>Checks only an explicitly reserved ASCII namespace, independently of the scheme.</summary>
    /// <param name="authorization">The raw Authorization value. Never log it.</param>
    /// <returns>Whether the value must be refused before dispatch.</returns>
    internal bool Refuses(string authorization)
    {
        var separator = authorization.IndexOf(' ');
        var value = separator < 0 ? authorization.AsSpan() : authorization.AsSpan(separator + 1).TrimStart(' ');
        foreach (var prefix in _prefixes)
        {
            if (value.Length >= prefix.Length && System.Text.Ascii.EqualsIgnoreCase(value[..prefix.Length], prefix)) return true;
        }
        return false;
    }
}
