// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Arc.Commands;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Identity.Approvals;

/// <summary>
/// Enforces approval requirements before command handler work begins.
/// </summary>
internal static class ApprovalGate
{
    /// <summary>
    /// The reason returned when approval cannot be initiated.
    /// </summary>
    internal const string RefusalReason = "Approval required: this command must be approved before it can run, and no approval package is registered.";

    static readonly ConcurrentDictionary<Type, bool> _requiresApproval = new();

    /// <summary>
    /// Checks the command type and its ancestors for an approval requirement.
    /// </summary>
    /// <param name="commandType">The command type to inspect.</param>
    /// <returns>Whether approval is required.</returns>
    internal static bool RequiresApproval(Type commandType) => _requiresApproval.GetOrAdd(commandType, type => type.IsDefined(typeof(RequiresApprovalAttribute), inherit: true));

    /// <summary>
    /// Creates an authorization refusal for a command requiring approval.
    /// </summary>
    /// <param name="correlationId">The command correlation ID.</param>
    /// <returns>The refusal result.</returns>
    internal static CommandResult Refusal(CorrelationId correlationId) => CommandResult.Unauthorized(correlationId, RefusalReason);

    /// <summary>
    /// Initiates approval instead of invoking a marked command's handler.
    /// </summary>
    /// <param name="context">The authorized and validated command context.</param>
    /// <param name="services">The command's scoped services.</param>
    /// <returns>The approval result, or null for an unmarked command.</returns>
    internal static async Task<CommandResult?> ForExecution(CommandContext context, IServiceProvider services)
    {
        if (!RequiresApproval(context.Type)) return null;
        var refusal = TryResolveFlows(context.Type, context.CorrelationId, services, out var flows, out var logger);
        if (refusal is not null) return refusal;

        try
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            var result = await flows[0].Begin(context);
            if (result is not null) return result;
            logger?.ApprovalFlowFailed(new InvalidOperationException(), context.Type.FullName ?? context.Type.Name);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger?.ApprovalFlowFailed(exception, context.Type.FullName ?? context.Type.Name);
        }

        return CommandResult.AuthorizationConfigurationFailure(context.CorrelationId);
    }

    /// <summary>
    /// Checks approval configuration without initiating a flow during validation.
    /// </summary>
    /// <param name="context">The authorized and validated command context.</param>
    /// <param name="services">The command's scoped services.</param>
    /// <returns>A refusal, or null when validation can proceed.</returns>
    internal static CommandResult? ForValidation(CommandContext context, IServiceProvider services)
    {
        if (!RequiresApproval(context.Type)) return null;
        return TryResolveFlows(context.Type, context.CorrelationId, services, out _, out _);
    }

    static CommandResult? TryResolveFlows(Type commandType, CorrelationId correlationId, IServiceProvider services, out IApprovalFlow[] flows, out ILogger<CommandPipeline>? logger)
    {
        flows = [];
        logger = null;
        var name = commandType.FullName ?? commandType.Name;
        try
        {
            logger = services.GetService<ILogger<CommandPipeline>>();
            flows = (services.GetService(typeof(IEnumerable<IApprovalFlow>)) as IEnumerable<IApprovalFlow>)?.ToArray() ?? [];
            if (flows.Length == 1) return null;
            if (flows.Length > 1)
            {
                logger?.AmbiguousApprovalFlows(name);
                return CommandResult.AuthorizationConfigurationFailure(correlationId);
            }

            logger?.ApprovalRequiredWithoutPackage(name);
            return Refusal(correlationId);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger?.ApprovalFlowFailed(exception, name);
            return CommandResult.AuthorizationConfigurationFailure(correlationId);
        }
    }
}
