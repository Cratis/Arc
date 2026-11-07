// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers;

/// <summary>
/// An immutable installation boundary for built-in and host-added retired credential namespaces.
/// This runs before source selection, including when an envelope or another header is present.
/// </summary>
/// <param name="prefixes">Validated ASCII prefixes owned by this installation.</param>
internal sealed class ReservedCredentialPrefixes(IEnumerable<string> prefixes)
{
    readonly string[] _prefixes = KnownRetiredPrefixes.Select(prefix => $"{prefix}_").Concat(prefixes).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

    /// <summary>Gets the retired ASCII public format markers that are always refused.</summary>
    internal static IReadOnlyList<string> KnownRetiredPrefixes { get; } = Array.AsReadOnly<string>(["dpat", "drun"]);

    /// <summary>Checks a retired ASCII namespace, independently of the scheme.</summary>
    /// <param name="authorization">The raw credential value. Never log it.</param>
    /// <returns>Whether the value must be refused before dispatch.</returns>
    internal bool Refuses(string authorization)
    {
        if (HasRetiredPrefix(authorization.AsSpan())) return true;
        var separator = authorization.IndexOfAny([' ', '\t', '\r', '\n', '\f', '\v']);
        return separator >= 0 && HasRetiredPrefix(authorization.AsSpan(separator + 1).TrimStart(" \t\r\n\f\v"));
    }

    bool HasRetiredPrefix(ReadOnlySpan<char> value)
    {
        foreach (var prefix in _prefixes)
        {
            if (value.Length >= prefix.Length && System.Text.Ascii.EqualsIgnoreCase(value[..prefix.Length], prefix)) return true;
        }
        return false;
    }
}
