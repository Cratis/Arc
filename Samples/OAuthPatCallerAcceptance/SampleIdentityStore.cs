// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Security.Cryptography;
using Cratis.Arc.PersonalAccessTokens;

namespace OAuthPatCallerAcceptance;

/// <summary>Process-local synthetic authorities for the executable sample. Not a production token store.</summary>
public sealed class SampleIdentityStore
{
    readonly ConcurrentDictionary<string, TokenRecord> _tokens = new(StringComparer.Ordinal);
    readonly ConcurrentDictionary<string, (PatRecord Record, PatIssuedIntent? Intent)> _pats = new(StringComparer.Ordinal);
    int _oauthReads;
    int _patReads;
    int _dispatches;
    int _patIssueAttempts;

    /// <summary>Gets the independent OAuth lookup count.</summary>
    public int OAuthReads => _oauthReads;

    /// <summary>Gets the authoritative PAT lookup count.</summary>
    public int PatReads => _patReads;

    /// <summary>Gets the number of dispatched operations.</summary>
    public int Dispatches => _dispatches;

    /// <summary>Gets the number of authoritative PAT issuance attempts.</summary>
    public int PatIssueAttempts => _patIssueAttempts;

    /// <summary>Gets whether the synthetic introspection service throws.</summary>
    public bool ThrowOnIntrospection { get; set; }

    /// <summary>Gets whether mapped persons exist.</summary>
    public bool PersonExists { get; set; } = true;

    /// <summary>Gets or sets whether the synthetic person remains a member.</summary>
    public bool MembershipActive { get; set; } = true;

    /// <summary>Gets or sets whether synthetic introspection reports cancellation.</summary>
    public bool CancelIntrospection { get; set; }

    /// <summary>Issues a random opaque synthetic token mapped to independent authoritative data.</summary>
    /// <param name="record">The introspection data.</param>
    /// <returns>The synthetic bearer. Never log it.</returns>
    public string Issue(TokenRecord record)
    {
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        _tokens[token] = record;
        return token;
    }

    /// <summary>Looks up a synthetic access token, without trusting forwarding expectations.</summary>
    /// <param name="token">The exact token.</param>
    /// <returns>Authoritative data, or null.</returns>
    /// <exception cref="InvalidOperationException">Synthetic introspection is configured to be unavailable.</exception>
    /// <exception cref="OperationCanceledException">Synthetic introspection is configured to time out.</exception>
    public TokenRecord? Introspect(string token)
    {
        Interlocked.Increment(ref _oauthReads);
        if (ThrowOnIntrospection) throw new InvalidOperationException("Synthetic introspection unavailable.");
        if (CancelIntrospection) throw new OperationCanceledException("Synthetic introspection timed out.");
        return _tokens.GetValueOrDefault(token);
    }

    /// <summary>Seeds an authoritative PAT record by its verifier digest, not its raw token.</summary>
    /// <param name="record">The record.</param>
    public void Seed(PatRecord record) => _pats[record.Verifier] = (record, null);

    /// <summary>Atomically records the verifier and secret-free issuance intent.</summary>
    /// <param name="record">The credential metadata.</param>
    /// <param name="intent">The issuance intent.</param>
    /// <returns>Whether the synthetic authority acknowledged the unique commit.</returns>
    public bool TryIssue(PatRecord record, PatIssuedIntent intent)
    {
        Interlocked.Increment(ref _patIssueAttempts);
        return _pats.TryAdd(record.Verifier, (record, intent));
    }

    /// <summary>Reads the authoritative PAT record.</summary>
    /// <param name="verifier">The verifier digest.</param>
    /// <returns>The record, or null.</returns>
    public PatRecord? ReadPat(string verifier)
    {
        Interlocked.Increment(ref _patReads);
        return _pats.TryGetValue(verifier, out var entry) ? entry.Record : null;
    }

    /// <summary>Records a successful authorized operation.</summary>
    public void Dispatched() => Interlocked.Increment(ref _dispatches);
}
