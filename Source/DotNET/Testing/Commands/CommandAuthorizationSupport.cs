// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Arc.Commands;
using Cratis.Arc.Http;
using Cratis.Arc.Identity.Approvals;
using Cratis.Arc.Tenancy;
using Cratis.Types;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Testing.Commands;

/// <summary>Identifies configuration that standalone authorization cannot reproduce.</summary>
static class CommandAuthorizationSupport
{
    static readonly Type[] _requiredServices =
    [
        typeof(AuthorizationEvaluation), typeof(AuthorizationDeclarations), typeof(IAuthorizationEvaluator),
        typeof(IAuthorizationPolicyRuntime), typeof(ICurrentPrincipalAccessor), typeof(ICurrentPrincipalOverride)
    ];

    /// <summary>Checks provider and context compatibility without invoking pipeline hooks.</summary>
    /// <param name="type">The command type.</param>
    /// <param name="services">The application scope.</param>
    /// <param name="fixture">Optional resource context.</param>
    /// <returns>An unsupported diagnostic, or null for a supported configuration.</returns>
    internal static CommandAuthorizationResult? Inspect(Type type, IServiceProvider services, CommandContext? fixture)
    {
        if (fixture is not null && InvalidFixture(type, services, fixture))
        {
            return CommandAuthorizationResult.Unsupported(type, "context-invalid", "Supply a matching command and an unevaluated context owned by this provider.");
        }

        if (ApprovalGate.RequiresApproval(type))
        {
            return CommandAuthorizationResult.Unsupported(type, "approval-required", "Use a command scenario for commands requiring approval, whether or not a flow is registered.");
        }

        return InspectServices(type, services) ?? InspectPipeline(type, services);
    }

    static CommandAuthorizationResult? InspectServices(Type type, IServiceProvider services)
    {
        if (services.GetService<IHttpRequestContextAccessor>()?.Current is not null)
        {
            return CommandAuthorizationResult.Unsupported(type, "host-required", "Use a hosted scenario for an active HTTP request.");
        }

        var probe = services.GetService<IServiceProviderIsService>();
        if (probe is null || _requiredServices.Any(service => !probe.IsService(service)))
        {
            return CommandAuthorizationResult.Unsupported(type, "services-missing", "Register Arc authorization services in a dedicated application scope.");
        }

        if (services.GetRequiredService<IAuthorizationPolicyRuntime>().GetType() != typeof(ArcAuthorizationPolicyRuntime))
        {
            return CommandAuthorizationResult.Unsupported(type, "runtime-unsupported", "Use a hosted scenario for host or custom authorization runtimes.");
        }

        return null;
    }

    static CommandAuthorizationResult? InspectPipeline(Type type, IServiceProvider services)
    {
        var probe = services.GetRequiredService<IServiceProviderIsService>();
        var memberships = services.GetService<IInstancesOf<ITenantMembership>>();
        if (probe.IsService(typeof(ITenantMembership)) || memberships?.Any() == true)
        {
            return CommandAuthorizationResult.Unsupported(type, "tenant-membership", "Use a hosted scenario to verify tenant membership and operation snapshots.");
        }

        return CommandAuthorizationFilters.Inspect(type, services);
    }

    static bool InvalidFixture(Type type, IServiceProvider services, CommandContext fixture) =>
        fixture.Type != type || fixture.Command is null || fixture.Command.GetType() != type ||
        (fixture.ServiceProvider is not null && !ReferenceEquals(fixture.ServiceProvider, services)) ||
        fixture.PreparedAuthorization is not null || fixture.AuthorizedPrincipal is not null;
}
