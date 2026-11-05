// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;

namespace Cratis.Arc.Capabilities;

/// <summary>Projects the verified caller onto the public capability contract.</summary>
internal static class CapabilityCallerProjection
{
    /// <summary>Creates the caller projection for a receiving capability group.</summary>
    /// <param name="caller">The verified caller.</param>
    /// <param name="endpointGroup">The receiving endpoint group.</param>
    /// <param name="protocol">The capability protocol.</param>
    /// <returns>The public caller projection.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The caller principal kind is unknown.</exception>
    internal static CapabilityCaller From(VerifiedCaller caller, string endpointGroup, string protocol)
    {
        return new(
            endpointGroup,
            protocol,
            KindFor(caller.PrincipalKind),
            caller.Person?.Value,
            caller.Actor is { } actor ? new(actor.Issuer, actor.Subject) : null,
            caller.Client is { } client ? new(client.Id, client.Authentication == CallerClientAuthentication.Authenticated) : null,
            caller.CredentialId.Value,
            caller.GrantId?.Value,
            caller.Tenant,
            caller.Scopes);
    }

    static CapabilityPrincipalKind KindFor(CallerPrincipalKind kind) => kind switch
    {
        CallerPrincipalKind.Person => CapabilityPrincipalKind.Person,
        CallerPrincipalKind.DelegatedActor => CapabilityPrincipalKind.DelegatedActor,
        CallerPrincipalKind.OrganizationAutomation => CapabilityPrincipalKind.OrganizationAutomation,
        CallerPrincipalKind.ClusterWorkload => CapabilityPrincipalKind.ClusterWorkload,
        CallerPrincipalKind.Runner => CapabilityPrincipalKind.Runner,
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };
}
