// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;

namespace Cratis.Arc.PersonalAccessTokens;

/// <summary>A one-time secret handle. Dispose when not revealed. Serialization and formatting do not reveal it.</summary>
public sealed class IssuedPat : IDisposable
{
    char[]? _value;

    /// <summary>Holds an issued value until one explicit reveal or disposal.</summary>
    /// <param name="value">The bearer value. Never log it.</param>
    /// <param name="credentialId">Non-secret identifier.</param>
    /// <param name="expiresAt">Absolute expiry.</param>
    internal IssuedPat(string value, CredentialId credentialId, DateTimeOffset expiresAt)
    {
        _value = value.ToCharArray();
        CredentialId = credentialId;
        ExpiresAt = expiresAt;
    }

    /// <summary>Gets the non-secret credential identifier.</summary>
    public CredentialId CredentialId { get; }

    /// <summary>Gets the recorded expiry.</summary>
    public DateTimeOffset ExpiresAt { get; }

    /// <summary>Reveals once, then forgets the secret. The returned value must never be logged or persisted.</summary>
    /// <returns>The bearer value on the first call, otherwise null.</returns>
    public string? Reveal()
    {
        var value = Interlocked.Exchange(ref _value, null);
        if (value is null) return null;
        try
        {
            return new string(value);
        }
        finally
        {
            Array.Clear(value);
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        var value = Interlocked.Exchange(ref _value, null);
        if (value is not null) Array.Clear(value);
    }

    /// <inheritdoc/>
    public override string ToString() => nameof(IssuedPat);
}
