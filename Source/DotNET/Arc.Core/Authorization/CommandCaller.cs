// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Arc.Commands;

namespace Cratis.Arc.Authorization;

/// <summary>An immutable view of the selected caller captured at the command boundary.</summary>
public sealed class CommandCaller
{
    CommandCaller(bool isAuthenticated, string? authenticationType, IReadOnlyList<CommandCallerClaim> claims)
    {
        IsAuthenticated = isAuthenticated;
        AuthenticationType = authenticationType;
        Claims = claims;
    }

    /// <summary>Gets whether the selected caller is authenticated.</summary>
    public bool IsAuthenticated { get; }

    /// <summary>Gets whether the caller is a system principal.</summary>
    public bool IsSystem => string.Equals(AuthenticationType, "System", StringComparison.Ordinal) || Claims.Any(claim => claim.Type == "sub" && claim.Value == "[System]");

    /// <summary>Gets the selected caller's authentication type.</summary>
    public string? AuthenticationType { get; }

    /// <summary>Gets the captured claims. Actor-chain claims are never included.</summary>
    public IReadOnlyList<CommandCallerClaim> Claims { get; }

    /// <summary>Finds exactly one distinct non-empty claim value using ordinal comparisons.</summary>
    /// <param name="claimType">The exact claim type.</param>
    /// <returns>A found, missing or ambiguous lookup.</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1720:Identifier contains type name", Justification = "Single describes claim cardinality, not the numeric type.")]
    public ReadModelKeyLookup Single(string claimType)
    {
        if (!IsAuthenticated) return ReadModelKeyLookup.Missing;
        var values = Claims.Where(claim => string.Equals(claim.Type, claimType, StringComparison.Ordinal) && !string.IsNullOrEmpty(claim.Value))
            .Select(claim => claim.Value).Distinct(StringComparer.Ordinal).Take(2).ToArray();
        return values.Length switch
        {
            0 => ReadModelKeyLookup.Missing,
            1 => ReadModelKeyLookup.Found(values[0]),
            _ => ReadModelKeyLookup.Ambiguous
        };
    }

    /// <summary>Builds a caller exclusively from captured standard identity content.</summary>
    /// <param name="snapshot">The captured principal.</param>
    /// <returns>The immutable caller.</returns>
    internal static CommandCaller From(PrincipalSnapshot snapshot)
    {
        var identities = JsonSerializer.Deserialize(snapshot.Fingerprint, PrincipalIdentityJsonSerializerContext.Default.Identities)?.ToArray() ?? [];
        var claims = identities.SelectMany(identity => identity.Claims).Select(claim => new CommandCallerClaim(claim.Type, claim.Value, claim.Issuer)).ToArray();
        var authenticationType = identities.FirstOrDefault()?.AuthenticationType;
        return new(!string.IsNullOrEmpty(authenticationType), authenticationType, Array.AsReadOnly(claims));
    }
}
