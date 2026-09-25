// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;

/// <summary>
/// Observes which tenant a transaction-like execution scope would bind at Begin and Complete.
/// </summary>
/// <param name="observations">The callback observations.</param>
public class SelectedTenantCommandScope(TenantCommandObservations observations) : ICommandOperationExecutionScope
{
    /// <inheritdoc/>
    public bool IsCommitParticipant => false;

    /// <inheritdoc/>
    public CommandCommitDisposition GetCommitDisposition(CommandContext context) => CommandCommitDisposition.NoCommit;

    /// <inheritdoc/>
    public void Begin(CommandContext context)
    {
        if (context.Command is Scenarios.for_Commands.ModelBound.PolicyProtectedCommand)
        {
            observations.RecordBegin(context.ServiceProvider!.GetRequiredService<TenantBoundService>().Tenant.Value);
        }
    }

    /// <inheritdoc/>
    public Task Complete(CommandContext context, CommandResult result)
    {
        if (context.Command is Scenarios.for_Commands.ModelBound.PolicyProtectedCommand)
        {
            observations.RecordComplete(context.ServiceProvider!.GetRequiredService<TenantBoundService>().Tenant.Value);
        }

        return Task.CompletedTask;
    }
}
