// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Tenancy;

namespace Cratis.Arc.Identity.Callers;

/// <summary>A framework-owned immutable caller. Collection values are frozen copies, never verifier-owned mutable sets.</summary>
/// <remarks>Internal until Cratis/Arc#3701 locks; then publish.</remarks>
/// <param name="ContractVersion">The locally assigned contract version.</param>
/// <param name="PrincipalKind">The verified principal kind.</param>
/// <param name="CredentialKind">The verified credential kind.</param>
/// <param name="CredentialId">The verified credential identifier.</param>
/// <param name="AccountKey">The person's account, if applicable.</param>
/// <param name="Person">The person, if applicable.</param>
/// <param name="ServicePrincipal">The service, if applicable.</param>
/// <param name="Actor">The delegated actor, if applicable.</param>
/// <param name="GrantId">The checked delegation grant, if applicable.</param>
/// <param name="Assurance">The verified person assurance, if applicable.</param>
/// <param name="Client">The client metadata, never implicit actor authority.</param>
/// <param name="Resource">The checked receiving resource.</param>
/// <param name="Tenant">The checked tenant.</param>
/// <param name="Organisation">The confirmed organization binding, if relevant.</param>
/// <param name="PolicyEpoch">The verified nonnegative policy epoch, never independent authorization.</param>
/// <param name="Environment">The checked environment.</param>
/// <param name="Scopes">The immutable verified scope ceiling.</param>
/// <param name="Permissions">The immutable explicit permission ceiling.</param>
/// <param name="SenderBinding">The checked sender binding, if present.</param>
internal sealed record VerifiedCaller(
    string ContractVersion,
    CallerPrincipalKind PrincipalKind,
    CallerCredentialKind CredentialKind,
    CredentialId CredentialId,
    CallerAccountKey? AccountKey,
    CallerPersonId? Person,
    CallerServicePrincipalId? ServicePrincipal,
    CallerActor? Actor,
    CallerGrantId? GrantId,
    CallerAssurance? Assurance,
    CallerClient? Client,
    string Resource,
    TenantId? Tenant,
    CallerOrganisationId? Organisation,
    ulong? PolicyEpoch,
    string Environment,
    IReadOnlySet<string> Scopes,
    IReadOnlySet<string> Permissions,
    CallerSenderBinding? SenderBinding);
