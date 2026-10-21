// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.DependencyInjection;

namespace Cratis.Arc.Chronicle.ReadModels;

/// <summary>
/// Owns the observational context through command completion without participating in the transaction.
/// </summary>
[Singleton]
internal sealed class ReadProtectionMeasureScope : IAsyncCommandExecutionScope, ICommandInvocationExecutionScope, ICommandOperationExecutionScope
{
    /// <inheritdoc/>
    public bool IsCommitParticipant => false;

    /// <inheritdoc/>
    public CommandCommitDisposition GetCommitDisposition(CommandContext context) => CommandCommitDisposition.NotCommitted;

    /// <inheritdoc/>
    public void Begin(CommandContext context) => ReadProtectionMeasurement.Begin(context);

    /// <inheritdoc/>
    public Task Complete(CommandContext context, CommandResult result) => Task.CompletedTask;

    /// <inheritdoc/>
    public void End(CommandContext context) => ReadProtectionMeasurement.EndInvocation(context);

    /// <inheritdoc/>
    Task IAsyncCommandExecutionScope.BeginAsync(CommandContext context) => ReadProtectionMeasurement.Current?.CaptureBoundary() ?? Task.CompletedTask;
}
