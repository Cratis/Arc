// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.ExceptionServices;
using Cratis.Chronicle.Occurrences;
using Cratis.Chronicle.Transactions;

namespace Cratis.Arc.Chronicle.Occurrences;

/// <summary>
/// Holds the keyed execution of an occurrence effect that is running in the current async flow. The
/// <see cref="Commands.TransactionalCommandScope"/> consults it to enroll the effect receipt in the command's unit of
/// work and to refuse the commit styles an effect receipt cannot cover.
/// </summary>
/// <param name="key">The <see cref="EffectKey"/> of the effect.</param>
/// <param name="commandName">The name of the executed command type.</param>
sealed class OccurrenceExecution(EffectKey key, string commandName)
{
    static readonly AsyncLocal<OccurrenceExecution?> _current = new();
    volatile bool _ended;
    Exception? _programmingError;

    /// <summary>
    /// Gets the keyed execution running in the current async flow, if any.
    /// </summary>
    internal static OccurrenceExecution? Current => _current.Value is { } execution && !execution._ended ? execution : null;

    /// <summary>
    /// Gets the key of the effect.
    /// </summary>
    internal EffectKey Key { get; } = key;

    /// <summary>
    /// Gets the name of the executed command type.
    /// </summary>
    internal string CommandName { get; } = commandName;

    /// <summary>
    /// Gets a value indicating whether the command's unit of work held events at commit.
    /// </summary>
    internal bool HadEvents { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the receipt was enrolled in the unit of work.
    /// </summary>
    internal bool Enrolled { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the command appended events outside its unit of work.
    /// </summary>
    internal bool AppendedImmediately { get; private set; }

    /// <summary>
    /// Gets the receipt reported by a successful receipted commit.
    /// </summary>
    internal EffectReceiptResult? Receipt { get; private set; }

    /// <summary>
    /// Makes an execution current for the async flow.
    /// </summary>
    /// <param name="execution">The execution.</param>
    /// <returns>A scope that ends the execution.</returns>
    internal static IDisposable Begin(OccurrenceExecution execution)
    {
        _current.Value = execution;
        return new Ending(execution);
    }

    /// <summary>
    /// Refuses an early commit of a unit of work while an effect is executing.
    /// </summary>
    /// <exception cref="OccurrenceCommandCannotCommitEarly">An effect is executing.</exception>
    internal static void RefuseEarlyCommit()
    {
        if (Current is { } execution)
        {
            execution.Refuse(new OccurrenceCommandCannotCommitEarly($"The command '{execution.CommandName}' for effect '{execution.Key.Value}' committed its unit of work early. Only the final commit carries the effect receipt."));
        }
    }

    /// <summary>
    /// Refuses a stream completion while an effect is executing.
    /// </summary>
    /// <exception cref="OccurrenceCommandCannotCompleteStreams">An effect is executing.</exception>
    internal static void RefuseStreamCompletion()
    {
        if (Current is { } execution)
        {
            execution.Refuse(new OccurrenceCommandCannotCompleteStreams($"The command '{execution.CommandName}' for effect '{execution.Key.Value}' completes an event stream, which an effect receipt cannot cover."));
        }
    }

    /// <summary>
    /// Records that the command appended events immediately.
    /// </summary>
    internal void RecordImmediateAppend() => AppendedImmediately = true;

    /// <summary>
    /// Enrolls the effect receipt in the unit of work before it commits. A unit with neither events nor protected decision
    /// reads is left alone; its no-event receipt is recorded by the executor.
    /// </summary>
    /// <param name="unitOfWork">The command's unit of work.</param>
    internal void Enroll(IUnitOfWork unitOfWork)
    {
        var events = unitOfWork.GetEvents().Count();
        HadEvents = events > 0;
        if (!HadEvents && !unitOfWork.HasEnrolledDecisionReads && !unitOfWork.IsEnforcingReadProtection)
        {
            return;
        }

        unitOfWork.UseEffectReceipt(Key, HadEvents ? OccurrenceReceiptResponses.Appended(CommandName, events) : OccurrenceReceiptResponses.NoEvents(CommandName));
        Enrolled = true;
    }

    /// <summary>
    /// Captures the receipt reported by the unit of work after its commit.
    /// </summary>
    /// <param name="unitOfWork">The committed unit of work.</param>
    internal void Committed(IUnitOfWork unitOfWork) => Receipt = unitOfWork.EffectReceipt;

    /// <summary>
    /// Rethrows a programming error even when the command pipeline captured it in its result.
    /// </summary>
    internal void ThrowIfRefused()
    {
        if (_programmingError is { } error)
        {
            ExceptionDispatchInfo.Capture(error).Throw();
        }
    }

    void Refuse(Exception error)
    {
        _programmingError = error;
        throw error;
    }

    sealed class Ending(OccurrenceExecution execution) : IDisposable
    {
        public void Dispose()
        {
            // A copied ExecutionContext still references this object after its parent's async flow ends.
            execution._ended = true;
            if (ReferenceEquals(_current.Value, execution))
            {
                _current.Value = null;
            }
        }
    }
}
