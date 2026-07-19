// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Aggregates;
using Cratis.Arc.Commands;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Transactions;
using Cratis.DependencyInjection;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.Commands;

/// <summary>
/// Represents an <see cref="ICommandExecutionScope"/> that makes every command a transactional scope for the events
/// it declares transactional: events returned from the handler and appends through the explicit transactional style
/// enroll in a <see cref="IUnitOfWork"/> bounded by the command, committed atomically when the command succeeds —
/// surfacing any constraint or concurrency violations on the <see cref="CommandResult"/> — and rolled back when the
/// command fails. Immediate appends stay immediate and final, but they are never silently swallowed: a failed
/// immediate append during the command fails the command.
/// </summary>
/// <remarks>
/// A command always owns its transaction; only a nested command — one executed from within another command — joins
/// the outermost command's transaction, and only the outermost commits or rolls back. A unit of work established by
/// other integrations, such as Chronicle's request-level middleware, is left untouched. An aggregate root reuses the
/// command's unit of work and commits it itself; the scope then leaves it alone. The <see cref="IUnitOfWorkManager"/>
/// is resolved from the command's own service provider so the unit of work is created over the tenant-correct event
/// store.
/// </remarks>
[Singleton]
public class TransactionalCommandScope : ICommandExecutionScope
{
    static readonly AsyncLocal<OwnedTransaction?> _owned = new();

    /// <inheritdoc/>
    public void Begin(CommandContext context)
    {
        if (context.ServiceProvider is not { } serviceProvider || CommandTransaction.TryGetActive(out _))
        {
            // A nested command joins the outermost command's transaction, and without a service provider there is
            // nothing to own — either way this frame must not inherit ownership from an outer frame.
            _owned.Value = null;
            return;
        }

        var unitOfWorkManager = serviceProvider.GetRequiredService<IUnitOfWorkManager>();
        var unitOfWork = unitOfWorkManager.Begin(context.CorrelationId);
        var failedAppends = new List<AppendedEventWithResult>();
        var subscription = (serviceProvider.GetService<IEventLog>()?.AppendOperations)?.Subscribe(appended =>
        {
            lock (failedAppends)
            {
                // Only failures belonging to this command — a failure attributed to a different correlation is a
                // concurrent command's and must not fail this one. An unattributed failure (no correlation on the
                // result) during this command's window is treated as this command's.
                failedAppends.AddRange(appended.Where(_ =>
                    !_.Result.IsSuccess &&
                    (_.Result.CorrelationId == context.CorrelationId || _.Result.CorrelationId == CorrelationId.NotSet)));
            }
        });

        CommandTransaction.Current = unitOfWork;
        _owned.Value = new OwnedTransaction(unitOfWork, subscription, failedAppends);
    }

    /// <inheritdoc/>
    public async Task Complete(CommandContext context, CommandResult result)
    {
        if (_owned.Value is not { } owned)
        {
            return;
        }

        _owned.Value = null;
        CommandTransaction.Current = null;
        owned.Subscription?.Dispose();

        AppendedEventWithResult[] failedAppends;
        lock (owned.FailedAppends)
        {
            failedAppends = [.. owned.FailedAppends];
        }

        // Immediate appends are never silently swallowed: a failed one fails the command — which in turn rolls back
        // everything enrolled in the command's transaction below.
        foreach (var failedAppend in failedAppends.DistinctBy(_ => _.Result))
        {
            result.MergeWith(failedAppend.Result.ToCommandResult());
        }

        var unitOfWork = owned.UnitOfWork;
        if (result.IsSuccess)
        {
            if (!unitOfWork.IsCompleted)
            {
                await unitOfWork.Commit();
            }

            var commitResult = AggregateRootCommitResult.CreateFrom(unitOfWork, []);
            if (!commitResult.IsSuccess)
            {
                result.MergeWith(commitResult.ToCommandResult(result.CorrelationId));
            }
        }
        else if (!unitOfWork.IsCompleted)
        {
            await unitOfWork.Rollback();
        }
    }

    sealed record OwnedTransaction(IUnitOfWork UnitOfWork, IDisposable? Subscription, List<AppendedEventWithResult> FailedAppends);
}
