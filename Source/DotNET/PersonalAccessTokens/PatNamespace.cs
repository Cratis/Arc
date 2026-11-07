// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;

namespace Cratis.Arc.PersonalAccessTokens;

/// <summary>Classifies an extracted authorization value before parsing or credential verification.</summary>
/// <remarks>
/// Mirrors the anchored ASCII pattern [a-z0-9][a-z0-9-]*_pat_ case-insensitively.
/// A PAT namespace hint applies under any authorization scheme, including a non-Bearer scheme;
/// it does not accept that scheme or authenticate the credential. Never retry such a value as OAuth.
/// </remarks>
internal static class PatNamespace
{
    /// <summary>Gets a dispatch hint without normalizing the value or testing whether its resource is registered.</summary>
    /// <param name="value">The extracted credential value, independent of the authorization scheme.</param>
    /// <returns>The PAT or OAuth namespace hint, never proof of authentication.</returns>
    internal static PresentedCredentialShape Classify(string? value) => IsPatNamespace(value) ? PresentedCredentialShape.PatBearer : PresentedCredentialShape.OAuthBearer;

    static bool IsPatNamespace(string? value)
    {
        if (string.IsNullOrEmpty(value)) return false;
        var keyEnd = value.IndexOf('_', StringComparison.Ordinal);
        if (keyEnd < 1 || !char.IsAsciiLetterOrDigit(value[0])) return false;
        foreach (var character in value.AsSpan(1, keyEnd - 1))
        {
            if (!char.IsAsciiLetterOrDigit(character) && character != '-') return false;
        }
        return value.AsSpan(keyEnd).StartsWith("_pat_", StringComparison.OrdinalIgnoreCase);
    }
}
