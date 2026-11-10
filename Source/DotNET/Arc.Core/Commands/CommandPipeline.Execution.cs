// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Arc.Authorization;
using Cratis.Arc.DependencyInjection;
using Cratis.Arc.Identity.Approvals;
using Cratis.Arc.Observability;
using Cratis.Arc.Validation;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Commands;

public partial class CommandPipeline
{
    /// <summary>
    /// Runs the identical entry sequence of a command execution: a span, then the core, then recording the outcome.
    /// </summary>
    /// <param name="command">The command.</param>
    /// <param name="recordServices">The services used to record metrics.</param>
    /// <param name="core">The core to run with the span's activity.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The command result.</returns>
    async Task<CommandResult> TraceExecution(object command, IServiceProvider recordServices, Func<Activity?, Task<CommandResult>> core, CancellationToken cancellationToken)
    {
        using var span = activitySource.Execute(TypeNameOf(command.GetType()));
        OperationActivity.AddCommandType(span.Activity, command.GetType());
        var started = Stopwatch.GetTimestamp();
        try
        {
            var result = await core(span.Activity);
            RecordCommand(span.Activity, recordServices, command.GetType(), result, started, cancellationToken);
            return result;
        }
        catch (Exception ex)
        {
            RecordCommandFailure(span.Activity, recordServices, command.GetType(), ex, started, cancellationToken);
            throw;
        }
    }

    /// <summary>
    /// Runs the identical entry sequence of a command validation: a span, then the core, then recording the outcome.
    /// </summary>
    /// <param name="command">The command.</param>
    /// <param name="core">The core to run with the span's activity.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The validation result.</returns>
    async Task<CommandResult> TraceValidation(object command, Func<Activity?, Task<CommandResult>> core, CancellationToken cancellationToken)
    {
        using var span = activitySource.Validate(TypeNameOf(command.GetType()));
        OperationActivity.AddCommandType(span.Activity, command.GetType());
        try
        {
            var result = await core(span.Activity);
            RecordValidation(span.Activity, result, cancellationToken);
            return result;
        }
        catch (Exception ex)
        {
            RecordValidationFailure(span.Activity, ex, cancellationToken);
            throw;
        }
    }

    /// <summary>
    /// Resolves the prepared authorization, checks its roles and begins the selected principal's scope.
    /// </summary>
    /// <param name="run">The run state, receiving the prepared authorization.</param>
    /// <param name="suppliedAuthorization">The authorization prepared by a hosted transport, if any.</param>
    /// <returns>The unauthorized result when the roles do not match; otherwise null.</returns>
    /// <exception cref="InvalidAuthorizationConfiguration">Thrown when authorization services are unavailable.</exception>
    async Task<CommandResult?> PrepareAuthorization(CommandRun run, PreparedAuthorization? suppliedAuthorization)
    {
        var services = run.Services;
        var commandType = run.Command.GetType();
        var declarations = services.GetService<AuthorizationDeclarations>() ??
            throw new InvalidAuthorizationConfiguration("Authorization declarations are unavailable.");
        var prepared = suppliedAuthorization;
        if (prepared is null && declarations.For(commandType).RequiresAsynchronousEvaluation)
        {
            prepared = await (services.GetService<AuthorizationEvaluation>() ??
                throw new InvalidAuthorizationConfiguration("Authorization evaluation is unavailable."))
                .Prepare(commandType, services, run.CancellationToken);
        }

        run.Prepared = prepared;
        if (prepared is null)
        {
            return null;
        }

        if (!AuthorizationEvaluator.CheckRoles(prepared.Declaration, prepared.SelectedPrincipal, prepared.EvaluatesAnonymous))
        {
            return CommandResult.Unauthorized(run.CorrelationId);
        }

        if (prepared.PrincipalChanged)
        {
            run.PrincipalLease.Attach(services.GetRequiredService<AuthorizationPrincipalScope>()
                .Begin(prepared.SelectedPrincipal!, services));
        }

        return null;
    }

    /// <summary>
    /// Builds the <see cref="CommandContext"/> of the run and makes it the current one.
    /// </summary>
    /// <param name="run">The run state, receiving the context.</param>
    /// <param name="allowedSeverity">The validation severity threshold.</param>
    void BuildCommandContext(CommandRun run, ValidationResultSeverity? allowedSeverity)
    {
        var command = run.Command;
        var severityPolicy = CommandValidationResults.ForCommand(command.GetType(), allowedSeverity);
        var commandContext = new CommandContext(
            run.CorrelationId,
            command.GetType(),
            command,
            [],
            BuildContextValues(command, run.Services, run.Prepared),
            severityPolicy.AllowedSeverity,
            ServiceProvider: run.Services,
            CancellationToken: run.CancellationToken)
        {
            PreparedAuthorization = run.Prepared,
            BlockUnknownValidationSeverity = severityPolicy.BlockUnknown,
            ReceivedAt = OperationContextScope.Current ?? default
        };
        run.Context = commandContext;
        contextModifier.SetCurrent(commandContext);
    }

    /// <summary>
    /// Materializes the execution scopes of the run and begins them, synchronous scopes first.
    /// </summary>
    /// <param name="run">The run state, receiving the scopes and the count of those begun.</param>
    /// <returns>A task completing when every scope has begun.</returns>
    async Task BeginExecutionScopes(ExecutionRun run)
    {
        // Materialized once so Begin and Complete are guaranteed to act on the same scope instances, and resolved
        // from the command's own scope rather than the provider that constructed this singleton, so an execution
        // scope depending on a scoped service is created in the scope the command runs in instead of the root.
        ICommandExecutionScope[] scopes = [.. DiscoveredInstances.ResolvedFrom(run.Services, executionScopes)];
        run.Scopes = scopes;
        if (scopes.Any(scope => scope is ICommandOperationExecutionScope { IsCommitParticipant: true }))
        {
            AuthorizationCommandIdentity.MarkTransactional();
        }

        if (run.Frame.MayParticipate)
        {
            run.Frame.Validate(scopes);
        }

        foreach (var executionScope in scopes)
        {
            run.BegunScopeCount++;
            executionScope.Begin(run.Current);
        }

        foreach (var executionScope in scopes.OfType<IAsyncCommandExecutionScope>())
        {
            run.CancellationToken.ThrowIfCancellationRequested();
            await executionScope.BeginAsync(run.Current);
            run.CancellationToken.ThrowIfCancellationRequested();
        }
    }

    /// <summary>
    /// Runs the filters and the approval gate that precede argument resolution.
    /// </summary>
    /// <param name="run">The run state.</param>
    /// <returns>The result to complete the execution with, or null to continue.</returns>
    /// <exception cref="InvalidAuthorizationConfiguration">Thrown when a scheme was selected after execution scopes began.</exception>
    async Task<CommandResult?> RunFilters(ExecutionRun run)
    {
        var commandType = run.Command.GetType();
        run.Result = await commandFilters.OnExecution(run.Current);
        if (run.Snapshot.MembershipDenied)
        {
            return CommandResult.Unauthorized(run.CorrelationId);
        }

        run.Result = FilterValidationResults(run.Result, run.Current);
        if (!run.Result.IsSuccess)
        {
            return run.Result;
        }

        if (run.Prepared is null && run.Current.AuthorizedPrincipal is not null)
        {
            // Dynamically supplied scheme metadata was not visible before execution scopes began.
            throw new InvalidAuthorizationConfiguration($"Command '{commandType}' selected an authentication scheme after execution scopes began.");
        }

        return ApprovalGate.RequiresApproval(commandType) ? await CheckApproval(run) : null;
    }

    /// <summary>
    /// Verifies the authorization verdict and runs the approval gate, which can begin only after authorization.
    /// </summary>
    /// <param name="run">The run state.</param>
    /// <returns>The result to complete the execution with, or null to continue.</returns>
    async Task<CommandResult?> CheckApproval(ExecutionRun run)
    {
        if (!HasCurrentAuthorizationVerdict(run))
        {
            return CommandResult.AuthorizationConfigurationFailure(run.CorrelationId);
        }

        if (run.Snapshot.MembershipDenied)
        {
            return CommandResult.Unauthorized(run.CorrelationId);
        }

        // Approval can begin only after authorization, and never reaches argument resolution or the handler.
        return await ApprovalGate.ForExecution(run.Current, run.Services);
    }

    /// <summary>
    /// Resolves the handler arguments and performs the last authorization checks before the handler may run.
    /// </summary>
    /// <param name="run">The run state, receiving the resolved dependencies.</param>
    /// <param name="commandHandler">The handler whose arguments are resolved.</param>
    /// <returns>The result to complete the execution with, or null to continue.</returns>
    async Task<CommandResult?> ResolveArguments(ExecutionRun run, ICommandHandler commandHandler)
    {
        run.CancellationToken.ThrowIfCancellationRequested();
        var resolution = await argumentResolver.Resolve(commandHandler, run.Current, run.Services, run.Current.AllowedSeverity);

        // Generated values run validators that can swallow the snapshot denial; reject before any such failure is reported.
        if (run.Snapshot.MembershipDenied)
        {
            return CommandResult.Unauthorized(run.CorrelationId);
        }

        run.Result.MergeWith(resolution.ControlResult);
        run.Result = FilterValidationResults(run.Result, run.Current);
        if (!run.Result.IsSuccess)
        {
            return run.Result;
        }

        run.Context = run.Current with { Dependencies = resolution.Arguments };

        run.CancellationToken.ThrowIfCancellationRequested();

        // A custom attribute evaluator can declare a policy without a recognized Authorize attribute, so
        // preparation may not have run. An omitted or replaced authorization filter must not turn that into
        // permission to invoke the handler. Re-resolve even after a successful no-policy filter: a custom
        // evaluator can change requirements on the same target between the filter and invocation. A cached
        // no-policy marker would hide that change. This repeats declaration lookup, not policy evaluation.
        if (!HasCurrentAuthorizationVerdict(run))
        {
            return CommandResult.AuthorizationConfigurationFailure(run.CorrelationId);
        }

        // Read again: a context values provider or modifier can have resolved the tenant since the read above.
        OperationActivity.AddResolvedTenant(run.Activity, run.Services);

        // The single choke point: a denied snapshot never reaches the handler.
        return run.Snapshot.MembershipDenied ? CommandResult.Unauthorized(run.CorrelationId) : null;
    }

    /// <summary>
    /// Determines whether the context holds an authorization verdict that is current for the command.
    /// </summary>
    /// <param name="run">The run state.</param>
    /// <returns>True when the verdict is present where required and still current.</returns>
    /// <exception cref="InvalidAuthorizationConfiguration">Thrown when authorization declarations are unavailable.</exception>
    bool HasCurrentAuthorizationVerdict(ExecutionRun run)
    {
        var commandType = run.Command.GetType();
        var context = run.Current;
        var currentDeclaration = (run.Services.GetService<AuthorizationDeclarations>() ??
            throw new InvalidAuthorizationConfiguration("Authorization declarations are unavailable.")).For(commandType);
        var missingVerdict = context.AuthorizedExecution is null &&
            (run.Prepared?.Declaration.RequiresAsynchronousEvaluation == true ||
             currentDeclaration.RequiresAsynchronousEvaluation);
        return !missingVerdict &&
            (context.AuthorizedExecution is not { } verdict ||
             verdict.IsCurrent(commandType, run.Services.GetRequiredService<ICurrentPrincipalAccessor>(), currentDeclaration));
    }

    /// <summary>
    /// Runs the handler and processes what it returned, either as command operations or as an ordinary response.
    /// </summary>
    /// <param name="run">The run state, receiving the result and context.</param>
    /// <param name="commandHandler">The handler to run.</param>
    /// <returns>A task completing when the response has been processed.</returns>
    /// <exception cref="InvalidCommandOperation">Thrown when the handler returned an ordinary collection of operations.</exception>
    async Task HandleResponse(ExecutionRun run, ICommandHandler commandHandler)
    {
        var response = await Handle(commandHandler, run.Current);
        var values = CommandOperationExecution.Flatten(response).ToArray();
        if (values.Any(value => CommandOperationBoundary.IsBareCollection(value.GetType())))
        {
            throw new InvalidCommandOperation("Use CommandOperations instead of returning an ordinary collection of operation declarations.");
        }

        if (values.Any(value => value is ICommandOperation or CommandOperations))
        {
            await ExecuteOperations(run, values);
        }
        else if (response is not null)
        {
            var processedResult = await ProcessResponseValue(response, run.Current, run.CorrelationId, run.Result);
            run.Context = processedResult.CommandContext;
            run.Result = processedResult.Result;
        }
    }

    /// <summary>
    /// Processes a response holding command operations and executes them when the response succeeded.
    /// </summary>
    /// <param name="run">The run state, receiving the operations, result and context.</param>
    /// <param name="values">The flattened values the handler returned.</param>
    /// <returns>A task completing when the operations have executed.</returns>
    /// <exception cref="InvalidCommandOperation">Thrown when the operations cannot be executed in the current commit state.</exception>
    async Task ExecuteOperations(ExecutionRun run, object[] values)
    {
        if (!run.Frame.MayParticipate)
        {
            throw new InvalidCommandOperation("Declare operation return types explicitly using ICommandOperation, a concrete operation, or CommandOperations. Object-erased operations are unsupported.");
        }

        run.Frame.Validate(run.Scopes!);
        var operationDeclarations = values.SelectMany(value => value switch
        {
            ICommandOperation operation => [operation],
            CommandOperations batch => batch,
            _ => []
        });
        var operations = new CommandOperationExecution(operationDeclarations, run.Services, run.Frame);
        run.Operations = operations;
        run.FailureSource = CommandOperationFailureSource.ResponseHandling;
        var processed = await ProcessOperationResponse(values, run.Current, run.CorrelationId, run.Result);
        run.Context = processed.CommandContext;
        run.Result = FilterValidationResults(processed.Result, run.Current);
        operations.CaptureFailure(run.Result, run.FailureSource);
        if (run.Result.IsSuccess)
        {
            var disposition = CommandOperationExecution.Disposition(run.Scopes!, run.Current);
            if (disposition is not (CommandCommitDisposition.NoCommit or CommandCommitDisposition.NotCommitted))
            {
                throw new InvalidCommandOperation("Operations cannot start after an early, unknown, or mixed business commit.");
            }

            run.FailureSource = CommandOperationFailureSource.Execution;
            await operations.Execute(run.CancellationToken);
        }
    }

    /// <summary>
    /// Completes the begun execution scopes in reverse order, exactly once, then recovers any operations.
    /// </summary>
    /// <param name="run">The run state.</param>
    /// <param name="commandResult">The result to complete the scopes with.</param>
    /// <returns>The result, with scope failures merged in.</returns>
    async Task<CommandResult> CompleteScopes(ExecutionRun run, CommandResult commandResult)
    {
        if (run.Context is null || run.Scopes is not { } scopes || run.ScopesCompleted)
        {
            return commandResult;
        }

        run.ScopesCompleted = true;
        var operations = run.Operations;
        operations?.CaptureFailure(commandResult, run.FailureSource);

        // Scopes nest: the last scope begun is the first completed, like using-blocks. Every scope completes
        // exactly once and in isolation — a failure completing one scope must never prevent the remaining
        // scopes from completing — and a failure becomes an exception outcome on the result rather than a
        // raw exception to the caller.
        for (var index = (run.Frame.MayParticipate ? run.BegunScopeCount : scopes.Length) - 1; index >= 0; index--)
        {
            operations?.RestoreFailure(commandResult);
            await CompleteScope(run, scopes[index], commandResult);
            operations?.CaptureFailure(commandResult, CommandOperationFailureSource.ScopeCompletion);
        }

        if (operations is not null)
        {
            await RecoverOperations(run, operations, commandResult);
        }

        // A response describes what the command produced, and it is bound onto the result as soon as the handler
        // returns it - before the scopes above complete, and therefore before a transaction has committed. Once
        // every scope has had its say, a command that did not succeed produced nothing the caller may act on, so
        // the response is taken back rather than serialized into an error response body.
        if (!commandResult.IsSuccess)
        {
            commandResult.ClearResponse();
        }

        return commandResult;
    }

    /// <summary>
    /// Completes one execution scope, turning a failure into an exception outcome on the result.
    /// </summary>
    /// <param name="run">The run state.</param>
    /// <param name="scope">The scope to complete.</param>
    /// <param name="commandResult">The result to complete it with.</param>
    /// <returns>A task completing when the scope has completed.</returns>
    async Task CompleteScope(ExecutionRun run, ICommandExecutionScope scope, CommandResult commandResult)
    {
        try
        {
            await scope.Complete(run.Current, commandResult);
        }
        catch (Exception ex)
        {
            OperationActivity.RecordException(run.Activity, ex);
            commandResult.MergeWith(CommandResult.FromException(run.CorrelationId, ex));
        }
    }

    /// <summary>
    /// Recovers the operations of the run according to the commit disposition of the completed scopes.
    /// </summary>
    /// <param name="run">The run state.</param>
    /// <param name="operations">The operations to recover.</param>
    /// <param name="commandResult">The result to recover with.</param>
    /// <returns>A task completing when recovery has run.</returns>
    async Task RecoverOperations(ExecutionRun run, CommandOperationExecution operations, CommandResult commandResult)
    {
        var disposition = CommandCommitDisposition.Unknown;
        try
        {
            disposition = CommandOperationExecution.Disposition(run.Scopes!, run.Current);
        }
        catch (Exception exception)
        {
            OperationActivity.RecordException(run.Activity, exception);
            commandResult.MergeWith(CommandResult.FromException(run.CorrelationId, exception));
            operations.CaptureFailure(commandResult, CommandOperationFailureSource.ScopeCompletion);
        }

        await operations.Recover(commandResult, disposition);
    }

    /// <summary>
    /// The per-call inputs shared by the execution and validation cores.
    /// </summary>
    /// <param name="Command">The command.</param>
    /// <param name="AllowedSeverity">The validation severity threshold.</param>
    /// <param name="Activity">The active span, if any.</param>
    /// <param name="CancellationToken">The request cancellation token.</param>
    sealed record CommandCall(object Command, ValidationResultSeverity? AllowedSeverity, Activity? Activity, CancellationToken CancellationToken);

    /// <summary>
    /// The mutable state of one command validation, shared by its steps.
    /// </summary>
    class CommandRun
    {
        public required object Command { get; init; }

        public required IServiceProvider Services { get; init; }

        public required CorrelationId CorrelationId { get; init; }

        public required Activity? Activity { get; init; }

        public required CancellationToken CancellationToken { get; init; }

        public required AuthorizationPrincipalLease PrincipalLease { get; init; }

        public required CommandResult Result { get; set; }

        public PreparedAuthorization? Prepared { get; set; }

        public CommandContext? Context { get; set; }

        public CommandContext Current => Context!;
    }

    /// <summary>
    /// The mutable state of one command execution, shared by its steps and by the invocation scope cleanup.
    /// </summary>
    sealed class ExecutionRun : CommandRun
    {
        public required AuthorizationPrincipalLease IdentityLease { get; init; }

        public required AuthorizationPrincipalLease SnapshotLease { get; init; }

        public required CommandOperationFrame Frame { get; init; }

        public ICommandExecutionScope[]? Scopes { get; set; }

        public int BegunScopeCount { get; set; }

        public bool ScopesCompleted { get; set; }

        public CommandOperationExecution? Operations { get; set; }

        public CommandOperationFailureSource FailureSource { get; set; } = CommandOperationFailureSource.Planning;

        public OperationSnapshot Snapshot { get; set; } = null!;
    }
}
