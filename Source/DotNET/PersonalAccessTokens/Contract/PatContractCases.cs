// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.PersonalAccessTokens.Contract;

/// <summary>Required behavioral checks over fresh isolated stores.</summary>
/// <param name="cancellationToken">The bounded case cancellation token.</param>
internal sealed class PatContractCases(CancellationToken cancellationToken)
{
    /// <summary>Gets every required named check.</summary>
    internal IReadOnlyList<(string Name, Func<PatContractFixture, Task<bool>> Check)> Checks =>
    [
        ("duplicate verifier", fixture => Duplicate(fixture, false)),
        ("duplicate credential", fixture => Duplicate(fixture, true)),
        ("authoritative lookup", Lookup),
        ("revoke-before-read ordering", OrderedRevoke),
        ("owner-bound revocation", OwnerBoundRevoke),
        ("concurrent create", ConcurrentCreate),
        ("authority outage", AuthorityOutage),
        ("immutable stored metadata", ImmutableMetadata),
        ("cancelled create", CancelledCreate),
        ("missing recovery evidence", MissingRecovery),
        ("restore fence", RestoreFence),
        ("recovery outage", RecoveryOutage),
        ("monotonic recovery epoch", MonotonicEpoch),
        ("policy denial", PolicyDenial),
        ("policy outage", PolicyOutage),
        ("positive policy lifetime", PolicyLifetime),
        ("issued token verifies", IssuedToken),
        ("revoked token refuses", RevokedToken),
        ("recovery invalidates token", RecoveryInvalidatesToken),
        ("verification outage refuses", VerificationOutage),
        ("hostile metadata refuses", HostileMetadata),
        ("verifier-only persistence", VerifierOnly)
    ];

    static bool SameMetadata(PatRecord? actual, PatRecord expected)
    {
        if (actual is null) return false;
        if (actual with { Scopes = expected.Scopes, Assurance = expected.Assurance } != expected) return false;
        if (!actual.Scopes.SetEquals(expected.Scopes)) return false;
        if (actual.Assurance with { Amr = expected.Assurance.Amr } != expected.Assurance) return false;
        return (actual.Assurance.Amr ?? []).SequenceEqual(expected.Assurance.Amr ?? []);
    }

    async Task Active(PatContractFixture fixture)
    {
        if (!await fixture.Create(cancellationToken)) throw new InvalidOperationException();
        if ((await fixture.Verify(cancellationToken)).Facts is null) throw new InvalidOperationException();
    }

    async Task<bool> Duplicate(PatContractFixture fixture, bool credential)
    {
        await Active(fixture);
        var replacement = credential
            ? fixture.Record with { Verifier = PatVerifier.Derive("synthetic-other-verifier") }
            : fixture.Record with { CredentialId = new(Guid.NewGuid().ToString("D")) };
        var created = await fixture.Adapter.Authority.TryCreate(replacement, new(replacement.CredentialId, replacement.CreatedAt), cancellationToken);
        var original = await fixture.Read(cancellationToken);
        return !created && SameMetadata(original, fixture.Record);
    }

    async Task<bool> Lookup(PatContractFixture fixture)
    {
        await Active(fixture);
        var authority = fixture.Adapter.Authority;
        if (await authority.ReadAuthoritative("other-resource", fixture.Record.Verifier, cancellationToken) is not null) return false;
        foreach (var verifier in new[] { PatVerifier.Derive("unknown"), "short", fixture.Record.Verifier.ToLowerInvariant(), new string('z', 64) })
        {
            if (await authority.ReadAuthoritative(fixture.Record.Resource, verifier, cancellationToken) is not null) return false;
        }
        return true;
    }

    async Task<bool> OrderedRevoke(PatContractFixture fixture)
    {
        for (var index = 0; index < 100; index++)
        {
            var record = fixture.Record with { CredentialId = new(Guid.NewGuid().ToString("D")), Verifier = PatVerifier.Derive($"synthetic-order-{index}") };
            if (!await fixture.Adapter.Authority.TryCreate(record, new(record.CredentialId, record.CreatedAt), cancellationToken)) return false;
            var revoke = fixture.Adapter.Authority.TryRevoke(fixture.Owner, record.CredentialId, cancellationToken);
            var read = ReadAfterRevoke(fixture, record, revoke);
            await Task.WhenAll(revoke, read);
            if (!await read) return false;
        }
        return true;
    }

    async Task<bool> ReadAfterRevoke(PatContractFixture fixture, PatRecord record, Task<PatRevokeResult> revoke)
    {
        if (await revoke != PatRevokeResult.Revoked) return false;
        var current = await fixture.Adapter.Authority.ReadAuthoritative(record.Resource, record.Verifier, cancellationToken);
        return current?.Revoked == true;
    }

    async Task<bool> OwnerBoundRevoke(PatContractFixture fixture)
    {
        await Active(fixture);
        var owner = fixture.Owner;
        var others = new[]
        {
            owner with { Resource = "other-resource" },
            owner with { Environment = "other-environment" },
            owner with { Person = new("other-person") },
            owner with { AccountKey = owner.AccountKey with { Issuer = "https://other.example" } },
            owner with { AccountKey = owner.AccountKey with { Subject = "00000000-0000-4000-8000-000000000002" } },
            owner with { Tenant = new("other-tenant") }
        };
        foreach (var other in others)
        {
            if (await fixture.Adapter.Authority.TryRevoke(other, fixture.Record.CredentialId, cancellationToken) != PatRevokeResult.Refused) return false;
            if ((await fixture.Read(cancellationToken))?.Revoked != false) return false;
        }
        if (await fixture.Adapter.Authority.TryRevoke(owner, new("unknown-credential"), cancellationToken) != PatRevokeResult.Refused) return false;
        if (await fixture.Adapter.Authority.TryRevoke(owner, fixture.Record.CredentialId, cancellationToken) != PatRevokeResult.Revoked) return false;
        var before = await fixture.Read(cancellationToken);
        var repeated = await fixture.Adapter.Authority.TryRevoke(owner, fixture.Record.CredentialId, cancellationToken);
        return repeated == PatRevokeResult.AlreadyRevoked && SameMetadata(await fixture.Read(cancellationToken), before!);
    }

    async Task<bool> ConcurrentCreate(PatContractFixture fixture)
    {
        var results = await Task.WhenAll(Enumerable.Range(0, 16).Select(_ => fixture.Create(cancellationToken)));
        return results.Count(created => created) == 1 && SameMetadata(await fixture.Read(cancellationToken), fixture.Record);
    }

    async Task<bool> AuthorityOutage(PatContractFixture fixture)
    {
        await Active(fixture);
        await fixture.Adapter.SetOutage!(true);
        var replacement = fixture.Record with { CredentialId = new(Guid.NewGuid().ToString("D")), Verifier = PatVerifier.Derive("synthetic-outage") };
        if (await OrUnavailable(() => fixture.Adapter.Authority.TryCreate(replacement, new(replacement.CredentialId, replacement.CreatedAt), cancellationToken), false)) return false;
        if (await OrUnavailable(() => fixture.Read(cancellationToken), null) is not null) return false;
        var revoked = await OrUnavailable(() => fixture.Adapter.Authority.TryRevoke(fixture.Owner, fixture.Record.CredentialId, cancellationToken), PatRevokeResult.Unavailable);
        if (revoked != PatRevokeResult.Unavailable) return false;
        await fixture.Adapter.SetOutage!(false);
        if ((await fixture.Read(cancellationToken))?.Revoked != false) return false;
        return await fixture.Adapter.Authority.ReadAuthoritative(replacement.Resource, replacement.Verifier, cancellationToken) is null;
    }

    async Task<bool> ImmutableMetadata(PatContractFixture fixture)
    {
        var scopes = new HashSet<string> { "read" };
        var methods = new[] { "sso" };
        var record = fixture.Record with { Scopes = scopes, Assurance = fixture.Record.Assurance with { Amr = methods } };
        if (!await fixture.Adapter.Authority.TryCreate(record, fixture.Intent, cancellationToken)) return false;
        scopes.Add("injected");
        methods[0] = "injected";
        var stored = await fixture.Read(cancellationToken);
        return stored?.Scopes.SetEquals(["read"]) == true && stored.Assurance.Amr?.SequenceEqual(["sso"]) == true;
    }

    async Task<bool> CancelledCreate(PatContractFixture fixture)
    {
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();
        var created = false;
        try { created = await fixture.Create(cancelled.Token); }
        catch (OperationCanceledException) { return await fixture.Read(cancellationToken) is null; }
        return !created && await fixture.Read(cancellationToken) is null;
    }

    async Task<bool> MissingRecovery(PatContractFixture fixture)
    {
        await fixture.Adapter.RemoveRecoveryEvidence!();
        if (await OrUnavailable(() => fixture.Adapter.RecoveryEpoch.Read(cancellationToken), null) is not null) return false;
        return await OrUnavailable(() => fixture.Adapter.RecoveryEpoch.Read(cancellationToken), null) is null;
    }

    async Task<bool> RestoreFence(PatContractFixture fixture)
    {
        await Active(fixture);
        await fixture.Adapter.Restore!();
        var epoch = await OrUnavailable(() => fixture.Adapter.RecoveryEpoch.Read(cancellationToken), null);
        return epoch is null || epoch > fixture.Record.RecoveryEpoch;
    }

    async Task<bool> RecoveryOutage(PatContractFixture fixture)
    {
        await fixture.Adapter.SetOutage!(true);
        return await OrUnavailable(() => fixture.Adapter.RecoveryEpoch.Read(cancellationToken), null) is null;
    }

    async Task<bool> MonotonicEpoch(PatContractFixture fixture)
    {
        var before = fixture.Record.RecoveryEpoch;
        for (var index = 0; index < 3; index++)
        {
            await fixture.Adapter.AdvanceRecoveryEpoch!();
            var after = await fixture.Adapter.RecoveryEpoch.Read(cancellationToken);
            if (after is null || after < before) return false;
            before = after;
        }
        return true;
    }

    async Task<bool> PolicyDenial(PatContractFixture fixture)
    {
        if (!await PolicyPermits(fixture)) return false;
        await fixture.Adapter.DenyEverything!();
        return !await PolicyPermitsEither(fixture);
    }

    async Task<bool> PolicyOutage(PatContractFixture fixture)
    {
        if (!await PolicyPermits(fixture)) return false;
        await fixture.Adapter.SetOutage!(true);
        return !await PolicyPermitsEither(fixture);
    }

    async Task<bool> PolicyLifetime(PatContractFixture fixture)
    {
        foreach (var maximum in new[] { TimeSpan.Zero, TimeSpan.FromSeconds(-1) })
        {
            await fixture.Adapter.Permit!(maximum);
            foreach (var issuing in new[] { false, true })
            {
                var decision = await fixture.Adapter.Policy.Evaluate(fixture.Record, issuing, cancellationToken);
                if (decision.Permitted && decision.MaximumLifetime <= TimeSpan.Zero) return false;
            }
        }
        return true;
    }

    async Task<bool> PolicyPermits(PatContractFixture fixture)
    {
        foreach (var issuing in new[] { false, true })
        {
            if (!(await fixture.Adapter.Policy.Evaluate(fixture.Record, issuing, cancellationToken)).Permitted) return false;
        }
        return true;
    }

    async Task<bool> PolicyPermitsEither(PatContractFixture fixture)
    {
        foreach (var issuing in new[] { false, true })
        {
            var decision = await OrUnavailable(() => fixture.Adapter.Policy.Evaluate(fixture.Record, issuing, cancellationToken), new(false, TimeSpan.Zero, null));
            if (decision.Permitted) return true;
        }
        return false;
    }

    async Task<bool> IssuedToken(PatContractFixture fixture)
    {
        var result = await fixture.Module().Issue(TimeSpan.FromHours(1), ["read"], cancellationToken);
        using var token = result.Token;
        return token is not null && (await fixture.VerifyRaw(token.Reveal()!, cancellationToken)).Facts is not null;
    }

    async Task<bool> RevokedToken(PatContractFixture fixture)
    {
        await Active(fixture);
        if (await fixture.Adapter.Authority.TryRevoke(fixture.Owner, fixture.Record.CredentialId, cancellationToken) != PatRevokeResult.Revoked) return false;
        return (await fixture.Verify(cancellationToken)).Facts is null;
    }

    async Task<bool> RecoveryInvalidatesToken(PatContractFixture fixture)
    {
        await Active(fixture);
        await fixture.Adapter.AdvanceRecoveryEpoch!();
        if ((await fixture.Verify(cancellationToken)).Facts is not null) return false;
        var result = await fixture.Module().Issue(TimeSpan.FromHours(1), ["read"], cancellationToken);
        using var token = result.Token;
        if (token is null) return false;
        if ((await fixture.VerifyRaw(token.Reveal()!, cancellationToken)).Facts is null) return false;
        await fixture.Adapter.Restore!();
        return (await fixture.VerifyRaw(token.Reveal()!, cancellationToken)).Facts is null;
    }

    async Task<bool> VerificationOutage(PatContractFixture fixture)
    {
        await Active(fixture);
        await fixture.Adapter.SetOutage!(true);
        return (await fixture.Verify(cancellationToken)).Facts is null;
    }

    async Task<bool> HostileMetadata(PatContractFixture fixture)
    {
        await Active(fixture);
        var mutations = new Func<PatRecord, PatRecord>[]
        {
            record => record with { RecoveryEpoch = record.RecoveryEpoch + 1 },
            record => record with { ExpiresAt = record.CreatedAt },
            record => record with { Audience = "https://other.example" }
        };
        foreach (var mutation in mutations)
        {
            var authority = new PatContractAuthority(fixture.Adapter.Authority, mutation);
            var adapter = fixture.Adapter with { Authority = authority };
            if ((await fixture.Verify(cancellationToken, adapter)).Facts is not null) return false;
        }
        return true;
    }

    async Task<bool> VerifierOnly(PatContractFixture fixture)
    {
        var authority = new PatContractAuthority(fixture.Adapter.Authority);
        var result = await fixture.Module(authority).Issue(TimeSpan.FromHours(1), ["read"], cancellationToken);
        using var token = result.Token;
        if (token is null) return false;
        var raw = token.Reveal()!;
        var adapter = fixture.Adapter with { Authority = authority };
        if ((await fixture.VerifyRaw(raw, cancellationToken, adapter)).Facts is null) return false;
        return authority.AttemptedRecord?.Verifier == PatVerifier.Derive(raw) && authority.Lookup == PatVerifier.Derive(raw) && authority.Lookup != raw;
    }

    async Task<T> OrUnavailable<T>(Func<Task<T>> operation, T unavailable)
    {
        try { return await operation(); }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception) { return unavailable; }
    }
}
