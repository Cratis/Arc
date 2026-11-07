// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.PersonalAccessTokens.Contract;

/// <summary>Behavioral cases, each starting with isolated authoritative state.</summary>
/// <param name="harness">The adapter's behavioral controls.</param>
/// <param name="cancellationToken">Cancellation token.</param>
internal sealed class PatContractCases(PatAdapterHarness harness, CancellationToken cancellationToken)
{
    /// <summary>Gets the complete named behavioral checks.</summary>
    internal IReadOnlyList<(string Name, Func<Task<bool>> Check)> Checks =>
    [
        ("atomic issuance", AtomicIssuance),
        ("failed issuance rollback", FailedIssuance),
        ("resource partition", ResourcePartition),
        ("duplicate credential", () => Duplicate(true)),
        ("duplicate verifier", () => Duplicate(false)),
        ("wrong resource owner", () => WrongOwner(owner => owner with { Resource = "other-resource" })),
        ("wrong environment owner", () => WrongOwner(owner => owner with { Environment = "other-environment" })),
        ("wrong person owner", () => WrongOwner(owner => owner with { Person = new("other-person") })),
        ("wrong issuer owner", () => WrongOwner(owner => owner with { AccountKey = owner.AccountKey with { Issuer = "https://other.example" } })),
        ("wrong subject owner", () => WrongOwner(owner => owner with { AccountKey = owner.AccountKey with { Subject = "00000000-0000-4000-8000-000000000002" } })),
        ("wrong tenant owner", () => WrongOwner(owner => owner with { Tenant = new("other-tenant") })),
        ("revocation idempotency", Revoke),
        ("unknown credential revoke", UnknownRevoke),
        ("second-process revocation", SecondProcessRevocation),
        ("restore fence", RestoreFence),
        ("missing recovery evidence", MissingRecovery),
        ("unavailable authority", () => RefusesAfter(harness.BlockAuthority!, true)),
        ("unavailable recovery", () => RefusesAfter(harness.BlockRecovery!, true)),
        ("unavailable policy", () => RefusesAfter(harness.BlockPolicy!, true)),
        ("disabled person", () => RefusesAfter(harness.DisablePerson!, true)),
        ("removed membership", () => RefusesAfter(harness.RemoveMembership!, true)),
        ("reduced lifetime", () => RefusesAfter(harness.ReduceLifetime!, false)),
        ("reduced scopes", () => RefusesAfter(harness.ReduceScopes!, true)),
        ("expired SSO", () => RefusesAfter(harness.ExpireSso!, true))
    ];

    async Task<PatContractFixture> Fixture(bool create = true)
    {
        var adapter = await harness.Create!(cancellationToken);
        var epoch = await adapter.RecoveryEpoch.Read(cancellationToken) ?? throw new InvalidOperationException();
        var fixture = new PatContractFixture(adapter, epoch);
        if (!create) return fixture;
        if (!await fixture.Create(cancellationToken)) throw new InvalidOperationException();
        if ((await fixture.Verify(cancellationToken)).Facts is null) throw new InvalidOperationException();
        return fixture;
    }

    async Task<bool> AtomicIssuance()
    {
        var fixture = await Fixture(false);
        var result = await fixture.Module().Issue(TimeSpan.FromHours(1), ["read"], cancellationToken);
        using var token = result.Token;
        if (token is null) return false;
        var record = await fixture.Adapter.Authority.ReadAuthoritative(fixture.Record.Resource, PatVerifier.Derive(token.Reveal()!), cancellationToken);
        return record?.CredentialId == token.CredentialId && await harness.HasIssuedIntent!(fixture.Adapter, token.CredentialId, cancellationToken);
    }

    async Task<bool> FailedIssuance()
    {
        var fixture = await Fixture(false);
        await harness.FailNextCommit!(fixture.Adapter, cancellationToken);
        if (await TryCreate(fixture)) return false;
        if (await fixture.Read(cancellationToken) is not null || await harness.HasIssuedIntent!(fixture.Adapter, fixture.Record.CredentialId, cancellationToken)) return false;
        await harness.FailNextCommit!(fixture.Adapter, cancellationToken);
        var result = await fixture.Module().Issue(TimeSpan.FromHours(1), ["read"], cancellationToken);
        using var token = result.Token;
        return token is null;
    }

    async Task<bool> ResourcePartition()
    {
        var fixture = await Fixture();
        return await fixture.Adapter.Authority.ReadAuthoritative("other-resource", fixture.Record.Verifier, cancellationToken) is null;
    }

    async Task<bool> Duplicate(bool credential)
    {
        var fixture = await Fixture();
        var replacement = credential
            ? fixture.Record with { Verifier = PatVerifier.Derive("synthetic-other-verifier") }
            : fixture.Record with { CredentialId = new(Guid.NewGuid().ToString("D")) };
        var created = await fixture.Adapter.Authority.TryCreate(replacement, new(replacement.CredentialId, replacement.CreatedAt), cancellationToken);
        var original = await fixture.Read(cancellationToken);
        return !created && original?.CredentialId == fixture.Record.CredentialId && original.Verifier == fixture.Record.Verifier;
    }

    async Task<bool> WrongOwner(Func<PatOwner, PatOwner> change)
    {
        var fixture = await Fixture();
        var result = await fixture.Adapter.Authority.TryRevoke(change(fixture.Owner), fixture.Record.CredentialId, cancellationToken);
        return result == PatRevokeResult.Refused && (await fixture.Read(cancellationToken))?.Revoked == false;
    }

    async Task<bool> Revoke()
    {
        var fixture = await Fixture();
        var first = await fixture.Adapter.Authority.TryRevoke(fixture.Owner, fixture.Record.CredentialId, cancellationToken);
        var second = await fixture.Adapter.Authority.TryRevoke(fixture.Owner, fixture.Record.CredentialId, cancellationToken);
        return first == PatRevokeResult.Revoked && second == PatRevokeResult.AlreadyRevoked && (await fixture.Read(cancellationToken))?.Revoked == true;
    }

    async Task<bool> UnknownRevoke()
    {
        var fixture = await Fixture();
        return await fixture.Adapter.Authority.TryRevoke(fixture.Owner, new("unknown-credential"), cancellationToken) == PatRevokeResult.Refused;
    }

    async Task<bool> SecondProcessRevocation()
    {
        var fixture = await Fixture();
        var second = await harness.SecondProcess!(fixture.Adapter, cancellationToken);
        if ((await fixture.Verify(cancellationToken, second)).Facts is null) return false;
        if (await fixture.Adapter.Authority.TryRevoke(fixture.Owner, fixture.Record.CredentialId, cancellationToken) != PatRevokeResult.Revoked) return false;
        return (await fixture.Read(cancellationToken, second))?.Revoked == true && (await fixture.Verify(cancellationToken, second)).Facts is null;
    }

    async Task<bool> RestoreFence()
    {
        var fixture = await Fixture();
        var second = await harness.SecondProcess!(fixture.Adapter, cancellationToken);
        if ((await fixture.Verify(cancellationToken, second)).Facts is null) return false;
        await harness.Snapshot!(fixture.Adapter, cancellationToken);
        if (await fixture.Adapter.Authority.TryRevoke(fixture.Owner, fixture.Record.CredentialId, cancellationToken) != PatRevokeResult.Revoked) return false;
        await harness.AdvanceRecoveryEpoch!(fixture.Adapter, cancellationToken);
        await harness.Restore!(fixture.Adapter, cancellationToken);
        var restored = await fixture.Read(cancellationToken, second);
        var epoch = await second.RecoveryEpoch.Read(cancellationToken);
        return restored?.Revoked == false && epoch is not null && epoch != fixture.Record.RecoveryEpoch && (await fixture.Verify(cancellationToken, second)).Facts is null;
    }

    async Task<bool> MissingRecovery()
    {
        var fixture = await Fixture();
        await harness.RemoveRecoveryEvidence!(fixture.Adapter, cancellationToken);
        for (var attempt = 0; attempt < 2; attempt++)
        {
            if (await ReadEpoch(fixture) is not null) return false;
        }
        return await Refused(fixture, true);
    }

    async Task<bool> TryCreate(PatContractFixture fixture)
    {
        try
        {
            return await fixture.Create(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception) { return false; }
    }

    async Task<ulong?> ReadEpoch(PatContractFixture fixture)
    {
        try
        {
            return await fixture.Adapter.RecoveryEpoch.Read(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception) { return null; }
    }

    async Task<bool> RefusesAfter(Func<PatAdapterInstance, CancellationToken, Task> change, bool issue)
    {
        var fixture = await Fixture();
        await change(fixture.Adapter, cancellationToken);
        return await Refused(fixture, issue);
    }

    async Task<bool> Refused(PatContractFixture fixture, bool issue)
    {
        if ((await fixture.Verify(cancellationToken)).Facts is not null) return false;
        if (!issue) return true;
        var result = await fixture.Module().Issue(TimeSpan.FromHours(1), ["read"], cancellationToken);
        using var token = result.Token;
        return token is null;
    }
}
