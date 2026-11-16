// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Arc.Identity.Approvals;
using Cratis.Arc.Observability;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Commands;

#pragma warning disable SA1601 // Partial elements should be documented - documented in CommandPipeline.cs
public partial class CommandPipeline
#pragma warning restore SA1601
{
    /// <summary>
    /// Represents one command execution and the state its authorization, scope and handling steps share.
    /// </summary>
    /// <remarks>
    /// Assignments an async method makes to async-local values never flow back to its caller once it returns. Every step
    /// that sets ambient state - principal, caller identity, snapshot, operation boundary and the synchronous scope
    /// beginnings - is therefore a synchronous method called from <see cref="Run"/>, which keeps the state visible to
    /// the later steps, scope completion and the invocation cleanup, exactly as within a single method.
    /// </remarks>
    /// <param name="pipeline">The pipeline the command runs in.</param>
    /// <param name="invocation">The invocation being executed.</param>
    /// <param name="suppliedAuthorization">Authorization a hosted transport already prepared, if any.</param>
    sealed class Execution(CommandPipeline pipeline, CommandInvocation invocation, PreparedAuthorization? suppliedAuthorization)
    {
        CorrelationId _correlationId = CorrelationId.NotSet;
        CommandResult _result = null!;
        CommandOperationFrame _frame = null!;
        ICommandHandler? _handler;
        PreparedAuthorization? _preparedAuthorization;
        CommandContext? _context;
        ICommandExecutionScope[]? _scopes;
        ICommandExecutionScope[]? _scopesToComplete;
        int _begunScopeCount;
        OperationSnapshot _snapshot = null!;
        CommandOperationExecution? _operations;
        CommandOperationFailureSource _failureSource = CommandOperationFailureSource.Planning;

        /// <summary>
        /// Runs the command through authorization, execution scopes, filters, approval, argument resolution and its handler.
        /// </summary>
        /// <returns>The command result.</returns>
        internal async Task<CommandResult> Run()
        {
            using var validation = CommandValidationExecution.Suspend();
            using var decisionPolicy = CommandDecisionPolicy.Begin(invocation.CommandType, invocation.Services);
            _correlationId = pipeline.GetCorrelationId();
            _result = CommandResult.Success(_correlationId);
            using var principalLease = new AuthorizationPrincipalLease();
            using var identityLease = new AuthorizationPrincipalLease();
            using var snapshotLease = new AuthorizationPrincipalLease();
            if (EnterBoundary() is { } refused)
            {
                return refused;
            }

            using var boundary = _frame;

            // Invocation resources end on every path, only after the scopes completed, like a using declared here.
            using var invocations = new InvocationEnd(this);
            try
            {
                if (LocateHandler() is { } missing)
                {
                    return missing;
                }

                _preparedAuthorization = await PrepareAuthorization(invocation.CommandType, invocation.Services, suppliedAuthorization, invocation.CancellationToken);
                if (EnterCommand(principalLease, identityLease) is { } unauthorized)
                {
                    return unauthorized;
                }

                foreach (var executionScope in _scopes!.OfType<IAsyncCommandExecutionScope>())
                {
                    invocation.CancellationToken.ThrowIfCancellationRequested();
                    await executionScope.BeginAsync(_context!);
                    invocation.CancellationToken.ThrowIfCancellationRequested();
                }

                BeginSnapshot(snapshotLease);
                _result = await pipeline.RunFilters(_context!);
                if (await AdmitFiltered() is { } rejected)
                {
                    return await CompleteExecutionScopes(rejected);
                }

                invocation.CancellationToken.ThrowIfCancellationRequested();
                var resolution = await pipeline.ResolveArguments(_handler!, _context!, invocation.Services);
                if (AdmitResolved(resolution) is { } refusal)
                {
                    return await CompleteExecutionScopes(refusal);
                }

                var response = await pipeline.Handle(_handler!, _context!);
                await HandleResponse(response);
            }
            catch (Exception ex)
            {
                Fail(ex);
            }

            return await CompleteExecutionScopes(_result);
        }

        static void End(CommandContext context, ICommandExecutionScope[] begun, int index)
        {
            if (index < 0)
            {
                return;
            }

            try
            {
                (begun[index] as ICommandInvocationExecutionScope)?.End(context);
            }
            finally
            {
                // Even a faulty cleanup cannot leave another scope's invocation resources alive.
                End(context, begun, index - 1);
            }
        }

        CommandResult? EnterBoundary()
        {
            try
            {
                _frame = pipeline.EnterOperationBoundary(invocation.CommandType);
                return null;
            }
            catch (InvalidCommandOperation exception)
            {
                OperationActivity.RecordException(invocation.Activity, exception);
                return CommandResult.FromException(_correlationId, exception);
            }
        }

        CommandResult? LocateHandler()
        {
            EnsureDecisionSupport(invocation.Services);
            _handler = pipeline.FindHandler(invocation.Command);
            return _handler is null ? CommandResult.MissingHandler(_correlationId, invocation.CommandType) : null;
        }

        CommandResult? EnterCommand(AuthorizationPrincipalLease principalLease, AuthorizationPrincipalLease identityLease)
        {
            var services = invocation.Services;
            if (!AttachAuthorizedPrincipal(_preparedAuthorization, principalLease, services))
            {
                return CommandResult.Unauthorized(_correlationId);
            }

            identityLease.Attach(pipeline.EnterCallerIdentity(services, _preparedAuthorization, false));
            AuthorizationExecutionScopes.MarkWorkStarted(services);
            OperationActivity.AddResolvedTenant(invocation.Activity, services);

            // Held before the modifier runs, so a failure setting it is still mapped against this command's context.
            _context = pipeline.CreateContext(invocation, _correlationId, _preparedAuthorization);
            pipeline.SetCurrentContext(_context);
            BeginExecutionScopes(_context);
            return null;
        }

        void BeginExecutionScopes(CommandContext context)
        {
            // Materialized once so Begin and Complete are guaranteed to act on the same scope instances, and resolved
            // from the command's own scope rather than the provider that constructed this singleton, so an execution
            // scope depending on a scoped service is created in the scope the command runs in instead of the root.
            var scopes = pipeline.ResolveExecutionScopes(invocation.Services);
            _scopes = scopes;
            _scopesToComplete = scopes;
            if (scopes.Any(scope => scope is ICommandOperationExecutionScope { IsCommitParticipant: true }))
            {
                AuthorizationCommandIdentity.MarkTransactional();
            }

            if (_frame.MayParticipate)
            {
                _frame.Validate(scopes);
            }

            foreach (var executionScope in scopes)
            {
                _begunScopeCount++;
                executionScope.Begin(context);
            }
        }

        void BeginSnapshot(AuthorizationPrincipalLease snapshotLease)
        {
            _snapshot = OperationSnapshot.Begin(invocation.Services, () => _context!.AuthorizedPrincipalSnapshot);
            snapshotLease.Attach(_snapshot);
        }

        async ValueTask<CommandResult?> AdmitFiltered()
        {
            if (_snapshot.MembershipDenied)
            {
                return CommandResult.Unauthorized(_correlationId);
            }

            _result = pipeline.FilterValidationResults(_result, _context!);
            if (!_result.IsSuccess)
            {
                return _result;
            }

            if (_preparedAuthorization is null && _context!.AuthorizedPrincipal is not null)
            {
                // Dynamically supplied scheme metadata was not visible before execution scopes began.
                throw new InvalidAuthorizationConfiguration($"Command '{invocation.CommandType}' selected an authentication scheme after execution scopes began.");
            }

            return ApprovalGate.RequiresApproval(invocation.CommandType) ? await Approve() : null;
        }

        Task<CommandResult?> Approve()
        {
            if (!HasCurrentAuthorizationVerdict())
            {
                return Task.FromResult<CommandResult?>(CommandResult.AuthorizationConfigurationFailure(_correlationId));
            }

            if (_snapshot.MembershipDenied)
            {
                return Task.FromResult<CommandResult?>(CommandResult.Unauthorized(_correlationId));
            }

            // Approval can begin only after authorization, and never reaches argument resolution or the handler.
            return ApprovalGate.ForExecution(_context!, invocation.Services);
        }

        CommandResult? AdmitResolved(CommandHandlerArgumentResolution resolution)
        {
            // Generated values run validators that can swallow the snapshot denial; reject before any such failure is reported.
            if (_snapshot.MembershipDenied)
            {
                return CommandResult.Unauthorized(_correlationId);
            }

            _result.MergeWith(resolution.ControlResult);
            _result = pipeline.FilterValidationResults(_result, _context!);
            if (!_result.IsSuccess)
            {
                return _result;
            }

            _context = _context! with { Dependencies = resolution.Arguments };
            invocation.CancellationToken.ThrowIfCancellationRequested();

            // A custom attribute evaluator can declare a policy without a recognized Authorize attribute, so
            // preparation may not have run. An omitted or replaced authorization filter must not turn that into
            // permission to invoke the handler. Re-resolve even after a successful no-policy filter: a custom
            // evaluator can change requirements on the same target between the filter and invocation. A cached
            // no-policy marker would hide that change. This repeats declaration lookup, not policy evaluation.
            if (!HasCurrentAuthorizationVerdict())
            {
                return CommandResult.AuthorizationConfigurationFailure(_correlationId);
            }

            // Read again: a context values provider or modifier can have resolved the tenant since the read above.
            OperationActivity.AddResolvedTenant(invocation.Activity, invocation.Services);

            // The single choke point: a denied snapshot never reaches the handler.
            return _snapshot.MembershipDenied ? CommandResult.Unauthorized(_correlationId) : null;
        }

        bool HasCurrentAuthorizationVerdict()
        {
            var currentDeclaration = (invocation.Services.GetService<AuthorizationDeclarations>() ??
                throw new InvalidAuthorizationConfiguration("Authorization declarations are unavailable.")).For(invocation.CommandType);
            var missingVerdict = _context!.AuthorizedExecution is null &&
                (_preparedAuthorization?.Declaration.RequiresAsynchronousEvaluation == true ||
                 currentDeclaration.RequiresAsynchronousEvaluation);
            return !missingVerdict &&
                (_context.AuthorizedExecution is not { } verdict ||
                 verdict.IsCurrent(invocation.CommandType, invocation.Services.GetRequiredService<ICurrentPrincipalAccessor>(), currentDeclaration));
        }

        async ValueTask HandleResponse(object? response)
        {
            var values = CommandOperationExecution.Flatten(response).ToArray();
            if (values.Any(value => CommandOperationBoundary.IsBareCollection(value.GetType())))
            {
                throw new InvalidCommandOperation("Use CommandOperations instead of returning an ordinary collection of operation declarations.");
            }

            if (values.Any(value => value is ICommandOperation or CommandOperations))
            {
                await ExecuteOperations(values);
                return;
            }

            if (response is not null)
            {
                (_context, _result) = await pipeline.ProcessResponseValue(response, _context!, _correlationId, _result);
            }
        }

        async ValueTask ExecuteOperations(object[] values)
        {
            if (!_frame.MayParticipate)
            {
                throw new InvalidCommandOperation("Declare operation return types explicitly using ICommandOperation, a concrete operation, or CommandOperations. Object-erased operations are unsupported.");
            }

            _frame.Validate(_scopes!);
            var operationDeclarations = values.SelectMany(value => value switch
            {
                ICommandOperation operation => [operation],
                CommandOperations batch => batch,
                _ => []
            });
            _operations = new CommandOperationExecution(operationDeclarations, invocation.Services, _frame);
            _failureSource = CommandOperationFailureSource.ResponseHandling;
            var processed = await pipeline.ProcessOperationResponse(values, _context!, _correlationId, _result);
            _context = processed.CommandContext;
            _result = pipeline.FilterValidationResults(processed.Result, _context);
            _operations.CaptureFailure(_result, _failureSource);
            if (!_result.IsSuccess)
            {
                return;
            }

            var disposition = CommandOperationExecution.Disposition(_scopes!, _context);
            if (disposition is not (CommandCommitDisposition.NoCommit or CommandCommitDisposition.NotCommitted))
            {
                throw new InvalidCommandOperation("Operations cannot start after an early, unknown, or mixed business commit.");
            }

            _failureSource = CommandOperationFailureSource.Execution;
            await _operations.Execute(invocation.CancellationToken);
        }

        void Fail(Exception exception)
        {
            OperationActivity.RecordException(invocation.Activity, exception);
            if (exception is InvalidAuthorizationConfiguration)
            {
                invocation.Services.GetService<ILogger<CommandPipeline>>()?.AuthorizationConfigurationFailed(exception);
                _result.MergeWith(CommandResult.AuthorizationConfigurationFailure(_correlationId));
                _operations?.CaptureFailure(_result, _failureSource);
                return;
            }

            _result.MergeWith(MapException(_correlationId, _context, exception));
            _operations?.CaptureFailure(_result, exception is OperationCanceledException ? CommandOperationFailureSource.Cancellation : _failureSource);
        }

        Task<CommandResult> CompleteExecutionScopes(CommandResult result)
        {
            if (_scopesToComplete is not { } scopes)
            {
                return Task.FromResult(result);
            }

            _scopesToComplete = null;
            _operations?.CaptureFailure(result, _failureSource);
            return CompleteScopes(scopes, _frame.MayParticipate ? _begunScopeCount : scopes.Length, result);
        }

        async Task<CommandResult> CompleteScopes(ICommandExecutionScope[] scopes, int count, CommandResult result)
        {
            var context = _context!;

            // Scopes nest: the last scope begun is the first completed, like using-blocks. Every scope completes
            // exactly once and in isolation — a failure completing one scope must never prevent the remaining
            // scopes from completing — and a failure becomes an exception outcome on the result rather than a
            // raw exception to the caller.
            for (var index = count - 1; index >= 0; index--)
            {
                _operations?.RestoreFailure(result);
                try
                {
                    await scopes[index].Complete(context, result);
                }
                catch (Exception ex)
                {
                    OperationActivity.RecordException(invocation.Activity, ex);
                    result.MergeWith(CommandResult.FromException(context.CorrelationId, ex));
                }

                _operations?.CaptureFailure(result, CommandOperationFailureSource.ScopeCompletion);
            }

            if (_operations is { } operations)
            {
                await RecoverOperations(context, scopes, operations, result);
            }

            // A response describes what the command produced, and it is bound onto the result as soon as the handler
            // returns it - before the scopes above complete, and therefore before a transaction has committed. Once
            // every scope has had its say, a command that did not succeed produced nothing the caller may act on, so
            // the response is taken back rather than serialized into an error response body.
            if (!result.IsSuccess)
            {
                result.ClearResponse();
            }

            return result;
        }

        async ValueTask RecoverOperations(CommandContext context, ICommandExecutionScope[] scopes, CommandOperationExecution operations, CommandResult result)
        {
            var disposition = CommandCommitDisposition.Unknown;
            try
            {
                disposition = CommandOperationExecution.Disposition(scopes, context);
            }
            catch (Exception exception)
            {
                OperationActivity.RecordException(invocation.Activity, exception);
                result.MergeWith(CommandResult.FromException(context.CorrelationId, exception));
                operations.CaptureFailure(result, CommandOperationFailureSource.ScopeCompletion);
            }

            await operations.Recover(result, disposition);
        }

        void EndInvocations()
        {
            if (_context is { } context && _scopes is { } begun)
            {
                End(context, begun, _begunScopeCount - 1);
            }
        }

        readonly struct InvocationEnd(Execution execution) : IDisposable
        {
            public void Dispose() => execution.EndInvocations();
        }
    }
}
