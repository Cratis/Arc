// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Authorization;
using Cratis.Arc.Commands;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Testing.Commands;

/// <summary>Adapts standalone inputs to the real runtime authorization boundary.</summary>
static class CommandAuthorizationEvaluation
{
    /// <summary>Evaluates in a temporary caller scope and translates known runtime refusals.</summary>
    /// <param name="type">The declaration type.</param>
    /// <param name="services">The application scope.</param>
    /// <param name="principal">The explicit caller.</param>
    /// <param name="fixture">Optional unevaluated resource context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The authorization result.</returns>
    internal static async Task<CommandAuthorizationResult> Evaluate(Type type, IServiceProvider services, ClaimsPrincipal principal, CommandContext? fixture, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(principal);
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            return CommandAuthorizationSupport.Inspect(type, services, fixture) ??
                await WithPrincipal(type, services, principal, fixture, cancellationToken);
        }
        catch (Exception exception) when (exception is InvalidAuthorizationConfiguration or AmbiguousAuthorizationLevel)
        {
            return CommandAuthorizationResult.Unsupported(type, "invalid-configuration", "Check authorization declarations and named policy registrations.");
        }
        catch (AsynchronousAuthorizationRequired)
        {
            return CommandAuthorizationResult.Unsupported(type, "async-runtime-required", "Use an asynchronous authorization runtime or a hosted scenario.");
        }
        catch (Exception exception) when (ReadModelKeyResolutionDenied.IsCauseOf(exception))
        {
            return CommandAuthorizationResult.Unsupported(type, "read-model-key-resolution", "Use a command scenario to reproduce read-model key-resolution refusals during binding.");
        }
        catch (Exception exception) when (UnauthorizedCause.IsCauseOf(exception))
        {
            return CommandAuthorizationResult.Evaluated(type, false);
        }
    }

    static async Task<CommandAuthorizationResult> WithPrincipal(Type type, IServiceProvider services, ClaimsPrincipal principal, CommandContext? fixture, CancellationToken cancellationToken)
    {
        using var principalScope = services.GetRequiredService<ICurrentPrincipalOverride>().BeginScope(principal);
        if (!ReferenceEquals(services.GetRequiredService<ICurrentPrincipalAccessor>().Current, principal))
        {
            return CommandAuthorizationResult.Unsupported(type, "principal-unavailable", "Use compatible Arc principal override and accessor registrations.");
        }

        var declaration = services.GetRequiredService<AuthorizationDeclarations>().For(type);
        if (declaration.Requirements.Any(requirement => requirement.AuthenticationSchemes.Count > 0))
        {
            return CommandAuthorizationResult.Unsupported(type, "host-required", "Use a hosted scenario for named authentication schemes.");
        }

        if (fixture is null && declaration.RequiresAsynchronousEvaluation)
        {
            return CommandAuthorizationResult.Unsupported(type, "resource-required", "Supply a command instance or explicit context for asynchronous requirements.");
        }

        var evaluation = services.GetRequiredService<AuthorizationEvaluation>();
        var prepared = await evaluation.Prepare(type, services, cancellationToken);
        if (prepared.SelectedPrincipal?.Identity?.IsAuthenticated == true && !ReferenceEquals(prepared.SelectedPrincipal, principal))
        {
            return CommandAuthorizationResult.Unsupported(type, "principal-selection", "Use a hosted scenario for a selected authenticated identity.");
        }

        // Deliberately no host: the pipeline's scope factory is unavailable to a standalone adapter.
        using var caller = AuthorizationCommandIdentity.Enter(principal, host: null, CommandCallerProvenance.Ambient);
        object resource = fixture is null ? new TypeOnlyResource() : FreshContext(fixture, services, prepared, cancellationToken);
        var authorized = await evaluation.IsAuthorized(type, resource, services, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        return CommandAuthorizationResult.Evaluated(type, authorized);
    }

    static CommandContext FreshContext(CommandContext fixture, IServiceProvider services, PreparedAuthorization prepared, CancellationToken cancellationToken) =>
        new(fixture.CorrelationId, fixture.Type, fixture.Command, fixture.Dependencies, fixture.Values, ServiceProvider: services)
        {
            ReceivedAt = fixture.ReceivedAt,
            AllowedSeverity = fixture.AllowedSeverity,
            BlockUnknownValidationSeverity = fixture.BlockUnknownValidationSeverity,
            CancellationToken = cancellationToken,
            PreparedAuthorization = prepared
        };

    sealed record TypeOnlyResource;
}
