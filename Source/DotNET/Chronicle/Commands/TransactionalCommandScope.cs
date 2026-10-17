// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using Cratis.Arc.Chronicle.Aggregates;
using Cratis.Arc.Chronicle.Occurrences;
using Cratis.Arc.Chronicle.ReadModels;
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;
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
public class TransactionalCommandScope : ICommandOperationExecutionScope, ICommandInvocationExecutionScope
{
    static readonly AsyncLocal<TransactionFrame?> _frames = new();
    static readonly ConcurrentDictionary<Type, bool> _guardedGeneratedParameters = new();
    static readonly ConditionalWeakTable<CommandContextValues, CommitObservation> _observations = new();

    /// <inheritdoc/>
    public bool IsCommitParticipant => true;

    /// <summary>
    /// Gets freshness captures for the active invocation, without requiring a command outside the pipeline.
    /// </summary>
    internal static FreshIdentifierScopes? CurrentFreshIdentifiers => ActiveFrame()?.FreshIdentifiers;

    /// <summary>
    /// Gets the enforcing unit retained until invocation exit, including after an early aggregate commit.
    /// </summary>
    internal static IUnitOfWork? CurrentProtectedUnit => ActiveFrame()?.Protection?.UnitOfWork;

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
        if (CommandDecisionPolicy.DeclaredReadProtection > CommandDecisionPolicy.ReadProtection)
        {
            ReadProtectionTelemetry.Record(context.Type, "mode_lowered", reason: $"{CommandDecisionPolicy.DeclaredReadProtection} → {CommandDecisionPolicy.ReadProtection}");
        }
        var previous = ActiveFrame();
        var readProtected = CommandDecisionPolicy.ReadProtection == ReadProtectionMode.On;
        var frame = new TransactionFrame(context.Values, previous, FreshIdentifierScopes.For(context), context.Type, readProtected) { Services = context.ServiceProvider };
        if (previous is not null)
        {
            lock (previous)
            {
                // Every live sibling is checked: a sibling that ended earlier must not hide one that is still running.
                foreach (var sibling in previous.Children)
                {
                    // The owner boundary belongs to every command declaring On, not only to an enforcing outer unit.
                    if (previous.Protection is not null || sibling.ReadProtected || readProtected)
                    {
                        throw new ConcurrentNestedCommandsUnderReadProtection(previous.CommandType, sibling.CommandType, context.Type);
                    }
                }
                if (previous.Children.Count > 0 && CommandDecisionPolicy.ReadProtection == ReadProtectionMode.Measure)
                {
                    ReadProtectionTelemetry.Record(context.Type, "nested_concurrent");
                }
                previous.Children.Add(frame);
            }
        }
        _frames.Value = frame;
        CommandDecisionReads.Begin(context.Type);
        if (context.ServiceProvider is not { } serviceProvider)
        {
            return;
        }
        var inheritedUnit = previous?.Protection?.UnitOfWork;
        if (inheritedUnit is not null || CommandTransaction.TryGetActive(out inheritedUnit))
        {
            // An enforcing unit is retained even after early completion; a nested invocation cannot replace it.
            frame.UnitOfWork = inheritedUnit;
            if (previous?.Protection is not null)
            {
                frame.Protection = inheritedUnit.EnforceReadProtection();

                // Read regions never cross a command boundary: a nested On command does not inherit an advisory region
                // opened by its caller's body or by an enclosing command that does not enable read protection.
                frame.ReadAllowance = readProtected
                    ? inheritedUnit.SuspendReadRegions()
                    : inheritedUnit.AllowReads(ReadAllowance.Advisory, "Nested command does not enable read protection");
            }
            RegisterEnrollmentContributor(context, frame, inheritedUnit);
            return;
        }

        var unitOfWorkManager = serviceProvider.GetRequiredService<IUnitOfWorkManager>();
        var unitOfWork = unitOfWorkManager.Begin(context.CorrelationId);

        // The refusal frame owns keyed completion, including commands with protected reads.
        var owner = OccurrenceExecution.Current is null ? (unitOfWork as UnitOfWork)?.ClaimDecisionReadCommitOwnership() : null;
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
        frame.Owned = new OwnedTransaction(unitOfWork, owner, subscription, failedAppends, observation);
        frame.UnitOfWork = unitOfWork;
        if (OccurrenceExecution.Current is { } occurrenceExecution)
        {
            try
            {
                frame.ImmediateAppendRefusal = unitOfWork.RefuseImmediateAppends();
            }
            catch (UnitOfWorkImmediateAppendRefusalNotSupported error)
            {
                occurrenceExecution.Refuse(error);
            }
        }
        if (CommandDecisionPolicy.ReadProtection == ReadProtectionMode.On)
        {
            frame.Protection = unitOfWork.EnforceReadProtection();
        }
        RegisterEnrollmentContributor(context, frame, unitOfWork);
    }

    /// <inheritdoc/>
    public async Task Complete(CommandContext context, CommandResult result)
    {
        var frame = ActiveFrame();
        if (frame is null || !ReferenceEquals(frame.Values, context.Values))
        {
            // Begin may have thrown before this scope created a frame. Do not complete an enclosing command.
            return;
        }

        try
        {
            await CompleteFrame(context, result, frame);
        }
        finally
        {
            frame.EnrollmentContributor?.Dispose();
            frame.EnrollmentContributor = null;
        }
    }

    /// <inheritdoc/>
    public void End(CommandContext context)
    {
        var frame = _frames.Value;
        while (frame is not null && !ReferenceEquals(frame.Values, context.Values))
        {
            frame = frame.Previous;
        }
        if (frame?.Exited != false)
        {
            return;
        }

        try
        {
            frame.ReadAllowance?.Dispose();
            frame.Protection?.Dispose();
        }
        finally
        {
            frame.ImmediateAppendRefusal?.Dispose();
            frame.EnrollmentContributor?.Dispose();
            frame.Completed = true;
            frame.Exited = true;
            if (frame.Previous is { } previous)
            {
                lock (previous)
                {
                    previous.Children.Remove(frame);
                }
            }
            _frames.Value = frame.Previous;
        }
    }

    /// <summary>
    /// Commits an aggregate through the invocation's owner capability when enforcement is active.
    /// </summary>
    /// <param name="unit">The aggregate's unit.</param>
    /// <returns>Whether the protected invocation performed the commit.</returns>
    /// <exception cref="NestedEarlyCompletionUnderReadProtection">A nested command attempted to complete its parent's unit.</exception>
    internal static async Task<bool> CommitAggregate(IUnitOfWork unit)
    {
        OccurrenceExecution.RefuseEarlyCommit();
        var frame = ActiveFrame();
        var ownsUnit = frame is not null && ReferenceEquals(frame.UnitOfWork, unit);
        if (ownsUnit && frame!.Owned is null && frame.Previous is { } parent && (frame.Protection is not null || frame.ReadProtected))
        {
            // A nested command declaring On must not complete its parent's unit, whether or not the parent enforces.
            throw new NestedEarlyCompletionUnderReadProtection(frame.CommandType, parent.CommandType);
        }
        if (!ownsUnit || frame!.Protection is null)
        {
            if (frame?.Previous is not null && CommandDecisionPolicy.ReadProtection == ReadProtectionMode.Measure)
            {
                ReadProtectionTelemetry.Record(frame.CommandType, "nested_early_completion");
            }
            return false;
        }
        if (frame.Owned is not { } owned)
        {
            throw new NestedEarlyCompletionUnderReadProtection(frame.CommandType, frame.Previous?.CommandType ?? frame.CommandType);
        }

        await CommitOwned(owned, null);
        return true;
    }

    /// <summary>
    /// Reports decision-read conflicts of an early aggregate commit made through the protected owner as read changes,
    /// as completion does for a commit made at the end of the command.
    /// </summary>
    /// <param name="unit">The aggregate's unit.</param>
    /// <param name="result">The aggregate's commit result.</param>
    /// <returns>The result, with conflicting reads reported as read changes.</returns>
    internal static AggregateRootCommitResult ReportReadChanges(IUnitOfWork unit, AggregateRootCommitResult result) =>
        !result.IsSuccess && ActiveFrame() is { Protection: not null, Owned: not null } frame && ReferenceEquals(frame.UnitOfWork, unit)
            ? WithReadChanges(result, unit, frame.CommandType, enforcing: true, frame.Services)
            : result;

    /// <summary>
    /// Matches live guarded captures from every active invocation sharing the command's unit.
    /// </summary>
    /// <param name="unit">The receiving unit of work.</param>
    /// <param name="source">The append destination.</param>
    /// <returns>Whether an active invocation captured that destination with a guarded policy.</returns>
    internal static bool MatchesFreshIdentifier(IUnitOfWork unit, EventSourceId source)
    {
        for (var frame = ActiveFrame(); frame is not null; frame = frame.Previous)
        {
            if (!frame.Completed && ReferenceEquals(frame.UnitOfWork, unit) && frame.FreshIdentifiers.MatchesOwnCapture(source))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Defers stream completion until the command transaction's owner succeeds.
    /// </summary>
    /// <param name="context">The current command context.</param>
    /// <param name="completion">The resolved completion.</param>
    /// <returns>Whether an active execution frame accepted the completion.</returns>
    internal static bool EnrollCompletion(CommandContext context, PendingStreamCompletion completion)
    {
        OccurrenceExecution.RefuseStreamCompletion();
        var frame = ActiveFrame();
        if (frame is null || !ReferenceEquals(frame.Values, context.Values))
        {
            return false;
        }

        // A nested command's returned events join its owner's unit of work. Its completions must wait for that
        // owner's commit too, rather than closing a stream while the outer command still holds pending events.
        var owner = frame;
        while (owner.Owned is null && owner.Previous is { Completed: false } previous)
        {
            owner = previous;
        }
        owner.Completions.Add(completion);

        return true;
    }

    static async Task CompleteFrame(CommandContext context, CommandResult result, TransactionFrame frame)
    {
        // Leave this invocation's cache and provenance behind before a nested command resumes its own frame.
        CommandDecisionReads.End();
        var owned = frame.Owned;

        // Complete is async; a change to AsyncLocal.Value inside it does not propagate to its caller.
        // Mark the shared frame instead so a resumed outer command still owns its transaction.
        frame.Completed = true;
        _frames.Value = frame.Previous;
        if (owned is null)
        {
            if (result.IsSuccess)
            {
                await CompleteStreams(frame, result);
            }

            return;
        }

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
            var sequence = context.ServiceProvider?.GetService<IEventLog>()?.Id;
            result.MergeWith(sequence is not null ? failedAppend.Result.ToCommandResult(context, sequence) : failedAppend.Result.ToCommandResult());
        }

        var unitOfWork = owned.UnitOfWork;
        var observation = owned.Observation;

        // Fail-loud backstop only: this notification arrives after an immediate append committed without a receipt.
        // Native Chronicle appends are refused up front; retain detection for custom sequence implementations.
        if (OccurrenceExecution.Current is { } occurrenceExecution && observation.ImmediateCommitted)
        {
            occurrenceExecution.RecordImmediateAppend();
            result.MergeWith(CommandResult.Error(context.CorrelationId, "The command appended events outside its unit of work while executing for an occurrence."));
        }

        // IsCompleted also means rollback or a commit that threw. Never interpret it as authoritative commitment.
        if (unitOfWork.IsCompleted && !observation.CompletionObserved)
        {
            observation.Disposition = CommandCommitDisposition.Unknown;
        }

        if (result.IsSuccess)
        {
            if (!unitOfWork.IsCompleted)
            {
                await CommitOwned(owned, frame.ImmediateAppendRefusal);
            }

            var commitResult = AggregateRootCommitResult.CreateFrom(unitOfWork, []);
            if (!commitResult.IsSuccess)
            {
                result.MergeWith(WithReadChanges(commitResult, unitOfWork, context.Type, frame.Protection is not null, context.ServiceProvider).ToCommandResult(result.CorrelationId));
            }

            if (result.IsSuccess && frame.Completions.Count > 0)
            {
                // A sequence number reported by the unit is not commit evidence: Chronicle also reports one after
                // rollback or an append that threw. Only this scope's observed successful commit authorizes completion.
                if (observation.CompletionObserved && commitResult.IsSuccess)
                {
                    await CompleteStreams(frame, result);
                }
                else
                {
                    result.MergeWith(CommandResult.Error(context.CorrelationId, "Stream completion was not applied because the command's commit outcome is unknown."));
                }
            }
        }
        else if (!unitOfWork.IsCompleted)
        {
            observation.Disposition = CommandCommitDisposition.Unknown;
            if (frame.ImmediateAppendRefusal is { } refusal)
            {
                await refusal.Rollback();
            }
            else if (owned.Owner is not null && unitOfWork is UnitOfWork protectedUnit && (protectedUnit.HasEnrolledDecisionReads || protectedUnit.IsEnforcingReadProtection))
            {
                // Chronicle refuses the public Rollback for an owner-claimed unit with enrolled decision reads, so the failed
                // command rolls back with the capability it claimed when it began. Without decision reads the public
                // Rollback below keeps the exact behaviour commands had before decision reads existed.
                await protectedUnit.RollbackAsOwner(owned.Owner);
            }
            else
            {
                await unitOfWork.Rollback();
            }

            observation.CompletionObserved = true;
            observation.Disposition = CommandCommitDisposition.NotCommitted;
        }
    }

    static AggregateRootCommitResult WithReadChanges(AggregateRootCommitResult commitResult, IUnitOfWork unitOfWork, Type commandType, bool enforcing, IServiceProvider? services)
    {
        var conflicts = unitOfWork is UnitOfWork protectedUnit ? protectedUnit.GetDecisionConflicts().ToArray() : [];
        var conflictLabels = conflicts.Select(_ => (string)_.Key).ToHashSet();
        return new AggregateRootCommitResult
        {
            Events = commitResult.Events,
            SequenceNumbers = commitResult.SequenceNumbers,
            FreshIdentifiers = commitResult.FreshIdentifiers,
            ConstraintViolations = commitResult.ConstraintViolations,
            ConcurrencyViolations = commitResult.ConcurrencyViolations.Where(_ => !conflictLabels.Contains((string)_.EventSourceId)).ToArray(),
            Errors = commitResult.Errors,
            ValidationResults = commitResult.ValidationResults.Concat(conflicts.Select(_ => ReadChanges.For(_, commandType, enforcing, services))).ToArray()
        };
    }

    static async Task CommitOwned(OwnedTransaction owned, ImmediateAppendRefusal? refusal)
    {
        using var measurementCommit = ReadProtectionMeasurement.Committing();
        var unit = owned.UnitOfWork;
        var hasEvents = unit.GetEvents().Any();
        var occurrenceExecution = OccurrenceExecution.Current;
        occurrenceExecution?.Enroll(unit);
        owned.Observation.Disposition = CommandCommitDisposition.Unknown;
        if (refusal is not null)
        {
            await refusal.Commit();
        }
        else if (owned.Owner is not null)
        {
            await ((UnitOfWork)unit).CommitAsOwner(owned.Owner);
        }
        else
        {
            if (unit is UnitOfWork { IsEnforcingReadProtection: true } or UnitOfWork { HasEnrolledDecisionReads: true })
            {
                throw new ProtectedDecisionRequiresOwnerCapableUnitOfWork();
            }
            await unit.Commit();
        }
        occurrenceExecution?.Committed(unit);
        owned.Observation.CompletionObserved = true;
        owned.Observation.Disposition = unit.GetAppendErrors().Any()
            ? CommandCommitDisposition.Unknown
            : unit.GetConstraintViolations().Any() || unit.GetConcurrencyViolations().Any()
                ? CommandCommitDisposition.NotCommitted
                : hasEvents ? CommandCommitDisposition.Committed : CommandCommitDisposition.NotCommitted;
    }

    static void RegisterEnrollmentContributor(CommandContext context, TransactionFrame frame, IUnitOfWork unit)
    {
        if (!HasGuardedGeneratedParameters(context))
        {
            return;
        }

        try
        {
            frame.EnrollmentContributor = unit.AddEnrollmentContributor(new FreshIdentifierEnrollmentContributor(frame.FreshIdentifiers, unit));
        }
        catch (UnitOfWorkEnrollmentContributorsNotSupported exception)
        {
            throw new FreshIdentifierGuardRequiresEnrollmentContributors(exception);
        }
    }

    static bool HasGuardedGeneratedParameters(CommandContext context)
    {
        if (_guardedGeneratedParameters.TryGetValue(context.Type, out var guarded))
        {
            return guarded;
        }

        var providers = context.ServiceProvider?.GetService<ICommandHandlerProviders>();
        if (providers?.TryGetHandlerFor(context.Command, out var handler) != true)
        {
            return false;
        }

        return _guardedGeneratedParameters.GetOrAdd(
            context.Type,
            static (_, handler) => handler.Parameters.Any(parameter => parameter.GetCustomAttribute<GeneratedValueAttribute>()?.Freshness == GeneratedValueFreshness.Guarded),
            handler);
    }

    static async Task CompleteStreams(TransactionFrame frame, CommandResult result)
    {
        foreach (var completion in frame.Completions.Distinct())
        {
            if (frame.Protection is { } protection)
            {
                await protection.RunAfterCommit(_ => completion.Complete(result));
            }
            else
            {
                await completion.Complete(result);
            }
            if (!result.IsSuccess)
            {
                break;
            }
        }
    }

    static TransactionFrame? ActiveFrame()
    {
        var frame = _frames.Value;
        while (frame?.Completed == true)
        {
            frame = frame.Previous;
        }

        return frame;
    }

    sealed class CommitObservation(IUnitOfWork unitOfWork)
    {
        public IUnitOfWork UnitOfWork { get; } = unitOfWork;
        public CommandCommitDisposition Disposition { get; set; } = CommandCommitDisposition.NotCommitted;
        public bool CompletionObserved { get; set; }
        public bool ImmediateCommitted { get; set; }
        public bool ImmediateUncertain { get; set; }
    }

    sealed class TransactionFrame(CommandContextValues values, TransactionFrame? previous, FreshIdentifierScopes freshIdentifiers, Type commandType, bool readProtected)
    {
        public Type CommandType { get; } = commandType;
        public IServiceProvider? Services { get; init; }
        public bool ReadProtected { get; } = readProtected;
        public ReadProtectionFrame? Protection { get; set; }
        public ImmediateAppendRefusal? ImmediateAppendRefusal { get; set; }
        public IDisposable? ReadAllowance { get; set; }
        public List<TransactionFrame> Children { get; } = [];
        public bool Exited { get; set; }
        public FreshIdentifierScopes FreshIdentifiers { get; } = freshIdentifiers;
        public CommandContextValues Values { get; } = values;
        public TransactionFrame? Previous { get; } = previous;
        public OwnedTransaction? Owned { get; set; }
        public IUnitOfWork? UnitOfWork { get; set; }
        public IDisposable? EnrollmentContributor { get; set; }
        public List<PendingStreamCompletion> Completions { get; } = [];
        public bool Completed { get; set; }
    }

    sealed record OwnedTransaction(IUnitOfWork UnitOfWork, DecisionReadCommitOwner? Owner, IDisposable? Subscription, List<AppendedEventWithResult> FailedAppends, CommitObservation Observation);
}
