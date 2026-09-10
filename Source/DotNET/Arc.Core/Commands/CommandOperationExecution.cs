// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OneOf;

namespace Cratis.Arc.Commands;

/// <summary>
/// Owns a preflighted flat invocation journal and commit-aware, cooperative recovery.
/// </summary>
internal sealed class CommandOperationExecution
{
    readonly List<Invocation> _planned = [];
    readonly List<CommandOperationOutcome> _outcomes = [];
    readonly TimeSpan _timeout;
    readonly CommandOperationFrame _frame;
    readonly ILogger<CommandOperationExecution>? _logger;
    CommandResult? _originalResult;
    string[]? _originalFailure;
    CommandOperationFailureSource _failureSource;
    int? _failedInvocation;

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandOperationExecution"/> class by validating and preflighting all declarations.
    /// </summary>
    /// <param name="operations">Ordered declarations.</param>
    /// <param name="serviceProvider">The originating command service provider.</param>
    /// <param name="frame">The owning flat command boundary.</param>
    public CommandOperationExecution(IEnumerable<ICommandOperation> operations, IServiceProvider serviceProvider, CommandOperationFrame frame)
    {
        _frame = frame;
        _logger = serviceProvider.GetService<ILogger<CommandOperationExecution>>();
        _timeout = serviceProvider.GetService<IOptions<CommandOperationOptions>>()?.Value.CompensationTimeout ?? TimeSpan.FromSeconds(30);
        if (_timeout <= TimeSpan.Zero || _timeout.TotalMilliseconds > uint.MaxValue - 1)
        {
            throw new InvalidCommandOperation("CompensationTimeout must be positive and at most 4294967294 milliseconds.");
        }

        // Validate every declaration before resolving any dependency, then preflight both methods before user execution.
        var metadata = operations.Select(operation => (Operation: operation, Invoker: CommandOperationInvokers.Get(operation.GetType()))).ToArray();
        foreach (var (operation, invoker) in metadata)
        {
            _planned.Add(new(operation, invoker, Resolve(invoker.ExecuteParameters, serviceProvider), Resolve(invoker.CompensateParameters, serviceProvider)));
        }
    }

    /// <summary>
    /// Normalizes tuples and the active union branch without interpreting ordinary collections.
    /// </summary>
    /// <param name="value">The returned graph.</param>
    /// <returns>Ordered leaf values.</returns>
    public static IEnumerable<object> Flatten(object? value)
    {
        switch (value)
        {
            case null:
                yield break;
            case IOneOf oneOf:
                foreach (var element in Flatten(oneOf.Value))
                {
                    yield return element;
                }
                break;
            case ITuple tuple:
                for (var index = 0; index < tuple.Length; index++)
                {
                    foreach (var element in Flatten(tuple[index]))
                    {
                        yield return element;
                    }
                }
                break;
            default:
                yield return value;
                break;
        }
    }

    /// <summary>
    /// Reads explicit integration commit facts, never command success.
    /// </summary>
    /// <param name="scopes">Preflighted compatible scopes.</param>
    /// <param name="context">The command context.</param>
    /// <returns>The participant's reported facts.</returns>
    public static CommandCommitDisposition Disposition(ICommandExecutionScope[] scopes, CommandContext context)
    {
        var participant = scopes.OfType<ICommandOperationExecutionScope>().SingleOrDefault(scope => scope.IsCommitParticipant);

        return participant?.GetCommitDisposition(context) ?? CommandCommitDisposition.NoCommit;
    }

    /// <summary>
    /// Freezes the first failure independently of the mutable command result.
    /// </summary>
    /// <param name="result">Observed command outcome.</param>
    /// <param name="source">The phase that failed.</param>
    public void CaptureFailure(CommandResult result, CommandOperationFailureSource source)
    {
        if (_originalFailure is null && !result.IsSuccess)
        {
            _originalFailure = result.ExceptionMessages.ToArray();
            _originalResult = new CommandResult
            {
                IsAuthorized = result.IsAuthorized,
                AuthorizationFailureReason = result.AuthorizationFailureReason,
                ValidationResults = result.ValidationResults.ToArray(),
                ExceptionMessages = _originalFailure,
                ExceptionStackTrace = result.ExceptionStackTrace
            };
            _failureSource = source;
        }
    }

    /// <summary>
    /// Executes sequentially and registers each invocation immediately before entry.
    /// </summary>
    /// <param name="cancellationToken">Forward cancellation.</param>
    /// <returns>Observed completion of all invocations.</returns>
    public async Task Execute(CancellationToken cancellationToken)
    {
        for (var index = 0; index < _planned.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var invocation = _planned[index];
            BindContext(invocation.Invoker.ExecuteParameters, invocation.ExecuteArguments, null, cancellationToken);

            // Entry is recorded immediately before the call, not while planning the batch.
            _outcomes.Add(new(index, invocation.Operation.GetType(), false, CommandOperationCompensation.NotNeeded));
            try
            {
                await invocation.Invoker.Execute(invocation.Operation, invocation.ExecuteArguments);
                _outcomes[index] = _outcomes[index] with { ExecutionCompleted = true };
            }
            catch (Exception)
            {
                _failedInvocation = index;
                throw;
            }

            // A nested pipeline call returns a failed CommandResult rather than throwing. Do not let an operation
            // discard that result and silently continue the batch or commit its enclosing command.
            _frame.ValidateNoNewNestedCommands(0);
        }

        cancellationToken.ThrowIfCancellationRequested();
    }

    /// <summary>
    /// Restores original failure facts before another scope can decide whether to commit.
    /// </summary>
    /// <param name="result">The mutable result shared by execution scopes.</param>
    public void RestoreFailure(CommandResult result)
    {
        if (_originalResult is not null)
        {
            result.IsAuthorized &= _originalResult.IsAuthorized;
            result.ValidationResults = result.ValidationResults.Union(_originalResult.ValidationResults).ToArray();
            result.ExceptionMessages = result.ExceptionMessages.Union(_originalResult.ExceptionMessages).ToArray();
            if (string.IsNullOrEmpty(result.AuthorizationFailureReason))
            {
                result.AuthorizationFailureReason = _originalResult.AuthorizationFailureReason;
            }

            if (!result.ExceptionStackTrace.Contains(_originalResult.ExceptionStackTrace, StringComparison.Ordinal))
            {
                result.ExceptionStackTrace = string.Join(Environment.NewLine, _originalResult.ExceptionStackTrace, result.ExceptionStackTrace);
            }
        }
    }

    /// <summary>
    /// Attempts eligible recovery and publishes immutable server-only observations.
    /// </summary>
    /// <param name="result">The final transport result, without recovery internals.</param>
    /// <param name="disposition">Integration-reported commitment.</param>
    /// <returns>Completion of all possible recovery callbacks.</returns>
    public async Task Recover(CommandResult result, CommandCommitDisposition disposition)
    {
        RestoreFailure(result);
        var requiresRecovery = _originalFailure is not null && _outcomes.Count > 0;
        var eligible = disposition is CommandCommitDisposition.NoCommit or CommandCommitDisposition.NotCommitted;
        var status = CommandRecoveryStatus.NotNeeded;
        if (requiresRecovery && !eligible)
        {
            status = disposition == CommandCommitDisposition.Committed ? CommandRecoveryStatus.Suppressed : CommandRecoveryStatus.Indeterminate;
            for (var index = 0; index < _outcomes.Count; index++)
            {
                _outcomes[index] = _outcomes[index] with { Compensation = CommandOperationCompensation.Suppressed };
            }
        }
        else if (requiresRecovery)
        {
            using var cleanup = new CancellationTokenSource(_timeout);
            for (var index = _outcomes.Count - 1; index >= 0; index--)
            {
                var invocation = _planned[index];
                var outcome = _outcomes[index];
                if (invocation.Invoker.Compensate is null)
                {
                    _outcomes[index] = outcome with { Compensation = CommandOperationCompensation.NotAvailable };
                    continue;
                }

                if (cleanup.IsCancellationRequested)
                {
                    _outcomes[index] = outcome with { Compensation = CommandOperationCompensation.BudgetExpired };
                    continue;
                }

                var failure = new CommandOperationFailure(index, outcome.ExecutionCompleted, _failedInvocation == index, _failureSource, disposition, _originalFailure!);
                BindContext(invocation.Invoker.CompensateParameters, invocation.CompensateArguments, failure, cleanup.Token);
                var previousNestedAttempts = _frame.NestedCommandAttempts;
                try
                {
                    await invocation.Invoker.Compensate(invocation.Operation, invocation.CompensateArguments);
                    _frame.ValidateNoNewNestedCommands(previousNestedAttempts);
                    _outcomes[index] = outcome with { Compensation = CommandOperationCompensation.Completed };
                }
                catch (Exception exception)
                {
                    // Recovery failures never replace the original command failure or prevent the next eligible callback.
                    _outcomes[index] = outcome with { Compensation = CommandOperationCompensation.Failed, CompensationFailure = exception.Message };
                    RecordDiagnostic(() => _logger?.CompensationFailed(result.CorrelationId.ToString(), index, outcome.OperationType.FullName ?? outcome.OperationType.Name, exception));
                }
            }

            status = _outcomes.TrueForAll(outcome => outcome.Compensation == CommandOperationCompensation.Completed)
                ? CommandRecoveryStatus.Completed : CommandRecoveryStatus.Incomplete;
        }

        result.OperationOutcomes = Array.AsReadOnly(_outcomes.ToArray());
        var compensated = _outcomes.Count(outcome => outcome.Compensation == CommandOperationCompensation.Completed);
        result.Recovery = new(
            disposition,
            status,
            _outcomes.Count,
            _outcomes.Count(outcome => outcome.ExecutionCompleted),
            compensated,
            _outcomes.Count(outcome => outcome.Compensation == CommandOperationCompensation.Failed),
            requiresRecovery ? _outcomes.Count - compensated : 0);
        RecordDiagnostic(() => _logger?.RecoveryObserved(result.CorrelationId.ToString(), disposition, status, _outcomes.Count, compensated, result.Recovery.UncompensatedCount));
    }

    static void RecordDiagnostic(Action log)
    {
        try
        {
            log();
        }
        catch (Exception exception)
        {
            // Observability must not interrupt recovery or replace its original failure. Retain a sanitized tracing
            // signal when a custom logging provider fails; never retry a failing provider during recovery.
            Activity.Current?.AddEvent(new ActivityEvent("command.operation.logging.failed", tags: new ActivityTagsCollection
            {
                { "exception.type", exception.GetType().FullName }
            }));
        }
    }

    static object?[] Resolve(IReadOnlyList<Type> parameters, IServiceProvider serviceProvider) => parameters.Select(type =>
        type == typeof(CancellationToken) || type == typeof(CommandOperationFailure) ? null : serviceProvider.GetRequiredService(type)).ToArray();

    static void BindContext(IReadOnlyList<Type> parameters, object?[] arguments, CommandOperationFailure? failure, CancellationToken cancellationToken)
    {
        for (var index = 0; index < parameters.Count; index++)
        {
            if (parameters[index] == typeof(CancellationToken))
            {
                arguments[index] = cancellationToken;
            }
            else if (parameters[index] == typeof(CommandOperationFailure))
            {
                arguments[index] = failure;
            }
        }
    }

    sealed record Invocation(ICommandOperation Operation, CommandOperationInvoker Invoker, object?[] ExecuteArguments, object?[] CompensateArguments);
}
