// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Tenancy;

namespace Cratis.Arc.Identity.Callers;

/// <summary>
/// A framework-owned immutable caller. Collection values are frozen copies, never verifier-owned mutable sets.
/// Verification does not grant product membership or operation authorization.
/// </summary>
public sealed record VerifiedCaller
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VerifiedCaller"/> class from checked framework facts.
    /// </summary>
    /// <param name="contractVersion">The locally assigned contract version.</param>
    /// <param name="principalKind">The verified principal kind.</param>
    /// <param name="credentialKind">The verified credential kind.</param>
    /// <param name="credentialId">The verified credential identifier.</param>
    /// <param name="accountKey">The person's account, if applicable.</param>
    /// <param name="person">The person, if applicable.</param>
    /// <param name="servicePrincipal">The service, if applicable.</param>
    /// <param name="actor">The delegated actor, if applicable.</param>
    /// <param name="grantId">The checked delegation grant, if applicable.</param>
    /// <param name="assurance">The verified person assurance, if applicable.</param>
    /// <param name="client">Client metadata, never implicit actor authority.</param>
    /// <param name="resource">The checked receiving resource.</param>
    /// <param name="tenant">The checked tenant.</param>
    /// <param name="organisation">The confirmed organization binding, if relevant.</param>
    /// <param name="policyEpoch">The verified policy epoch, never independent authorization.</param>
    /// <param name="environment">The checked environment.</param>
    /// <param name="scopes">The immutable verified scope ceiling.</param>
    /// <param name="permissions">The immutable explicit permission ceiling.</param>
    /// <param name="senderBinding">The checked sender binding, if present.</param>
    internal VerifiedCaller(
        string contractVersion,
        CallerPrincipalKind principalKind,
        CallerCredentialKind credentialKind,
        CredentialId credentialId,
        CallerAccountKey? accountKey,
        CallerPersonId? person,
        CallerServicePrincipalId? servicePrincipal,
        CallerActor? actor,
        CallerGrantId? grantId,
        CallerAssurance? assurance,
        CallerClient? client,
        string resource,
        TenantId? tenant,
        CallerOrganisationId? organisation,
        ulong? policyEpoch,
        string environment,
        IReadOnlySet<string> scopes,
        IReadOnlySet<string> permissions,
        CallerSenderBinding? senderBinding)
    {
        ContractVersion = contractVersion;
        PrincipalKind = principalKind;
        CredentialKind = credentialKind;
        CredentialId = credentialId;
        AccountKey = accountKey;
        Person = person;
        ServicePrincipal = servicePrincipal;
        Actor = actor;
        GrantId = grantId;
        Assurance = assurance;
        Client = client;
        Resource = resource;
        Tenant = tenant;
        Organisation = organisation;
        PolicyEpoch = policyEpoch;
        Environment = environment;
        Scopes = scopes;
        Permissions = permissions;
        SenderBinding = senderBinding;
    }

    /// <summary>
    /// Gets the locally assigned contract version.
    /// </summary>
    public string ContractVersion { get; internal init; }

    /// <summary>
    /// Gets the verified principal kind.
    /// </summary>
    public CallerPrincipalKind PrincipalKind { get; internal init; }

    /// <summary>
    /// Gets the verified credential kind.
    /// </summary>
    public CallerCredentialKind CredentialKind { get; internal init; }

    /// <summary>
    /// Gets the non-secret credential identifier.
    /// </summary>
    public CredentialId CredentialId { get; internal init; }

    /// <summary>
    /// Gets the person's account, if applicable.
    /// </summary>
    public CallerAccountKey? AccountKey { get; internal init; }

    /// <summary>
    /// Gets the person, if applicable.
    /// </summary>
    public CallerPersonId? Person { get; internal init; }

    /// <summary>
    /// Gets the service principal, if applicable.
    /// </summary>
    public CallerServicePrincipalId? ServicePrincipal { get; internal init; }

    /// <summary>
    /// Gets the delegated actor, if applicable.
    /// </summary>
    public CallerActor? Actor { get; internal init; }

    /// <summary>
    /// Gets the checked delegation grant, if applicable.
    /// </summary>
    public CallerGrantId? GrantId { get; internal init; }

    /// <summary>
    /// Gets the verified person assurance, if applicable.
    /// </summary>
    public CallerAssurance? Assurance { get; internal init; }

    /// <summary>
    /// Gets client metadata, never implicit actor authority.
    /// </summary>
    public CallerClient? Client { get; internal init; }

    /// <summary>
    /// Gets the checked receiving resource.
    /// </summary>
    public string Resource { get; internal init; }

    /// <summary>
    /// Gets the checked tenant, not a membership authorization.
    /// </summary>
    public TenantId? Tenant { get; internal init; }

    /// <summary>
    /// Gets the confirmed organization binding, if relevant.
    /// </summary>
    public CallerOrganisationId? Organisation { get; internal init; }

    /// <summary>
    /// Gets the verified policy epoch, never independent authorization.
    /// </summary>
    public ulong? PolicyEpoch { get; internal init; }

    /// <summary>
    /// Gets the checked environment.
    /// </summary>
    public string Environment { get; internal init; }

    /// <summary>
    /// Gets the immutable verified scope ceiling.
    /// </summary>
    public IReadOnlySet<string> Scopes { get; internal init; }

    /// <summary>
    /// Gets the immutable explicit permission ceiling.
    /// </summary>
    public IReadOnlySet<string> Permissions { get; internal init; }

    /// <summary>
    /// Gets the checked sender binding, if present.
    /// </summary>
    public CallerSenderBinding? SenderBinding { get; internal init; }
}
