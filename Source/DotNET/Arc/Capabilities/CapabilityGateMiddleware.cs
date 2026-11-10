// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using System.Text.Json;
using Cratis.Arc.AspNetCore.Http;
using Cratis.Arc.Identity.Callers;
using Cratis.Arc.Tenancy;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Capabilities;

/// <summary>Requires a verified caller for every request on a capability-group port.</summary>
/// <param name="arc">The endpoint-group port assignments.</param>
/// <param name="capabilities">The capability admission options.</param>
/// <param name="logger">The refusal logger.</param>
/// <param name="selection">The startup endpoint selection.</param>
internal sealed class CapabilityGateMiddleware(
    IOptions<ArcOptions> arc,
    IOptions<CapabilitiesOptions> capabilities,
    ILogger<CapabilityGateMiddleware> logger,
    CapabilityEndpointSelection selection) : IMiddleware
{
    static readonly Endpoint _outsideRefusal = new(context => Refuse(context, StatusCodes.Status401Unauthorized), EndpointMetadataCollection.Empty, "Capability admission refused");
    readonly IReadOnlySet<CallerPrincipalKind> _allowed = capabilities.Value.AllowedPrincipalKinds.Select(kind => kind switch
    {
        CapabilityPrincipalKind.Person => CallerPrincipalKind.Person,
        CapabilityPrincipalKind.DelegatedActor => CallerPrincipalKind.DelegatedActor,
        CapabilityPrincipalKind.OrganizationAutomation => CallerPrincipalKind.OrganizationAutomation,
        CapabilityPrincipalKind.ClusterWorkload => CallerPrincipalKind.ClusterWorkload,
        CapabilityPrincipalKind.Runner => CallerPrincipalKind.Runner,
        _ => throw new ArgumentOutOfRangeException(nameof(capabilities))
    }).ToHashSet();

    /// <inheritdoc/>
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var group = capabilities.Value.EndpointGroup;
        if (TryBypassGate(context, group))
        {
            await next(context);
            return;
        }

        // Capture tenant inputs before workload routing discards the incoming product principal.
        var workloadTenantAllowed = AllowsWorkloadTenant(context);
        var resolution = await ResolveCapabilityCaller(context);
        if (resolution.Caller is not { } caller)
        {
            var refusal = resolution.Refusal!.Value;
            logger.CallerRefused(refusal);
            await Refuse(context, new CapabilityRefusal(refusal.HttpStatus(), refusal));
            return;
        }
        if (!CallerAdmission.Allows(caller, _allowed) || !workloadTenantAllowed)
        {
            await Refuse(context, new CapabilityRefusal(StatusCodes.Status403Forbidden, null));
            return;
        }

        if (await ConflictsWithAnotherPrincipal(context, caller))
        {
            logger.CallerRefused(CallerRefusal.AmbiguousPrincipal);
            await Refuse(context, new CapabilityRefusal(CallerRefusal.AmbiguousPrincipal.HttpStatus(), CallerRefusal.AmbiguousPrincipal));
            return;
        }

        // The verified principal is built here and never registered as an authentication scheme, so the host's own
        // default scheme selection is untouched. Person claims transformations do not apply to workloads.
        var principal = await CreatePrincipal(context, caller);

        if (!VerifiedCallerPrincipal.Carries(principal, caller))
        {
            // A claims transformation or another component dropped the verified identity: the principal cannot be proved.
            logger.CallerRefused(CallerRefusal.AmbiguousPrincipal);
            await Refuse(context, new CapabilityRefusal(CallerRefusal.AmbiguousPrincipal.HttpStatus(), CallerRefusal.AmbiguousPrincipal));
            return;
        }

        if (!MembershipAllows(context, caller, principal))
        {
            await Refuse(context, new CapabilityRefusal(StatusCodes.Status403Forbidden, null));
            return;
        }

        using var tenantScope = BeginWorkloadTenantScope(context, caller);
        context.User = principal;
        context.Items[VerifiedCallerPrincipal.ItemKey] = principal;
        context.Items[CapabilityCallers.ItemKey] = CapabilityCallerProjection.From(caller, group, CapabilityProtocol.Rest);
        await next(context);
    }

    static bool HasNoResolvedTenant(HttpContext context)
    {
        // Candidate avoids person membership checks and includes cached and explicitly selected tenants.
        // An overridden accessor cannot be pinned by Arc and must not admit a workload request.
        var accessor = context.RequestServices.GetRequiredService<ITenantIdAccessor>();
        if (accessor is not TenantIdAccessor tenants || accessor.GetType() != typeof(TenantIdAccessor)) return false;
        var resolved = context.RequestServices.GetRequiredService<ITenantIdResolver>().Resolve();
        return tenants.Candidate == TenantId.NotSet && IsUnsetTenant(resolved);
    }

    static bool IsUnsetTenant(string? tenant) => string.IsNullOrEmpty(tenant) || tenant == TenantId.NotSet.Value;

    static IDisposable? BeginWorkloadTenantScope(HttpContext context, VerifiedCaller caller)
    {
        if (caller.PrincipalKind != CallerPrincipalKind.ClusterWorkload) return null;
        return context.RequestServices.GetRequiredService<TenantIdAccessor>().UseAuthorizedTenant(TenantId.NotSet);
    }

    static async Task<ClaimsPrincipal> CreatePrincipal(HttpContext context, VerifiedCaller caller)
    {
        var principal = VerifiedCallerPrincipal.Create(caller);
        if (caller.PrincipalKind == CallerPrincipalKind.ClusterWorkload) return principal;
        if (context.RequestServices.GetService<IClaimsTransformation>() is { } transformation) return await transformation.TransformAsync(principal);
        return principal;
    }

    static Task<CallerResolution> ResolveCapabilityCaller(HttpContext context)
    {
        if (context.RequestServices.GetService<WorkloadCallerInstallation>() is not null)
        {
            context.Items[ExpectedCredentialShape.Key] = new ExpectedCredentialShape(PresentedCredentialShape.ServiceAccount);
            context.User = new ClaimsPrincipal(new ClaimsIdentity());
        }
        else if (context.RequestServices.GetService<PatCapableCallerInstallation>() is not null)
        {
            var presentation = context.RequestServices.GetRequiredService<CredentialPresentationGate>().Inspect(new AspNetCoreHttpRequestContext(context));
            if (presentation.Refusal is not null || !AdmitsPresentation(context.GetEndpoint(), presentation.Shape))
            {
                return Task.FromResult(new CallerResolution(null, CallerRefusal.SourceRefused));
            }
        }
        return context.RequestServices.GetRequiredService<IVerifiedCallerAccessor>().GetCurrent();
    }

    static bool AdmitsPresentation(Endpoint? endpoint, PresentedCredentialShape? shape) =>
        shape == PresentedCredentialShape.PatBearer ||
        (shape == PresentedCredentialShape.OAuthBearer && endpoint?.Metadata.GetMetadata<AcceptsOAuthCapability>() is not null);

    static bool MembershipAllows(HttpContext context, VerifiedCaller caller, ClaimsPrincipal principal)
    {
        if (caller.PrincipalKind == CallerPrincipalKind.ClusterWorkload) return true;

        // The product's membership check runs here, once, for the verified principal and the caller's checked tenant.
        try
        {
            _ = context.RequestServices.GetService<TenantMembershipVerification>()?.Verify(caller.Tenant ?? TenantId.NotSet, principal);
            return true;
        }
        catch (TenantMembershipDenied)
        {
            return false;
        }
    }

    static async Task<bool> ConflictsWithAnotherPrincipal(HttpContext context, VerifiedCaller caller)
    {
        if (caller.PrincipalKind == CallerPrincipalKind.ClusterWorkload) return false;
        var subject = VerifiedCallerIdentity.SubjectOf(caller);
        var others = new List<ClaimsPrincipal>();
        if (context.User.Identities.Any(identity => identity.IsAuthenticated))
        {
            others.Add(context.User);
        }

        var defaultScheme = await context.RequestServices.GetRequiredService<IAuthenticationSchemeProvider>().GetDefaultAuthenticateSchemeAsync();
        if (defaultScheme is not null)
        {
            var result = await context.AuthenticateAsync(defaultScheme.Name);
            if (result.Succeeded && result.Principal is { } principal)
            {
                others.Add(principal);
            }
        }

        return others.Exists(other =>
        {
            var otherSubject = (other.FindFirst("sub") ?? other.FindFirst(ClaimTypes.NameIdentifier))?.Value;
            return otherSubject is null || !string.Equals(otherSubject, subject, StringComparison.Ordinal);
        });
    }

    static async Task Refuse(HttpContext context, CapabilityRefusal refusal)
    {
        if (context.GetEndpoint()?.Metadata.GetMetadata<ICapabilityRefusalWriter>() is not { } writer)
        {
            await Refuse(context, refusal.StatusCode);
            return;
        }

        // The refusal status is in place before the writer runs, so a writer that only writes a body still refuses.
        context.Response.StatusCode = refusal.StatusCode;

        // A custom writer must never turn a refusal into a success, even when it set a 2xx and started writing:
        // the status is forced at the moment the response starts.
        context.Response.OnStarting(
            static state =>
            {
                var (response, status) = ((HttpResponse, int))state;
                if (response.StatusCode < StatusCodes.Status400BadRequest)
                {
                    response.StatusCode = status;
                }

                return Task.CompletedTask;
            },
            (context.Response, refusal.StatusCode));
        await writer.Write(context, refusal);
    }

    static async Task Refuse(HttpContext context, int status)
    {
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";
        if (status == StatusCodes.Status401Unauthorized)
        {
            context.Response.Headers.WWWAuthenticate = "Bearer";
        }
        await using var writer = new Utf8JsonWriter(context.Response.BodyWriter);
        writer.WriteStartObject();
        writer.WriteString("type", "about:blank");
        var title = status switch
        {
            StatusCodes.Status400BadRequest => "Bad Request",
            StatusCodes.Status403Forbidden => "Forbidden",
            _ => "Unauthorized"
        };
        writer.WriteString("title", title);
        writer.WriteNumber("status", status);
        writer.WriteEndObject();
        await writer.FlushAsync(context.RequestAborted);
        await context.Response.BodyWriter.FlushAsync(context.RequestAborted);
    }

    bool AllowsWorkloadTenant(HttpContext context)
    {
        if (context.RequestServices.GetService<WorkloadCallerInstallation>() is null) return true;

        if (HasPresentedTenant(context)) return false;
        return HasNoResolvedTenant(context);
    }

    bool HasPresentedTenant(HttpContext context)
    {
        var tenancy = arc.Value.Tenancy;
        return context.Request.Headers.ContainsKey(tenancy.HttpHeader) ||
            context.Request.Query.ContainsKey(tenancy.QueryParameter) ||
            context.User.HasClaim(claim => claim.Type == tenancy.ClaimType);
    }

    bool TryBypassGate(HttpContext context, string group)
    {
        if (!arc.Value.EndpointGroups.PortsFor(group).Contains(context.Connection.LocalPort))
        {
            return true;
        }
        var endpoint = context.GetEndpoint();
        if (endpoint is null || !selection.IsOutside(endpoint))
        {
            return false;
        }
        context.Features.Set<IEndpointFeature>(new CapabilityOutsideEndpointFeature(selection, endpoint, _outsideRefusal));
        return true;
    }
}
