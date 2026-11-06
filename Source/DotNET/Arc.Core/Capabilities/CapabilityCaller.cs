// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Tenancy;

namespace Cratis.Arc.Capabilities;

/// <summary>
/// Represents the verified caller admitted to a capability endpoint group.
/// </summary>
/// <param name="EndpointGroup">The receiving endpoint group.</param>
/// <param name="Protocol">The capability protocol.</param>
/// <param name="PrincipalKind">The verified principal category.</param>
/// <param name="Person">The product person reference, if applicable.</param>
/// <param name="Actor">The current delegated actor, if applicable.</param>
/// <param name="Client">The client metadata, if present.</param>
/// <param name="CredentialId">The non-secret verified credential identifier.</param>
/// <param name="GrantId">The checked grant identifier, if applicable.</param>
/// <param name="Tenant">The verified tenant, if applicable.</param>
/// <param name="Scopes">The verified scope ceiling, not independent authorization.</param>
public sealed record CapabilityCaller(
    string EndpointGroup,
    string Protocol,
    CapabilityPrincipalKind PrincipalKind,
    string? Person,
    CapabilityActor? Actor,
    CapabilityClient? Client,
    string CredentialId,
    string? GrantId,
    TenantId? Tenant,
    IReadOnlySet<string> Scopes);
