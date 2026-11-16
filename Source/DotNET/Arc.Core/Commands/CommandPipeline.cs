// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using Cratis.Arc.Authorization;
using Cratis.Arc.DependencyInjection;
using Cratis.Arc.Identity.Approvals;
using Cratis.Arc.Observability;
using Cratis.Arc.Validation;
using Cratis.DependencyInjection;
using Cratis.Execution;
using Cratis.Traces;
using Cratis.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OneOf;

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
    public Task<CommandResult> Execute(object command, IServiceProvider serviceProvider, ValidationResultSeverity? allowedSeverity, CancellationToken cancellationToken) =>
        ExecuteTraced(new(command, serviceProvider, allowedSeverity, cancellationToken), hosted: false);

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
                IsRetryable = result.IsRetryable,
                IsMissingHandler = result.IsMissingHandler,
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
    public Task<CommandResult> ExecuteHosted(object command, IServiceProvider requestServices, ValidationResultSeverity? allowedSeverity, CancellationToken cancellationToken) =>
        ExecuteTraced(new(command, requestServices, allowedSeverity, cancellationToken), hosted: true);

    /// <inheritdoc/>
    public Task<CommandResult> Validate(object command, IServiceProvider serviceProvider, ValidationResultSeverity? allowedSeverity = default) =>
        Validate(command, serviceProvider, allowedSeverity, CancellationToken.None);

    /// <inheritdoc/>
    public Task<CommandResult> Validate(object command, IServiceProvider serviceProvider, ValidationResultSeverity? allowedSeverity, CancellationToken cancellationToken) =>
        ValidateTraced(new(command, serviceProvider, allowedSeverity, cancellationToken), hosted: false);

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
    /// <param name="measurement">What the command is measured with.</param>
    /// <param name="result">The result, or null when the command failed.</param>
    internal static void RecordCommand(in CommandMeasurement measurement, CommandResult? result)
    {
        var activity = measurement.Activity;
        var metrics = measurement.Services.GetService<PipelineMetrics>();
        var measured = metrics?.CommandsEnabled == true;
        if (activity is not { IsAllDataRequested: true } && !measured)
        {
            return;
        }

        var cancellationToken = measurement.CancellationToken;
        var outcome = result is null ? OperationOutcomes.ForException(cancellationToken) : OperationOutcomes.For(result, cancellationToken);
        if (result is not null)
        {
            OperationActivity.AddCorrelationId(activity, result.CorrelationId);
        }

        OperationActivity.RecordOutcome(activity, WellKnownTelemetryNames.CommandOutcome, outcome, result?.ValidationResults ?? []);
        if (measured)
        {
            metrics!.RecordCommand(TypeNameOf(measurement.CommandType), outcome, Stopwatch.GetElapsedTime(measurement.Started));
        }
    }

    /// <summary>
    /// Records a command failure on its span and metrics.
    /// </summary>
    /// <param name="measurement">What the command is measured with.</param>
    /// <param name="exception">The failure.</param>
    internal static void RecordCommandFailure(in CommandMeasurement measurement, Exception exception)
    {
        OperationActivity.RecordException(measurement.Activity, exception);
        RecordCommand(measurement, null);
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
    internal Task<CommandResult> ValidateHosted(object command, IServiceProvider requestServices, ValidationResultSeverity? allowedSeverity, CancellationToken cancellationToken) =>
        ValidateTraced(new(command, requestServices, allowedSeverity, cancellationToken), hosted: true);

    /// <summary>
    /// Reports a body-binding failure only after resource-free authorization, without creating a command context.
    /// </summary>
    /// <param name="request">The <see cref="UnboundCommandRefusalRequest"/> describing the unbound command.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The authorization refusal or the original binding failure.</returns>
    internal Task<CommandResult> RefuseUnboundHosted(UnboundCommandRefusalRequest request, CancellationToken cancellationToken) =>
        new UnboundCommandRefusal(activitySource, GetCorrelationId, PrepareHostedType).Refuse(request, cancellationToken);

    static async ValueTask<PreparedAuthorization?> PrepareAuthorization(Type commandType, IServiceProvider services, PreparedAuthorization? supplied, CancellationToken cancellationToken)
    {
        var declarations = services.GetService<AuthorizationDeclarations>() ??
            throw new InvalidAuthorizationConfiguration("Authorization declarations are unavailable.");
        if (supplied is not null || !declarations.For(commandType).RequiresAsynchronousEvaluation)
        {
            return supplied;
        }

        return await (services.GetService<AuthorizationEvaluation>() ??
            throw new InvalidAuthorizationConfiguration("Authorization evaluation is unavailable."))
            .Prepare(commandType, services, cancellationToken);
    }

    static bool AttachAuthorizedPrincipal(PreparedAuthorization? prepared, AuthorizationPrincipalLease lease, IServiceProvider services)
    {
        if (prepared is null)
        {
            return true;
        }

        if (!AuthorizationEvaluator.CheckRoles(prepared.Declaration, prepared.SelectedPrincipal, prepared.EvaluatesAnonymous))
        {
            return false;
        }

        if (prepared.PrincipalChanged)
        {
            lease.Attach(services.GetRequiredService<AuthorizationPrincipalScope>()
                .Begin(prepared.SelectedPrincipal!, services));
        }

        return true;
    }

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

    async Task<CommandResult> ExecuteTraced(CommandInvocation invocation, bool hosted)
    {
        // A hosted span starts before authorization is prepared, so a request turned away on the way in is traced and
        // measured like one turned away inside the pipeline.
        using var receipt = hosted ? null : OperationContextScope.BeginPipeline(invocation.Services);
        using var span = activitySource.Execute(TypeNameOf(invocation.CommandType));
        OperationActivity.AddCommandType(span.Activity, invocation.CommandType);
        var measurement = new CommandMeasurement(span.Activity, invocation.Services, invocation.CommandType, Stopwatch.GetTimestamp(), invocation.CancellationToken);
        try
        {
            var result = await Run(invocation with { Activity = span.Activity }, hosted, validate: false);
            RecordCommand(measurement, result);
            return result;
        }
        catch (Exception ex)
        {
            RecordCommandFailure(measurement, ex);
            throw;
        }
    }

    async Task<CommandResult> ValidateTraced(CommandInvocation invocation, bool hosted)
    {
        using var receipt = hosted ? null : OperationContextScope.BeginPipeline(invocation.Services);
        using var span = activitySource.Validate(TypeNameOf(invocation.CommandType));
        OperationActivity.AddCommandType(span.Activity, invocation.CommandType);
        try
        {
            var result = await Run(invocation with { Activity = span.Activity }, hosted, validate: true);
            RecordValidation(span.Activity, result, invocation.CancellationToken);
            return result;
        }
        catch (Exception ex)
        {
            RecordValidationFailure(span.Activity, ex, invocation.CancellationToken);
            throw;
        }
    }

    Task<CommandResult> Run(CommandInvocation invocation, bool hosted, bool validate) =>
        hosted ? RunHosted(invocation, validate) : RunCore(invocation, null, validate);

    Task<CommandResult> RunCore(CommandInvocation invocation, PreparedAuthorization? prepared, bool validate) =>
        validate ? ValidateCore(invocation, prepared) : ExecuteCore(invocation, prepared);

    async Task<CommandResult> RunHosted(CommandInvocation invocation, bool validate)
    {
        var services = invocation.Services;
        using var decisionPolicy = CommandDecisionPolicy.Begin(invocation.CommandType, services);
        if (UnsupportedDecisions(invocation) is { } unsupported)
        {
            return unsupported;
        }

        using var receipt = OperationContextScope.BeginIfNotSet(services);
        var declaration = GetHostedDeclaration(invocation.CommandType, services, invocation.Activity);
        if (declaration is null)
        {
            return CommandResult.AuthorizationConfigurationFailure(GetCorrelationId());
        }

        if (!declaration.RequiresAsynchronousEvaluation)
        {
            return await RunCore(invocation, null, validate);
        }

        var (prepared, failure) = await PrepareHostedType(invocation.CommandType, services, invocation.Activity, invocation.CancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        if (!prepared!.PrincipalChanged)
        {
            return await RunCore(invocation, prepared, validate);
        }

        using var scope = scopeFactory.CreateScope();
        using var ownership = AuthorizationExecutionScopes.Begin(scope.ServiceProvider);
        return await RunCore(invocation with { Services = scope.ServiceProvider }, prepared, validate);
    }

    CommandResult? UnsupportedDecisions(CommandInvocation invocation)
    {
        try
        {
            EnsureDecisionSupport(invocation.Services);
            return null;
        }
        catch (Exception exception)
        {
            OperationActivity.RecordException(invocation.Activity, exception);
            return CommandResult.FromException(GetCorrelationId(), exception);
        }
    }

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

    Task<CommandResult> ExecuteCore(CommandInvocation invocation, PreparedAuthorization? suppliedAuthorization) =>
        new Execution(this, invocation, suppliedAuthorization).Run();

    ICommandHandler? FindHandler(object command)
    {
        handlerProviders.TryGetHandlerFor(command, out var handler);
        return handler;
    }

    CommandContext BeginContext(CommandInvocation invocation, CorrelationId correlationId, PreparedAuthorization? prepared)
    {
        var context = CreateContext(invocation, correlationId, prepared);
        contextModifier.SetCurrent(context);
        return context;
    }

    ICommandExecutionScope[] ResolveExecutionScopes(IServiceProvider services) => [.. DiscoveredInstances.ResolvedFrom(services, executionScopes)];

    CommandOperationFrame EnterOperationBoundary(Type commandType) => CommandOperationBoundary.Enter(commandType, scopeFactory);

    Task<CommandResult> RunFilters(CommandContext context) => commandFilters.OnExecution(context);

    ValueTask<CommandHandlerArgumentResolution> ResolveArguments(ICommandHandler handler, CommandContext context, IServiceProvider services) =>
        argumentResolver.Resolve(handler, context, services, context.AllowedSeverity);

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

    async Task<CommandResult> ValidateCore(CommandInvocation invocation, PreparedAuthorization? suppliedAuthorization)
    {
        var (command, serviceProvider, activity) = (invocation.Command, invocation.Services, invocation.Activity);
        using var decisionPolicy = CommandDecisionPolicy.Begin(invocation.CommandType, serviceProvider);
        using var validation = CommandValidationExecution.Begin(invocation.CommandType);
        var correlationId = GetCorrelationId();
        var result = CommandResult.Success(correlationId);
        using var principalLease = new AuthorizationPrincipalLease();
        try
        {
            EnsureDecisionSupport(serviceProvider);
            if (FindHandler(command) is null)
            {
                return CommandResult.MissingHandler(correlationId, invocation.CommandType);
            }

            var preparedAuthorization = await PrepareAuthorization(invocation.CommandType, serviceProvider, suppliedAuthorization, invocation.CancellationToken);
            if (!AttachAuthorizedPrincipal(preparedAuthorization, principalLease, serviceProvider))
            {
                return CommandResult.Unauthorized(correlationId);
            }

            using var identityGuard = EnterCallerIdentity(serviceProvider, preparedAuthorization, true);
            AuthorizationExecutionScopes.MarkWorkStarted(serviceProvider);
            OperationActivity.AddResolvedTenant(activity, serviceProvider);
            var commandContext = BeginContext(invocation, correlationId, preparedAuthorization);

            // Run only filters (authorization and validation), skip handler execution and argument resolution
            using var snapshot = OperationSnapshot.Begin(serviceProvider, () => commandContext.AuthorizedPrincipalSnapshot);
            result = await RunFilters(commandContext);
            if (snapshot.MembershipDenied)
            {
                return CommandResult.Unauthorized(correlationId);
            }

            result = FilterValidationResults(result, commandContext);
            if (result.IsSuccess && ApprovalGate.ForValidation(commandContext, serviceProvider) is { } refusal)
            {
                result = refusal;
            }
        }
        catch (InvalidAuthorizationConfiguration ex)
        {
            OperationActivity.RecordException(activity, ex);
            serviceProvider.GetService<ILogger<CommandPipeline>>()?.AuthorizationConfigurationFailed(ex);
            result.MergeWith(CommandResult.AuthorizationConfigurationFailure(correlationId));
        }
        catch (Exception ex)
        {
            OperationActivity.RecordException(activity, ex);
            result.MergeWith(CommandResult.FromException(correlationId, ex));
        }

        return result;
    }

    CommandContext CreateContext(CommandInvocation invocation, CorrelationId correlationId, PreparedAuthorization? prepared)
    {
        var command = invocation.Command;
        var severityPolicy = CommandValidationResults.ForCommand(command.GetType(), invocation.AllowedSeverity);
        return new(
            correlationId,
            command.GetType(),
            command,
            [],
            BuildContextValues(command, invocation.Services, prepared),
            severityPolicy.AllowedSeverity,
            ServiceProvider: invocation.Services,
            CancellationToken: invocation.CancellationToken)
        {
            PreparedAuthorization = prepared,
            BlockUnknownValidationSeverity = severityPolicy.BlockUnknown,
            ReceivedAt = OperationContextScope.Current ?? default
        };
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

    async Task<(CommandContext CommandContext, CommandResult Result)> ProcessOperationResponse(
        object[] values,
        CommandContext commandContext,
        CorrelationId correlationId,
        CommandResult result)
    {
        var ordinary = values.Where(value => value is not ICommandOperation and not CommandOperations).ToArray();
        foreach (var value in ordinary)
        {
            valueHandlers.UpdateContext(commandContext, value);
        }

        var (response, withResponseContext) = IdentifyOperationResponse(ordinary, commandContext);
        commandContext = withResponseContext;
        if (response is not null)
        {
            var withResponse = CreateCommandResultWithResponse(correlationId, response);
            withResponse.MergeWith(result);
            result = withResponse;
        }

        // Control values always precede events/other handlers, regardless of tuple order. Operations are exclusively
        // owned here, so even a broad custom response handler never sees or executes an operation declaration.
        foreach (var value in ordinary.Where(IsOperationControl))
        {
            result.MergeWith(value is CommandResult control ? control : await valueHandlers.Handle(commandContext, value));
        }

        result = FilterValidationResults(result, commandContext);
        if (result.IsSuccess)
        {
            await HandleOrdinaryValues(ordinary, response, commandContext, result);
        }

        return (commandContext, result);
    }

    async Task HandleOrdinaryValues(object[] ordinary, object? response, CommandContext commandContext, CommandResult result)
    {
        foreach (var value in ordinary.Where(value => !IsOperationControl(value) && !ReferenceEquals(value, response)))
        {
            result.MergeWith(await valueHandlers.Handle(commandContext, value));
            if (!result.IsSuccess)
            {
                return;
            }
        }
    }

    (object? Response, CommandContext CommandContext) IdentifyOperationResponse(object[] ordinary, CommandContext commandContext)
    {
        object? response = null;
        foreach (var value in ordinary.Where(value => !IsOperationControl(value)))
        {
            if (valueHandlers.CanHandle(commandContext, value))
            {
                continue;
            }

            if (response is not null)
            {
                throw new MultipleUnhandledTupleValues([response, value]);
            }

            response = value;
            commandContext = commandContext with { Response = value };
        }

        return (response, commandContext);
    }

    bool IsOperationControl(object value) => value is CommandResult or ValidationResult or AuthorizationResult or IEnumerable<ValidationResult>;

    object UnwrapValue(object value)
    {
        return value switch
        {
            IOneOf oneOf => UnwrapValue(oneOf.Value),
            _ => value
        };
    }

    bool CanHandleValue(object value, CommandContext commandContext)
    {
        return value switch
        {
            IOneOf oneOf => CanHandleOneOfValue(oneOf, commandContext),
            ITuple tuple => CanHandleTuple(tuple, commandContext),
            _ => valueHandlers.CanHandle(commandContext, value)
        };
    }

    bool CanHandleOneOfValue(IOneOf oneOf, CommandContext commandContext)
    {
        if (valueHandlers.CanHandle(commandContext, oneOf))
        {
            return true;
        }

        return CanHandleValue(oneOf.Value, commandContext);
    }

    bool CanHandleTuple(ITuple tuple, CommandContext commandContext)
    {
        for (var i = 0; i < tuple.Length; i++)
        {
            var element = tuple[i];
            if (element is null)
            {
                continue;
            }

            if (!CanHandleValue(element, commandContext))
            {
                return false;
            }
        }

        return true;
    }

    async Task<(CommandContext CommandContext, CommandResult Result)> ProcessResponseValue(
        object response,
        CommandContext commandContext,
        CorrelationId correlationId,
        CommandResult result)
    {
        return response switch
        {
            ITuple tuple => await ProcessTupleResponse(tuple, commandContext, correlationId, result),
            IOneOf oneOf => await ProcessOneOfResponse(oneOf, commandContext, correlationId, result),
            _ => await ProcessSimpleResponse(response, commandContext, correlationId, result)
        };
    }

    async Task<(CommandContext CommandContext, CommandResult Result)> ProcessTupleResponse(
        ITuple tuple,
        CommandContext commandContext,
        CorrelationId correlationId,
        CommandResult result)
    {
        var values = ExtractValuesFromTupleInOrder(tuple).ToList();

        foreach (var value in values)
        {
            valueHandlers.UpdateContext(commandContext, value);
        }

        // First pass: identify and set the response value
        // Handlers may check commandContext.Response, so we need to set it before checking possibility to handle
        var valuesToProcess = new List<object>();
        (commandContext, result) = IdentifyTupleResponse(values, commandContext, correlationId, result);

        // Second pass: now that response is set, check which values can be handled
        // Some handlers may depend on commandContext.Response being set
        foreach (var value in values)
        {
            if (CanHandleValue(value, commandContext))
            {
                valuesToProcess.Add(value);
            }
        }

        // Third pass: handle all values that can be handled
        foreach (var value in valuesToProcess)
        {
            result.MergeWith(await HandleValue(value, commandContext));
        }

        return (commandContext, result);
    }

    (CommandContext CommandContext, CommandResult Result) IdentifyTupleResponse(List<object> values, CommandContext commandContext, CorrelationId correlationId, CommandResult result)
    {
        object? responseValue = null;
        foreach (var value in values)
        {
            // Check if this value should be the response (non-handled value)
            // We need to check this WITHOUT the response being set yet
            if (CanHandleValue(value, commandContext))
            {
                continue;
            }

            var unwrappedValue = UnwrapValue(value);
            if (responseValue is not null)
            {
                // Multiple unhandled values - can't determine which should be the response
                throw new MultipleUnhandledTupleValues([responseValue, unwrappedValue]);
            }

            responseValue = unwrappedValue;
            commandContext = commandContext with { Response = unwrappedValue };
            result = CreateCommandResultWithResponse(correlationId, unwrappedValue);
        }

        return (commandContext, result);
    }

    IEnumerable<object> ExtractValuesFromTupleInOrder(ITuple tuple)
    {
        for (var i = 0; i < tuple.Length; i++)
        {
            if (tuple[i] is not null)
            {
                yield return tuple[i]!;
            }
        }
    }

    async Task<(CommandContext CommandContext, CommandResult Result)> ProcessOneOfResponse(
        IOneOf oneOf,
        CommandContext commandContext,
        CorrelationId correlationId,
        CommandResult result)
    {
        var innerValue = oneOf.Value;
        return await ProcessResponseValue(innerValue, commandContext, correlationId, result);
    }

    async Task<(CommandContext CommandContext, CommandResult Result)> ProcessSimpleResponse(
        object response,
        CommandContext commandContext,
        CorrelationId correlationId,
        CommandResult result)
    {
        if (valueHandlers.CanHandle(commandContext, response))
        {
            result.MergeWith(await valueHandlers.Handle(commandContext, response));
        }
        else
        {
            commandContext = commandContext with { Response = response };
            result = CreateCommandResultWithResponse(correlationId, response);
        }

        return (commandContext, result);
    }

    async Task<CommandResult> HandleValue(object value, CommandContext commandContext)
    {
        return value switch
        {
            ITuple tuple => await HandleTuple(tuple, commandContext),
            IOneOf oneOf => await HandleValue(oneOf.Value, commandContext),
            _ => await valueHandlers.Handle(commandContext, value)
        };
    }

    async Task<CommandResult> HandleTuple(ITuple tuple, CommandContext commandContext)
    {
        var result = CommandResult.Success(commandContext.CorrelationId);

        for (var i = 0; i < tuple.Length; i++)
        {
            var element = tuple[i];
            if (element is null)
            {
                continue;
            }

            result.MergeWith(await HandleValue(element, commandContext));
        }

        return result;
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
}
