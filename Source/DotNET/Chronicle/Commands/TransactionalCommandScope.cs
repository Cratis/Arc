// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
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
public class TransactionalCommandScope : ICommandOperationExecutionScope
{
    static readonly AsyncLocal<OwnedTransaction?> _owned = new();
    static readonly ConditionalWeakTable<CommandContextValues, CommitObservation> _observations = new();

    /// <inheritdoc/>
    public bool IsCommitParticipant => true;

    /// <inheritdoc/>
    public CommandCommitDisposition GetCommitDisposition(CommandContext context)
    {
        if (!_observations.TryGetValue(context.Values, out var observation))
        {
            return CommandCommitDisposition.Unknown;
        }

        lock (observation)
        {
            if (!observation.CompletionObserved && observation.UnitOfWork.IsCompleted)
            {
                return CommandCommitDisposition.Unknown;
            }

            if (observation.ImmediateCommitted)
            {
                return observation.ImmediateUncertain || observation.Disposition == CommandCommitDisposition.Unknown
                    ? CommandCommitDisposition.Mixed : CommandCommitDisposition.Committed;
            }

            return observation.ImmediateUncertain ? CommandCommitDisposition.Unknown : observation.Disposition;
        }
    }

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
        var observation = new CommitObservation(unitOfWork);
        _observations.Remove(context.Values);
        _observations.Add(context.Values, observation);
        var subscription = (serviceProvider.GetService<IEventLog>()?.AppendOperations)?.Subscribe(appended =>
        {
            lock (failedAppends)
            {
                // Only failures belonging to this command — a failure attributed to a different correlation is a
                // concurrent command's and must not fail this one. An unattributed failure (no correlation on the
                // result) during this command's window is treated as this command's.
                var attributable = appended.Where(_ => _.Result.CorrelationId == context.CorrelationId || _.Result.CorrelationId == CorrelationId.NotSet).ToArray();
                failedAppends.AddRange(attributable.Where(_ => !_.Result.IsSuccess));
                lock (observation)
                {
                    observation.ImmediateCommitted |= attributable.Any(_ => _.Result.IsSuccess);
                    observation.ImmediateUncertain |= attributable.Any(_ => !_.Result.IsSuccess && _.Result.Errors.Any());
                }
            }
        });

        CommandTransaction.Current = unitOfWork;
        _owned.Value = new OwnedTransaction(unitOfWork, subscription, failedAppends, observation);
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
        var observation = owned.Observation;

        // IsCompleted also means rollback or a commit that threw. Never interpret it as authoritative commitment.
        if (unitOfWork.IsCompleted)
        {
            observation.Disposition = CommandCommitDisposition.Unknown;
        }

        if (result.IsSuccess)
        {
            if (!unitOfWork.IsCompleted)
            {
                var hasEvents = unitOfWork.GetEvents().Any();
                observation.Disposition = CommandCommitDisposition.Unknown;
                await unitOfWork.Commit();
                observation.CompletionObserved = true;
                observation.Disposition = unitOfWork.GetAppendErrors().Any()
                    ? CommandCommitDisposition.Unknown
                    : unitOfWork.GetConstraintViolations().Any() || unitOfWork.GetConcurrencyViolations().Any()
                        ? CommandCommitDisposition.NotCommitted
                        : hasEvents ? CommandCommitDisposition.Committed : CommandCommitDisposition.NotCommitted;
            }

            var commitResult = AggregateRootCommitResult.CreateFrom(unitOfWork, []);
            if (!commitResult.IsSuccess)
            {
                result.MergeWith(commitResult.ToCommandResult(result.CorrelationId));
            }
        }
        else if (!unitOfWork.IsCompleted)
        {
            observation.Disposition = CommandCommitDisposition.Unknown;
            await unitOfWork.Rollback();
            observation.CompletionObserved = true;
            observation.Disposition = CommandCommitDisposition.NotCommitted;
        }
    }

    sealed class CommitObservation(IUnitOfWork unitOfWork)
    {
        public IUnitOfWork UnitOfWork { get; } = unitOfWork;
        public CommandCommitDisposition Disposition { get; set; } = CommandCommitDisposition.NotCommitted;
        public bool CompletionObserved { get; set; }
        public bool ImmediateCommitted { get; set; }
        public bool ImmediateUncertain { get; set; }
    }

    sealed record OwnedTransaction(IUnitOfWork UnitOfWork, IDisposable? Subscription, List<AppendedEventWithResult> FailedAppends, CommitObservation Observation);
}
