// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Capabilities;
using Cratis.Arc.Tenancy;
using Cratis.Chronicle.Auditing;

namespace Cratis.Arc.Chronicle.Capabilities;

/// <summary>
/// Describes the verified capability caller recorded on an event's causation chain.
/// </summary>
public static class CapabilityCausation
{
    /// <summary>The receiving endpoint group property.</summary>
    public const string EndpointGroupProperty = "endpointGroup";

    /// <summary>The capability protocol property.</summary>
    public const string ProtocolProperty = "protocol";

    /// <summary>The verified principal kind property.</summary>
    public const string PrincipalKindProperty = "principalKind";

    /// <summary>The product person reference property.</summary>
    public const string PersonProperty = "person";

    /// <summary>The current actor issuer property.</summary>
    public const string ActorIssuerProperty = "actorIssuer";

    /// <summary>The current actor subject property.</summary>
    public const string ActorSubjectProperty = "actorSubject";

    /// <summary>The client identifier property.</summary>
    public const string ClientIdProperty = "clientId";

    /// <summary>The client authentication property, declared or authenticated.</summary>
    public const string ClientAuthenticationProperty = "clientAuthentication";

    /// <summary>The non-secret credential identifier property.</summary>
    public const string CredentialIdProperty = "credentialId";

    /// <summary>The checked delegation grant identifier property.</summary>
    public const string GrantIdProperty = "grantId";

    /// <summary>The verified tenant property.</summary>
    public const string TenantProperty = "tenant";

    /// <summary>The capability causation type.</summary>
    public static readonly CausationType Type = "Capability";

    /// <summary>
    /// Gets causation properties for a capability caller, omitting absent values and secrets.
    /// </summary>
    /// <param name="caller">The admitted capability caller.</param>
    /// <returns>The properties to record with the capability causation.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The caller principal kind is unknown.</exception>
    public static IDictionary<string, string> PropertiesFor(CapabilityCaller caller)
    {
        var properties = new Dictionary<string, string>
        {
            [EndpointGroupProperty] = caller.EndpointGroup,
            [ProtocolProperty] = caller.Protocol,
            [PrincipalKindProperty] = KindName(caller.PrincipalKind),
            [CredentialIdProperty] = caller.CredentialId
        };
        if (caller.Person is { } person) properties[PersonProperty] = person;
        if (caller.Actor is { } actor)
        {
            properties[ActorIssuerProperty] = actor.Issuer;
            properties[ActorSubjectProperty] = actor.Subject;
        }
        if (caller.Client is { } client)
        {
            properties[ClientIdProperty] = client.Id;
            properties[ClientAuthenticationProperty] = client.IsAuthenticated ? "authenticated" : "declared";
        }
        if (caller.GrantId is { } grant) properties[GrantIdProperty] = grant;
        if (caller.Tenant is { } tenant && tenant != TenantId.NotSet) properties[TenantProperty] = tenant.Value;
        return properties;
    }

    static string KindName(CapabilityPrincipalKind kind) => kind switch
    {
        CapabilityPrincipalKind.Person => "person",
        CapabilityPrincipalKind.DelegatedActor => "delegated-actor",
        CapabilityPrincipalKind.OrganizationAutomation => "organization-automation",
        CapabilityPrincipalKind.ClusterWorkload => "cluster-workload",
        CapabilityPrincipalKind.Runner => "runner",
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };
}
