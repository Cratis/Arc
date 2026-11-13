// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;

namespace Cratis.Arc.PersonalAccessTokens.Contract;

/// <summary>Only synthetic facts; none of the verified caller factories escape the assembly.</summary>
internal sealed class PatContractFixture : IPatIssuanceContext
{
    readonly string _raw;
    readonly VerifiedCaller _caller;

    /// <summary>Initializes a new instance of the <see cref="PatContractFixture"/> class.</summary>
    /// <param name="adapter">The isolated adapters.</param>
    /// <param name="epoch">The current recovery epoch.</param>
    internal PatContractFixture(PatAdapterInstance adapter, ulong? epoch)
    {
        Adapter = adapter;
        Settings = PatSettings.Create(new("stage-contract") { AllowedScopes = ["read"] }, new("pat-contract-resource", "https://pat-contract.example", "contract"));
        var now = DateTimeOffset.UtcNow;
        _raw = PatFormat.Create(Settings.ResourceKey).Reveal();
        Record = new(
            new(Guid.NewGuid().ToString("D")),
            PatVerifier.Derive(_raw),
            "stage-contract",
            Settings.Target.Resource,
            Settings.Target.Audience,
            Settings.Target.Environment,
            new("pat-contract-person"),
            new("https://pat-contract.example", "00000000-0000-4000-8000-000000000001"),
            new("pat-contract-tenant"),
            new("pat-contract-organisation"),
            new HashSet<string> { "read" },
            now,
            now.AddHours(1),
            new("https://pat-contract.example", now.AddMinutes(-5).ToUnixTimeSeconds(), "basic", ["sso"]),
            epoch,
            null);
        _caller = new VerifiedCallerBuilder(new(new Dictionary<string, string> { [Record.Resource] = Record.Audience })).Build(
            new(CallerPrincipalKind.Person, CallerCredentialKind.BrowserSession, new("pat-contract-session"), Settings.Target)
            {
                Person = Record.Person,
                AccountKey = Record.AccountKey,
                Tenant = Record.Tenant,
                Organisation = Record.Organisation,
                Assurance = Record.Assurance,
                Scopes = Record.Scopes
            },
            new(Record.Resource, Record.Environment),
            Record.Tenant).Caller!;
    }

    /// <summary>Gets the isolated adapters.</summary>
    internal PatAdapterInstance Adapter { get; }

    /// <summary>Gets the synthetic installation limits.</summary>
    internal PatSettings Settings { get; }

    /// <summary>Gets the synthetic credential metadata.</summary>
    internal PatRecord Record { get; }

    /// <summary>Gets the synthetic checked owner.</summary>
    internal PatOwner Owner => new(Record.Resource, Record.Environment, Record.Person, Record.AccountKey, Record.Tenant);

    /// <summary>Gets the synthetic issuance intent.</summary>
    internal PatIssuedIntent Intent => new(Record.CredentialId, Record.CreatedAt);

    /// <inheritdoc/>
    public Task<VerifiedCaller?> GetCurrent(CancellationToken cancellationToken) => Task.FromResult<VerifiedCaller?>(_caller);

    /// <summary>Commits the synthetic credential and intent.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Whether the atomic commit succeeded.</returns>
    internal Task<bool> Create(CancellationToken cancellationToken) => Adapter.Authority.TryCreate(Record, Intent, cancellationToken);

    /// <summary>Reads current authoritative credential state.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <param name="adapter">An optional second process.</param>
    /// <returns>The authoritative metadata.</returns>
    internal Task<PatRecord?> Read(CancellationToken cancellationToken, PatAdapterInstance? adapter = null) =>
        (adapter ?? Adapter).Authority.ReadAuthoritative(Record.Resource, Record.Verifier, cancellationToken);

    /// <summary>Exercises the framework verification path.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <param name="adapter">An optional second process.</param>
    /// <returns>The framework verification result.</returns>
    internal Task<CredentialVerification> Verify(CancellationToken cancellationToken, PatAdapterInstance? adapter = null) => VerifyRaw(_raw, cancellationToken, adapter);

    /// <summary>Verifies an issued token through the real framework path.</summary>
    /// <param name="raw">The synthetic token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <param name="adapter">An optional observing authority.</param>
    /// <returns>The framework verification result.</returns>
    internal Task<CredentialVerification> VerifyRaw(string raw, CancellationToken cancellationToken, PatAdapterInstance? adapter = null)
    {
        var current = adapter ?? Adapter;
        var source = new PatCredentialSource(Settings, current.Authority, current.RecoveryEpoch, current.Policy, TimeProvider.System);
        return source.Verify(new(["Bearer " + raw], new Dictionary<string, IReadOnlyList<string>>(), shape: PresentedCredentialShape.PatBearer), cancellationToken);
    }

    /// <summary>Creates the framework issuance module.</summary>
    /// <param name="authority">An optional observing authority.</param>
    /// <returns>The module bound to synthetic checked facts.</returns>
    internal PersonalAccessTokens Module(IPatAuthority? authority = null) => new(Settings, authority ?? Adapter.Authority, Adapter.RecoveryEpoch, Adapter.Policy, this, TimeProvider.System, []);
}
