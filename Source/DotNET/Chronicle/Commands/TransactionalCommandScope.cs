// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Aggregates;
using Cratis.Arc.Commands;
using Cratis.Chronicle.Transactions;
using Cratis.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.Commands;

/// <summary>
/// Represents an <see cref="ICommandExecutionScope"/> that makes every command a transactional scope: a
/// <see cref="IUnitOfWork"/> is begun before the command executes, every event the command appends enrolls in it, and
/// it is committed atomically when the command succeeds — surfacing any constraint or concurrency violations from the
/// commit on the <see cref="CommandResult"/> — or rolled back when the command fails for any reason. The invariant is
/// that a command whose result is not successful appends no events at all.
/// </summary>
/// <remarks>
/// A command always owns its transaction; only a nested command — one executed from within another command — joins
/// the outermost command's unit of work, and only the outermost commits or rolls back. A unit of work established by
/// other integrations, such as Chronicle's request-level middleware, is left untouched. An aggregate root reuses the
/// command's unit of work and commits it itself; the scope then leaves it alone. The <see cref="IUnitOfWorkManager"/>
/// is resolved from the command's own service provider so the unit of work is created over the tenant-correct event
/// store.
/// </remarks>
[Singleton]
public class TransactionalCommandScope : ICommandExecutionScope
{
    static readonly AsyncLocal<IUnitOfWork?> _ownedUnitOfWork = new();

    /// <inheritdoc/>
    public void Begin(CommandContext context)
    {
        if (context.ServiceProvider is not { } serviceProvider)
        {
            return;
        }

        if (CommandTransaction.TryGetActive(out _))
        {
            // A nested command joins the outermost command's transaction — only the outermost owns it.
            _ownedUnitOfWork.Value = null;
            return;
        }

        var unitOfWorkManager = serviceProvider.GetRequiredService<IUnitOfWorkManager>();
        var unitOfWork = unitOfWorkManager.Begin(context.CorrelationId);
        CommandTransaction.Current = unitOfWork;
        _ownedUnitOfWork.Value = unitOfWork;
    }

    /// <inheritdoc/>
    public async Task Complete(CommandContext context, CommandResult result)
    {
        if (_ownedUnitOfWork.Value is not { } unitOfWork)
        {
            return;
        }

        _ownedUnitOfWork.Value = null;
        CommandTransaction.Current = null;

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
}
