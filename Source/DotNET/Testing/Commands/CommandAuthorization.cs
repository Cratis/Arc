// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Commands;
using Cratis.Execution;

namespace Cratis.Arc.Testing.Commands;

/// <summary>Evaluates real Arc authorization without executing the command pipeline.</summary>
/// <remarks>
/// Use a dedicated application-configured scope. Policies and lazy context providers may execute application code.
/// Hosted authentication, pipeline binding and tenant-membership verification are not reproduced.
/// In particular, this helper does not reproduce read-model key-resolution refusals during binding.
/// Authorized means declared authorization passed; it does not imply the command will run.
/// </remarks>
public static class CommandAuthorization
{
    /// <summary>Evaluates a declaration without constructing a command. Async requirements need an instance instead.</summary>
    /// <typeparam name="TCommand">The command type.</typeparam>
    /// <param name="services">The caller-owned, dedicated scoped provider.</param>
    /// <param name="principal">The explicit caller.</param>
    /// <param name="cancellationToken">The evaluation cancellation token.</param>
    /// <returns>The runtime verdict or an unsupported diagnostic.</returns>
    public static Task<CommandAuthorizationResult> For<TCommand>(IServiceProvider services, ClaimsPrincipal principal, CancellationToken cancellationToken = default) =>
        CommandAuthorizationEvaluation.Evaluate(typeof(TCommand), services, principal, null, cancellationToken);

    /// <summary>Evaluates a command instance without binding, validating or handling it.</summary>
    /// <typeparam name="TCommand">The command type.</typeparam>
    /// <param name="services">The caller-owned, dedicated scoped provider.</param>
    /// <param name="principal">The explicit caller.</param>
    /// <param name="command">The resource for policy evaluation.</param>
    /// <param name="cancellationToken">The evaluation cancellation token.</param>
    /// <returns>The runtime verdict or an unsupported diagnostic.</returns>
    public static async Task<CommandAuthorizationResult> For<TCommand>(IServiceProvider services, ClaimsPrincipal principal, TCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(services);
        using var receipt = OperationContextScope.Begin(services);
        var context = new CommandContext(CorrelationId.New(), typeof(TCommand), command!, [], CommandContextValues.Empty, ServiceProvider: services)
        {
            ReceivedAt = OperationContextScope.Current ?? default
        };
        return await ForContext<TCommand>(services, principal, context, cancellationToken);
    }

    /// <summary>Evaluates explicit context fixtures in a fresh authorization context.</summary>
    /// <typeparam name="TCommand">The command type.</typeparam>
    /// <param name="services">The caller-owned, dedicated scoped provider.</param>
    /// <param name="principal">The explicit caller.</param>
    /// <param name="context">The command, dependencies, values, time and severity fixtures. Must not have been authorized.</param>
    /// <param name="cancellationToken">The evaluation cancellation token.</param>
    /// <returns>The runtime verdict or an unsupported diagnostic.</returns>
    public static Task<CommandAuthorizationResult> ForContext<TCommand>(IServiceProvider services, ClaimsPrincipal principal, CommandContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        return CommandAuthorizationEvaluation.Evaluate(typeof(TCommand), services, principal, context, cancellationToken);
    }
}
