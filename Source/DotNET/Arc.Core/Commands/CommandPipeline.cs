// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Arc.Authorization;
using Cratis.Arc.Identity.Approvals;
using Cratis.Arc.Observability;
using Cratis.Arc.Validation;
using Cratis.DependencyInjection;
using Cratis.Execution;
using Cratis.Traces;
using Cratis.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Commands;

/// <summary>
/// Represents an implementation of <see cref="ICommandPipeline"/>.
/// </summary>
/// <param name="correlationIdAccessor">The <see cref="ICorrelationIdAccessor"/> to use for accessing correlation IDs.</param>
/// <param name="commandFilters">The <see cref="ICommandFilters"/> to use for filtering commands.</param>
/// <param name="handlerProviders">The <see cref="ICommandHandlerProviders"/> to use for finding command handlers.</param>
/// <param name="valueHandlers">The <see cref="ICommandResponseValueHandlers"/> to use for handling response values.</param>
/// <param name="contextModifier">The <see cref="ICommandContextModifier"/> to use for setting the current command context.</param>
/// <param name="contextValuesBuilder">The <see cref="ICommandContextValuesBuilder"/> to use for building command context values.</param>
/// <param name="argumentResolver">The <see cref="ICommandHandlerArgumentResolver"/> to use for resolving handler arguments.</param>
/// <param name="executionScopes">The discovered <see cref="ICommandExecutionScope"/> implementations that participate in every command's execution.</param>
/// <param name="scopeFactory">The <see cref="IServiceScopeFactory"/> used to create a dedicated service scope when no <see cref="IServiceProvider"/> is provided.</param>
/// <param name="activitySource">The <see cref="IActivitySource{T}"/> for tracing.</param>
[Singleton]
public partial class CommandPipeline(
    ICorrelationIdAccessor correlationIdAccessor,
    ICommandFilters commandFilters,
    ICommandHandlerProviders handlerProviders,
    ICommandResponseValueHandlers valueHandlers,
    ICommandContextModifier contextModifier,
    ICommandContextValuesBuilder contextValuesBuilder,
    ICommandHandlerArgumentResolver argumentResolver,
    IInstancesOf<ICommandExecutionScope> executionScopes,
    IServiceScopeFactory scopeFactory,
    IActivitySource<CommandPipeline> activitySource) : ICommandPipelineWithCancellation
{
    /// <inheritdoc/>
    public async Task<CommandResult> Execute(object command, ValidationResultSeverity? allowedSeverity = default)
    {
        using var scope = scopeFactory.CreateScope();
        using var ownership = AuthorizationExecutionScopes.Begin(scope.ServiceProvider);
        return await Execute(command, scope.ServiceProvider, allowedSeverity, CancellationToken.None);
    }

    /// <inheritdoc/>
    public async Task<CommandResult> Execute(object command, ValidationResultSeverity? allowedSeverity, CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        using var ownership = AuthorizationExecutionScopes.Begin(scope.ServiceProvider);
        return await Execute(command, scope.ServiceProvider, allowedSeverity, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<CommandResult<TResult>> Execute<TResult>(object command, ValidationResultSeverity? allowedSeverity = default)
    {
        using var scope = scopeFactory.CreateScope();
        using var ownership = AuthorizationExecutionScopes.Begin(scope.ServiceProvider);
        return await Execute<TResult>(command, scope.ServiceProvider, allowedSeverity, CancellationToken.None);
    }

    /// <inheritdoc/>
    public async Task<CommandResult<TResult>> Execute<TResult>(object command, ValidationResultSeverity? allowedSeverity, CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        using var ownership = AuthorizationExecutionScopes.Begin(scope.ServiceProvider);
        return await Execute<TResult>(command, scope.ServiceProvider, allowedSeverity, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<CommandResult> Validate(object command, ValidationResultSeverity? allowedSeverity = default)
    {
        using var scope = scopeFactory.CreateScope();
        using var ownership = AuthorizationExecutionScopes.Begin(scope.ServiceProvider);
        return await Validate(command, scope.ServiceProvider, allowedSeverity, CancellationToken.None);
    }

    /// <inheritdoc/>
    public async Task<CommandResult> Validate(object command, ValidationResultSeverity? allowedSeverity, CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        using var ownership = AuthorizationExecutionScopes.Begin(scope.ServiceProvider);
        return await Validate(command, scope.ServiceProvider, allowedSeverity, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<CommandResult> Execute(object command, IServiceProvider serviceProvider, ValidationResultSeverity? allowedSeverity = default) =>
        Execute(command, serviceProvider, allowedSeverity, CancellationToken.None);

    /// <inheritdoc/>
    public async Task<CommandResult> Execute(object command, IServiceProvider serviceProvider, ValidationResultSeverity? allowedSeverity, CancellationToken cancellationToken)
    {
        using var receipt = OperationContextScope.BeginPipeline(serviceProvider);
        return await TraceExecution(
            command,
            serviceProvider,
            activity => ExecuteCore(new CommandCall(command, allowedSeverity, activity, cancellationToken), serviceProvider, null),
            cancellationToken);
    }

    /// <inheritdoc/>
    public Task<CommandResult<TResult>> Execute<TResult>(object command, IServiceProvider serviceProvider, ValidationResultSeverity? allowedSeverity = default) =>
        Execute<TResult>(command, serviceProvider, allowedSeverity, CancellationToken.None);

    /// <inheritdoc/>
    public async Task<CommandResult<TResult>> Execute<TResult>(object command, IServiceProvider serviceProvider, ValidationResultSeverity? allowedSeverity, CancellationToken cancellationToken)
    {
        var result = await Execute(command, serviceProvider, allowedSeverity, cancellationToken);
        if (result is CommandResult<TResult> typed)
        {
            return typed;
        }

        // The pipeline builds the result as CommandResult<runtimeTypeOfResponse>, which is not CommandResult<TResult>
        // even when the response is assignable to TResult (generics are invariant, so CommandResult<Dog> is not a
        // CommandResult<IAnimal>). Re-wrap when there is no response, or the response is a TResult — covering both the
        // failure/no-response case and a TResult that is an interface or base type. Only a genuine type mismatch falls
        // through to the cast, which still throws the documented InvalidCastException.
        var response = result.ResponseValue;
        if (response is null || response is TResult)
        {
            return new CommandResult<TResult>
            {
                CorrelationId = result.CorrelationId,
                IsAuthorized = result.IsAuthorized,
                ValidationResults = result.ValidationResults,
                ExceptionMessages = result.ExceptionMessages,
                ExceptionStackTrace = result.ExceptionStackTrace,
                AuthorizationFailureReason = result.AuthorizationFailureReason,
                AuthorizationConfigurationFailed = result.AuthorizationConfigurationFailed,
                Recovery = result.Recovery,
                OperationOutcomes = result.OperationOutcomes,
                Response = response is TResult typedResponse ? typedResponse : default
            };
        }

        return (CommandResult<TResult>)result;
    }

    /// <summary>
    /// Executes a command for a hosted transport, preparing authentication and an owned identity-bound scope when required.
    /// </summary>
    /// <param name="command">The command.</param>
    /// <param name="requestServices">The original request provider.</param>
    /// <param name="allowedSeverity">The validation severity threshold.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The command result.</returns>
    public async Task<CommandResult> ExecuteHosted(object command, IServiceProvider requestServices, ValidationResultSeverity? allowedSeverity, CancellationToken cancellationToken)
    {
        // The span starts before authorization is prepared, so a request turned away on the way in is traced and
        // measured like one turned away inside the pipeline.
        return await TraceExecution(
            command,
            requestServices,
            activity => ExecuteHostedCore(new CommandCall(command, allowedSeverity, activity, cancellationToken), requestServices),
            cancellationToken);
    }

    /// <inheritdoc/>
    public Task<CommandResult> Validate(object command, IServiceProvider serviceProvider, ValidationResultSeverity? allowedSeverity = default) =>
        Validate(command, serviceProvider, allowedSeverity, CancellationToken.None);

    /// <inheritdoc/>
    public async Task<CommandResult> Validate(object command, IServiceProvider serviceProvider, ValidationResultSeverity? allowedSeverity, CancellationToken cancellationToken)
    {
        using var receipt = OperationContextScope.BeginPipeline(serviceProvider);
        return await TraceValidation(
            command,
            activity => ValidateCore(new CommandCall(command, allowedSeverity, activity, cancellationToken), serviceProvider, null),
            cancellationToken);
    }

    /// <summary>
    /// Gets the telemetry name of a command type.
    /// </summary>
    /// <param name="type">The command type.</param>
    /// <returns>The full name, or the short name when there is none.</returns>
    internal static string TypeNameOf(Type type) => type.FullName ?? type.Name;

    /// <summary>
    /// Gets the authorization declaration of a command type, recording a configuration failure as a null result.
    /// </summary>
    /// <param name="commandType">The command type.</param>
    /// <param name="services">The request services.</param>
    /// <param name="activity">The active span, if any.</param>
    /// <returns>The declaration, or null when authorization is misconfigured.</returns>
    /// <exception cref="InvalidAuthorizationConfiguration">The declarations are unavailable; reported as a null result.</exception>
    internal static AuthorizationDeclaration? GetHostedDeclaration(Type commandType, IServiceProvider services, Activity? activity)
    {
        try
        {
            var declarations = services.GetService<AuthorizationDeclarations>() ??
                throw new InvalidAuthorizationConfiguration("Authorization declarations are unavailable.");
            return declarations.For(commandType);
        }
        catch (Exception exception) when (exception is InvalidAuthorizationConfiguration or AmbiguousAuthorizationLevel)
        {
            OperationActivity.RecordException(activity, exception);
            services.GetService<ILogger<CommandPipeline>>()?.AuthorizationConfigurationFailed(exception);
            return null;
        }
    }

    /// <summary>
    /// Ensures protected command decisions are supported by the request services.
    /// </summary>
    /// <param name="services">The request services.</param>
    /// <exception cref="ProtectedDecisionsRequireCommandAwareReader">Protected decisions have no command-aware reader.</exception>
    internal static void EnsureDecisionSupport(IServiceProvider services)
    {
        CommandDecisionPolicy.ThrowIfConflicting();
        if (CommandDecisionPolicy.IsProtected)
        {
            var support = services.GetService<ICommandProtectedDecisionSupport>() ??
                throw new ProtectedDecisionsRequireCommandAwareReader();
            support.EnsureSupported(services);
        }
    }

    /// <summary>
    /// Records the outcome of a command on its span and metrics.
    /// </summary>
    /// <param name="activity">The active span, if any.</param>
    /// <param name="serviceProvider">The request services.</param>
    /// <param name="commandType">The command type.</param>
    /// <param name="result">The result, or null when the command failed.</param>
    /// <param name="started">The start timestamp.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    internal static void RecordCommand(Activity? activity, IServiceProvider serviceProvider, Type commandType, CommandResult? result, long started, CancellationToken cancellationToken)
    {
        var metrics = serviceProvider.GetService<PipelineMetrics>();
        var measured = metrics?.CommandsEnabled == true;
        if (activity is not { IsAllDataRequested: true } && !measured)
        {
            return;
        }

        var outcome = result is null ? OperationOutcomes.ForException(cancellationToken) : OperationOutcomes.For(result, cancellationToken);
        if (result is not null)
        {
            OperationActivity.AddCorrelationId(activity, result.CorrelationId);
        }

        OperationActivity.RecordOutcome(activity, WellKnownTelemetryNames.CommandOutcome, outcome, result?.ValidationResults ?? []);
        if (measured)
        {
            metrics!.RecordCommand(TypeNameOf(commandType), outcome, Stopwatch.GetElapsedTime(started));
        }
    }

    /// <summary>
    /// Records a command failure on its span and metrics.
    /// </summary>
    /// <param name="activity">The active span, if any.</param>
    /// <param name="serviceProvider">The request services.</param>
    /// <param name="commandType">The command type.</param>
    /// <param name="exception">The failure.</param>
    /// <param name="started">The start timestamp.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    internal static void RecordCommandFailure(Activity? activity, IServiceProvider serviceProvider, Type commandType, Exception exception, long started, CancellationToken cancellationToken)
    {
        OperationActivity.RecordException(activity, exception);
        RecordCommand(activity, serviceProvider, commandType, null, started, cancellationToken);
    }

    /// <summary>
    /// Records the outcome of a validation on its span.
    /// </summary>
    /// <param name="activity">The active span, if any.</param>
    /// <param name="result">The result.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    internal static void RecordValidation(Activity? activity, CommandResult result, CancellationToken cancellationToken)
    {
        if (activity is { IsAllDataRequested: true })
        {
            OperationActivity.AddCorrelationId(activity, result.CorrelationId);
            OperationActivity.RecordOutcome(activity, WellKnownTelemetryNames.CommandOutcome, OperationOutcomes.For(result, cancellationToken), result.ValidationResults);
        }
    }

    /// <summary>
    /// Records a validation failure on its span.
    /// </summary>
    /// <param name="activity">The active span, if any.</param>
    /// <param name="exception">The failure.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    internal static void RecordValidationFailure(Activity? activity, Exception exception, CancellationToken cancellationToken)
    {
        OperationActivity.RecordException(activity, exception);
        OperationActivity.RecordOutcome(activity, WellKnownTelemetryNames.CommandOutcome, OperationOutcomes.ForException(cancellationToken), []);
    }

    /// <summary>
    /// Prepares validation-only HTTP authentication before any identity-bound command context providers run.
    /// </summary>
    /// <param name="command">The command to validate.</param>
    /// <param name="requestServices">The original request provider.</param>
    /// <param name="allowedSeverity">The severity threshold.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The validation result.</returns>
    internal async Task<CommandResult> ValidateHosted(object command, IServiceProvider requestServices, ValidationResultSeverity? allowedSeverity, CancellationToken cancellationToken)
    {
        return await TraceValidation(
            command,
            activity => ValidateHostedCore(new CommandCall(command, allowedSeverity, activity, cancellationToken), requestServices),
            cancellationToken);
    }

    /// <summary>
    /// Reports a body-binding failure only after resource-free authorization, without creating a command context.
    /// </summary>
    /// <param name="request">The <see cref="UnboundCommandRefusalRequest"/> describing the unbound command.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The authorization refusal or the original binding failure.</returns>
    internal Task<CommandResult> RefuseUnboundHosted(UnboundCommandRefusalRequest request, CancellationToken cancellationToken) =>
        new UnboundCommandRefusal(activitySource, GetCorrelationId, PrepareHostedType).Refuse(request, cancellationToken);

    static CommandResult MapException(CorrelationId correlationId, CommandContext? context, Exception exception)
    {
        if (UnauthorizedCause.IsCauseOf(exception) || ReadModelKeyResolutionDenied.IsCauseOf(exception))
        {
            return CommandResult.Unauthorized(correlationId);
        }

        try
        {
            if (context?.ServiceProvider?.GetService<IInstancesOf<ICommandExceptionMapper>>() is { } mappers)
            {
                foreach (var mapper in mappers)
                {
                    if (mapper.TryMap(context, exception, out var mapped))
                    {
                        return mapped;
                    }
                }
            }
        }
        catch (Exception mappingFailure)
        {
            // A failed mapper still fails the command and lets execution scopes roll back their pending work.
            return CommandResult.Error(correlationId, mappingFailure);
        }

        return CommandResult.FromException(correlationId, exception);
    }

    async Task<CommandResult> ExecuteHostedCore(CommandCall call, IServiceProvider requestServices)
    {
        var (command, _, activity, cancellationToken) = call;
        using var decisionPolicy = CommandDecisionPolicy.Begin(command.GetType(), requestServices);
        try
        {
            EnsureDecisionSupport(requestServices);
        }
        catch (Exception exception)
        {
            OperationActivity.RecordException(activity, exception);
            return CommandResult.FromException(GetCorrelationId(), exception);
        }
        using var receipt = OperationContextScope.BeginIfNotSet(requestServices);
        var declaration = GetHostedDeclaration(command.GetType(), requestServices, activity);
        if (declaration is null)
        {
            return CommandResult.AuthorizationConfigurationFailure(GetCorrelationId());
        }

        if (!declaration.RequiresAsynchronousEvaluation)
        {
            return await ExecuteCore(call, requestServices, null);
        }

        var (prepared, failure) = await PrepareHosted(command, requestServices, activity, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        if (!prepared!.PrincipalChanged)
        {
            return await ExecuteCore(call, requestServices, prepared);
        }

        using var scope = scopeFactory.CreateScope();
        using var ownership = AuthorizationExecutionScopes.Begin(scope.ServiceProvider);
        return await ExecuteCore(call, scope.ServiceProvider, prepared);
    }

    async Task<CommandResult> ValidateHostedCore(CommandCall call, IServiceProvider requestServices)
    {
        var (command, _, activity, cancellationToken) = call;
        using var decisionPolicy = CommandDecisionPolicy.Begin(command.GetType(), requestServices);
        try
        {
            EnsureDecisionSupport(requestServices);
        }
        catch (Exception exception)
        {
            OperationActivity.RecordException(activity, exception);
            return CommandResult.FromException(GetCorrelationId(), exception);
        }
        using var receipt = OperationContextScope.BeginIfNotSet(requestServices);
        var declaration = GetHostedDeclaration(command.GetType(), requestServices, activity);
        if (declaration is null)
        {
            return CommandResult.AuthorizationConfigurationFailure(GetCorrelationId());
        }

        if (!declaration.RequiresAsynchronousEvaluation)
        {
            return await ValidateCore(call, requestServices, null);
        }

        var (prepared, failure) = await PrepareHosted(command, requestServices, activity, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        if (!prepared!.PrincipalChanged)
        {
            return await ValidateCore(call, requestServices, prepared);
        }

        using var scope = scopeFactory.CreateScope();
        using var ownership = AuthorizationExecutionScopes.Begin(scope.ServiceProvider);
        return await ValidateCore(call, scope.ServiceProvider, prepared);
    }

    Task<(PreparedAuthorization? Authorization, CommandResult? Failure)> PrepareHosted(object command, IServiceProvider services, Activity? activity, CancellationToken token) =>
        PrepareHostedType(command.GetType(), services, activity, token);

    async Task<(PreparedAuthorization? Authorization, CommandResult? Failure)> PrepareHostedType(Type commandType, IServiceProvider services, Activity? activity, CancellationToken token)
    {
        try
        {
            var authorization = await (services.GetService<AuthorizationEvaluation>() ??
                throw new InvalidAuthorizationConfiguration("Authorization evaluation is unavailable.")).Prepare(commandType, services, token);
            return (authorization, null);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (UnauthorizedCause.IsCauseOf(exception))
        {
            OperationActivity.RecordException(activity, exception);
            return (null, CommandResult.Unauthorized(GetCorrelationId()));
        }
        catch (InvalidAuthorizationConfiguration exception)
        {
            OperationActivity.RecordException(activity, exception);
            services.GetService<ILogger<CommandPipeline>>()?.AuthorizationConfigurationFailed(exception);
            return (null, CommandResult.AuthorizationConfigurationFailure(GetCorrelationId()));
        }
        catch (Exception exception)
        {
            OperationActivity.RecordException(activity, exception);
            services.GetService<ILogger<CommandPipeline>>()?.AuthorizationPreparationFailed(exception);
            return (null, CommandResult.Error(GetCorrelationId(), "An error occurred while preparing authorization."));
        }
    }

    async Task<CommandResult> ExecuteCore(CommandCall call, IServiceProvider serviceProvider, PreparedAuthorization? suppliedAuthorization)
    {
        var (command, allowedSeverity, activity, cancellationToken) = call;
        using var execution = CommandValidationExecution.Suspend();
        using var decisionPolicy = CommandDecisionPolicy.Begin(command.GetType(), serviceProvider);
        var correlationId = GetCorrelationId();
        var result = CommandResult.Success(correlationId);
        using var principalLease = new AuthorizationPrincipalLease();
        using var identityLease = new AuthorizationPrincipalLease();
        using var snapshotLease = new AuthorizationPrincipalLease();
        CommandOperationFrame frame;
        try
        {
            frame = CommandOperationBoundary.Enter(command.GetType(), scopeFactory);
        }
        catch (InvalidCommandOperation exception)
        {
            OperationActivity.RecordException(activity, exception);
            return CommandResult.FromException(correlationId, exception);
        }

        using var boundary = frame;
        var run = new ExecutionRun
        {
            Command = command,
            Services = serviceProvider,
            CorrelationId = correlationId,
            Activity = activity,
            CancellationToken = cancellationToken,
            PrincipalLease = principalLease,
            IdentityLease = identityLease,
            SnapshotLease = snapshotLease,
            Result = result,
            Frame = frame
        };
        using var invocationScopes = new InvocationScopes(run);
        return await RunExecution(run, suppliedAuthorization, allowedSeverity);
    }

    async Task<CommandResult> RunExecution(ExecutionRun run, PreparedAuthorization? suppliedAuthorization, ValidationResultSeverity? allowedSeverity)
    {
        var command = run.Command;
        var serviceProvider = run.Services;
        var activity = run.Activity;
        var correlationId = run.CorrelationId;
        try
        {
            EnsureDecisionSupport(serviceProvider);
            handlerProviders.TryGetHandlerFor(command, out var commandHandler);
            if (commandHandler is null)
            {
                return CommandResult.MissingHandler(correlationId, command.GetType());
            }

            if (await PrepareAuthorization(run, suppliedAuthorization) is { } unauthorized)
            {
                return unauthorized;
            }

            run.IdentityLease.Attach(EnterCallerIdentity(serviceProvider, run.Prepared, false));
            AuthorizationExecutionScopes.MarkWorkStarted(serviceProvider);
            OperationActivity.AddResolvedTenant(activity, serviceProvider);

            BuildCommandContext(run, allowedSeverity);
            await BeginExecutionScopes(run);

            run.Snapshot = OperationSnapshot.Begin(serviceProvider, () => run.Current.AuthorizedPrincipalSnapshot);
            run.SnapshotLease.Attach(run.Snapshot);
            var earlyResult = await RunFilters(run) ?? await ResolveArguments(run, commandHandler);
            if (earlyResult is not null)
            {
                return await CompleteScopes(run, earlyResult);
            }

            await HandleResponse(run, commandHandler);
        }
        catch (InvalidAuthorizationConfiguration ex)
        {
            OperationActivity.RecordException(activity, ex);
            serviceProvider.GetService<ILogger<CommandPipeline>>()?.AuthorizationConfigurationFailed(ex);
            run.Result.MergeWith(CommandResult.AuthorizationConfigurationFailure(correlationId));
            run.Operations?.CaptureFailure(run.Result, run.FailureSource);
        }
        catch (Exception ex)
        {
            OperationActivity.RecordException(activity, ex);
            run.Result.MergeWith(MapException(correlationId, run.Context, ex));
            run.Operations?.CaptureFailure(run.Result, ex is OperationCanceledException ? CommandOperationFailureSource.Cancellation : run.FailureSource);
        }

        return await CompleteScopes(run, run.Result);
    }

    /// <summary>
    /// Runs the handler, in a handle span when anything listens.
    /// </summary>
    /// <param name="handler">The <see cref="ICommandHandler"/> to run.</param>
    /// <param name="context">The <see cref="CommandContext"/> to run it with.</param>
    /// <returns>The value the handler returned.</returns>
    /// <remarks>
    /// The span is started inside the async method, never here. A span started in a synchronous caller becomes
    /// Activity.Current on the caller's execution context, and the async method stopping it only restores its own
    /// context, so the stopped span would stay current for every span the pipeline starts afterwards.
    /// </remarks>
    ValueTask<object?> Handle(ICommandHandler handler, CommandContext context) =>
        activitySource.ActualSource.HasListeners() ? HandleTraced(handler, context) : handler.Handle(context);

    async ValueTask<object?> HandleTraced(ICommandHandler handler, CommandContext context)
    {
        using var activity = OperationActivity.StartCommandChild(activitySource.ActualSource, WellKnownTelemetryNames.CommandHandleSpan, context.Type);
        try
        {
            return await handler.Handle(context);
        }
        catch (Exception ex)
        {
            OperationActivity.RecordFailure(activity, ex, context.CancellationToken);
            throw;
        }
    }

    async Task<CommandResult> ValidateCore(CommandCall call, IServiceProvider serviceProvider, PreparedAuthorization? suppliedAuthorization)
    {
        var (command, allowedSeverity, activity, cancellationToken) = call;
        using var decisionPolicy = CommandDecisionPolicy.Begin(command.GetType(), serviceProvider);
        using var validation = CommandValidationExecution.Begin(command.GetType());
        var correlationId = GetCorrelationId();
        var result = CommandResult.Success(correlationId);
        using var principalLease = new AuthorizationPrincipalLease();
        var run = new CommandRun
        {
            Command = command,
            Services = serviceProvider,
            CorrelationId = correlationId,
            Activity = activity,
            CancellationToken = cancellationToken,
            PrincipalLease = principalLease,
            Result = result
        };
        try
        {
            EnsureDecisionSupport(serviceProvider);
            handlerProviders.TryGetHandlerFor(command, out var commandHandler);
            if (commandHandler is null)
            {
                return CommandResult.MissingHandler(correlationId, command.GetType());
            }

            if (await PrepareAuthorization(run, suppliedAuthorization) is { } unauthorized)
            {
                return unauthorized;
            }

            using var identityGuard = EnterCallerIdentity(serviceProvider, run.Prepared, true);
            AuthorizationExecutionScopes.MarkWorkStarted(serviceProvider);
            OperationActivity.AddResolvedTenant(activity, serviceProvider);
            BuildCommandContext(run, allowedSeverity);

            // Run only filters (authorization and validation), skip handler execution and argument resolution
            using var snapshot = OperationSnapshot.Begin(serviceProvider, () => run.Current.AuthorizedPrincipalSnapshot);
            run.Result = await commandFilters.OnExecution(run.Current);
            if (snapshot.MembershipDenied)
            {
                return CommandResult.Unauthorized(correlationId);
            }

            run.Result = FilterValidationResults(run.Result, run.Current);
            if (run.Result.IsSuccess && ApprovalGate.ForValidation(run.Current, serviceProvider) is { } refusal)
            {
                run.Result = refusal;
            }
        }
        catch (InvalidAuthorizationConfiguration ex)
        {
            OperationActivity.RecordException(activity, ex);
            serviceProvider.GetService<ILogger<CommandPipeline>>()?.AuthorizationConfigurationFailed(ex);
            run.Result.MergeWith(CommandResult.AuthorizationConfigurationFailure(correlationId));
        }
        catch (Exception ex)
        {
            OperationActivity.RecordException(activity, ex);
            run.Result.MergeWith(CommandResult.FromException(correlationId, ex));
        }

        return run.Result;
    }

    IDisposable EnterCallerIdentity(IServiceProvider services, PreparedAuthorization? prepared, bool forValidation)
    {
        var principal = prepared is { PrincipalChanged: true } changed ? changed.SelectedPrincipal : services.GetService<ICurrentPrincipalAccessor>()?.Current;
        var provenance = prepared?.PrincipalChanged == true ? CommandCallerProvenance.SchemeSelected : CommandCallerProvenance.Ambient;
        return forValidation
            ? AuthorizationCommandIdentity.EnterForValidation(principal, scopeFactory, provenance)
            : AuthorizationCommandIdentity.Enter(principal, scopeFactory, provenance);
    }

    CommandContextValues BuildContextValues(object command, IServiceProvider services, PreparedAuthorization? prepared)
    {
        if (prepared?.PrincipalChanged != true)
        {
            return contextValuesBuilder.Build(command);
        }

        var builder = services.GetRequiredService<ICommandContextValuesBuilder>();
        if (builder is not IScopedCommandContextValuesBuilder scoped)
        {
            throw new InvalidAuthorizationConfiguration("Scheme-selected commands require a scope-aware context values builder.");
        }

        return scoped.Build(command, services);
    }

    CorrelationId GetCorrelationId()
    {
        var correlationId = correlationIdAccessor.Current;
        if (correlationId == CorrelationId.NotSet)
        {
            correlationId = CorrelationId.New();
        }

        return correlationId;
    }

    /// <summary>
    /// Filters validation results based on the allowed severity level.
    /// </summary>
    /// <param name="result">The command result to filter. This method modifies the ValidationResults property.</param>
    /// <param name="context">The command context with the effective severity threshold.</param>
    /// <returns>The modified command result.</returns>
    /// <remarks>
    /// Without a policy or caller threshold, only errors block execution. An explicit threshold keeps results
    /// above it. Commands with a declared policy also keep Unknown results and cannot be loosened by the caller.
    /// </remarks>
    CommandResult FilterValidationResults(CommandResult result, CommandContext context)
    {
        result.ValidationResults = [.. CommandValidationResults.Blocking(result.ValidationResults, context.AllowedSeverity, context.BlockUnknownValidationSeverity)];
        return result;
    }

    CommandResult CreateCommandResultWithResponse(CorrelationId correlationId, object response)
    {
        if (CommandResultFactories.TryCreate(correlationId, response, out var result))
        {
            return result;
        }

        // Responses without a generated factory, such as a subtype of the declared response type, are wrapped through
        // reflection. That works under the JIT whatever the dynamic code settings say (an app built with PublishAot runs
        // under the JIT from dotnet run or a test host); only ahead-of-time compiled code cannot create the type.
        return CommandResultFactories.CreateWithoutFactory(correlationId, response, CommandResultFactories.CreateThroughReflection);
    }

    sealed class InvocationScopes(ExecutionRun run) : IDisposable
    {
        public void Dispose()
        {
            if (run.Context is { } current && run.Scopes is { } begun)
            {
                End(current, begun, run.BegunScopeCount - 1);
            }
        }

        static void End(CommandContext current, ICommandExecutionScope[] begun, int index)
        {
            if (index < 0)
            {
                return;
            }
            try
            {
                (begun[index] as ICommandInvocationExecutionScope)?.End(current);
            }
            finally
            {
                // Even a faulty cleanup cannot leave another scope's invocation resources alive.
                End(current, begun, index - 1);
            }
        }
    }
}
