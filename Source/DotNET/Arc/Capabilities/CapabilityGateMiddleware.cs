// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using System.Text.Json;
using Cratis.Arc.Identity.Callers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Capabilities;

/// <summary>Requires a verified caller for every request on a capability-group port.</summary>
/// <param name="arc">The endpoint-group port assignments.</param>
/// <param name="capabilities">The capability admission options.</param>
/// <param name="logger">The refusal logger.</param>
internal sealed class CapabilityGateMiddleware(
    IOptions<ArcOptions> arc,
    IOptions<CapabilitiesOptions> capabilities,
    ILogger<CapabilityGateMiddleware> logger) : IMiddleware
{
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
        if (!arc.Value.EndpointGroups.PortsFor(group).Contains(context.Connection.LocalPort))
        {
            await next(context);
            return;
        }

        var resolution = await context.RequestServices.GetRequiredService<IVerifiedCallerAccessor>().GetCurrent();
        if (resolution.Caller is not { } caller)
        {
            var refusal = resolution.Refusal!.Value;
            logger.CallerRefused(refusal);
            await Refuse(context, refusal.HttpStatus());
            return;
        }
        if (!CallerAdmission.Allows(caller, _allowed))
        {
            await Refuse(context, StatusCodes.Status403Forbidden);
            return;
        }

        if (await ConflictsWithAnotherPrincipal(context, caller))
        {
            logger.CallerRefused(CallerRefusal.AmbiguousPrincipal);
            await Refuse(context, CallerRefusal.AmbiguousPrincipal.HttpStatus());
            return;
        }

        context.Items[VerifiedCallerPrincipal.CallerItemKey] = caller;
        var result = await context.AuthenticateAsync(VerifiedCallerAuthentication.Scheme);
        if (!result.Succeeded || !VerifiedCallerPrincipal.Carries(result.Principal, caller))
        {
            // A claims transformation or another component dropped the verified identity: the principal cannot be proved.
            logger.CallerRefused(CallerRefusal.AmbiguousPrincipal);
            await Refuse(context, CallerRefusal.AmbiguousPrincipal.HttpStatus());
            return;
        }

        context.User = result.Principal!;
        context.Items[VerifiedCallerPrincipal.ItemKey] = result.Principal;
        context.Items[CapabilityCallers.ItemKey] = CapabilityCallerProjection.From(caller, group, CapabilityProtocol.Rest);
        await next(context);
    }

    static async Task<bool> ConflictsWithAnotherPrincipal(HttpContext context, VerifiedCaller caller)
    {
        var subject = VerifiedCallerIdentity.SubjectOf(caller);
        var others = new List<ClaimsPrincipal>();
        if (context.User.Identities.Any(identity => identity.IsAuthenticated))
        {
            others.Add(context.User);
        }

        var defaultScheme = await context.RequestServices.GetRequiredService<IAuthenticationSchemeProvider>().GetDefaultAuthenticateSchemeAsync();
        if (defaultScheme is not null && !string.Equals(defaultScheme.Name, VerifiedCallerAuthentication.Scheme, StringComparison.Ordinal))
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
}
