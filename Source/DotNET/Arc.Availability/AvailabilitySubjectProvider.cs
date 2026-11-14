// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Authorization;
using Cratis.Arc.Tenancy;
using Cratis.Choreography.Availability.Audiences;

namespace Cratis.Arc.Availability;

/// <summary>
/// Represents the default <see cref="IAvailabilitySubjectProvider"/>: the tenant of the request and the subject of the
/// current principal and, for a system caller, the client id it authenticated with.
/// </summary>
/// <param name="tenantIdAccessor">The <see cref="ITenantIdAccessor"/>.</param>
/// <param name="principalAccessor">The <see cref="ICurrentPrincipalAccessor"/>.</param>
public class AvailabilitySubjectProvider(ITenantIdAccessor tenantIdAccessor, ICurrentPrincipalAccessor principalAccessor) : IAvailabilitySubjectProvider
{
    /// <summary>
    /// The claim a client-credentials token carries for the client that obtained it.
    /// </summary>
    public const string ClientIdClaim = "client_id";

    /// <summary>
    /// The claim read when <see cref="ClientIdClaim"/> is absent: the party the token was issued to.
    /// </summary>
    public const string AuthorizedPartyClaim = "azp";

    /// <inheritdoc/>
    public AudienceSubject GetSubject()
    {
        var tenant = tenantIdAccessor.Current;
        var principal = principalAccessor.Current;
        var isSystem = principal?.Identity?.AuthenticationType == SystemPrincipal.AuthenticationType;
        var user = principal is { Identity.IsAuthenticated: true } && !isSystem
            ? principal.FindFirst("sub")?.Value ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? principal.Identity.Name
            : null;

        return new AudienceSubject(tenant == TenantId.NotSet ? null : tenant.Value, user, isSystem)
        {
            SystemClientId = isSystem ? GetClientId(principal!) : string.Empty
        };
    }

    static string GetClientId(ClaimsPrincipal principal) =>
        principal.FindFirst(ClientIdClaim)?.Value ?? principal.FindFirst(AuthorizedPartyClaim)?.Value ?? string.Empty;
}
