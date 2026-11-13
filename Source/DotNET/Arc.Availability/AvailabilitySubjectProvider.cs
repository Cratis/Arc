// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Authorization;
using Cratis.Arc.Tenancy;
using Cratis.Choreography.Availability.Audiences;

namespace Cratis.Arc.Availability;

/// <summary>
/// Represents the default <see cref="IAvailabilitySubjectProvider"/>: the tenant of the request and the subject of the
/// current principal.
/// </summary>
/// <param name="tenantIdAccessor">The <see cref="ITenantIdAccessor"/>.</param>
/// <param name="principalAccessor">The <see cref="ICurrentPrincipalAccessor"/>.</param>
public class AvailabilitySubjectProvider(ITenantIdAccessor tenantIdAccessor, ICurrentPrincipalAccessor principalAccessor) : IAvailabilitySubjectProvider
{
    /// <inheritdoc/>
    public AudienceSubject GetSubject()
    {
        var tenant = tenantIdAccessor.Current;
        var principal = principalAccessor.Current;
        var isSystem = principal?.Identity?.AuthenticationType == SystemPrincipal.AuthenticationType;
        var user = principal is { Identity.IsAuthenticated: true } && !isSystem
            ? principal.FindFirst("sub")?.Value ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? principal.Identity.Name
            : null;

        return new AudienceSubject(tenant == TenantId.NotSet ? null : tenant.Value, user, isSystem);
    }
}
