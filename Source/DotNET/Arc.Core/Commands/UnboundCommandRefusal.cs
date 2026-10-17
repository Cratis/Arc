// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Arc.Authorization;
using Cratis.Arc.Observability;
using Cratis.Execution;
using Cratis.Traces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Commands;

/// <summary>
/// Reports a body-binding failure only after resource-free authorization, without creating a command context.
/// </summary>
/// <param name="activitySource">The <see cref="IActivitySource{T}"/> used for tracing.</param>
/// <param name="correlationId">Supplies the correlation identifier reported on results created here.</param>
/// <param name="prepare">Prepares authorization for a command type.</param>
internal class UnboundCommandRefusal(
    IActivitySource<CommandPipeline> activitySource,
    Func<CorrelationId> correlationId,
    Func<Type, IServiceProvider, Activity?, CancellationToken, Task<(PreparedAuthorization? Authorization, CommandResult? Failure)>> prepare)
{
    /// <summary>
    /// Refuses the unbound command, traced and measured like any other command turned away.
    /// </summary>
    /// <param name="request">The <see cref="UnboundCommandRefusalRequest"/> to refuse.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The authorization refusal or the original binding failure.</returns>
    internal async Task<CommandResult> Refuse(UnboundCommandRefusalRequest request, CancellationToken cancellationToken)
    {
        var typeName = CommandPipeline.TypeNameOf(request.CommandType);
        using var span = request.ValidateOnly ? activitySource.Validate(typeName) : activitySource.Execute(typeName);
        OperationActivity.AddCommandType(span.Activity, request.CommandType);
        var started = Stopwatch.GetTimestamp();
        try
        {
            var result = await Evaluate(request, span.Activity, cancellationToken);
            RecordOutcome(request, span.Activity, result, started, cancellationToken);
            return result;
        }
        catch (Exception exception)
        {
            RecordFailure(request, span.Activity, exception, started, cancellationToken);
            throw;
        }
    }

    static void RecordOutcome(UnboundCommandRefusalRequest request, Activity? activity, CommandResult result, long started, CancellationToken cancellationToken)
    {
        if (request.ValidateOnly)
        {
            CommandPipeline.RecordValidation(activity, result, cancellationToken);
            return;
        }

        CommandPipeline.RecordCommand(activity, request.RequestServices, request.CommandType, result, started, cancellationToken);
    }

    static void RecordFailure(UnboundCommandRefusalRequest request, Activity? activity, Exception exception, long started, CancellationToken cancellationToken)
    {
        if (request.ValidateOnly)
        {
            CommandPipeline.RecordValidationFailure(activity, exception, cancellationToken);
            return;
        }

        CommandPipeline.RecordCommandFailure(activity, request.RequestServices, request.CommandType, exception, started, cancellationToken);
    }

    static T Required<T>(IServiceProvider services, string name)
        where T : class =>
        services.GetService<T>() ?? throw new InvalidAuthorizationConfiguration($"Authorization {name} is unavailable.");

    async Task<CommandResult> Evaluate(UnboundCommandRefusalRequest request, Activity? activity, CancellationToken cancellationToken)
    {
        var services = request.RequestServices;
        using var decisionPolicy = CommandDecisionPolicy.Begin(request.CommandType, services);
        if (RefuseUnsupportedDecisions(services, activity) is { } unsupported)
        {
            return unsupported;
        }

        using var receipt = OperationContextScope.BeginIfNotSet(services);
        var declaration = CommandPipeline.GetHostedDeclaration(request.CommandType, services, activity);
        if (declaration is null)
        {
            return CommandResult.AuthorizationConfigurationFailure(correlationId());
        }

        return await Authorize(request, declaration, activity, cancellationToken);
    }

    CommandResult? RefuseUnsupportedDecisions(IServiceProvider services, Activity? activity)
    {
        try
        {
            CommandPipeline.EnsureDecisionSupport(services);
            return null;
        }
        catch (Exception exception)
        {
            OperationActivity.RecordException(activity, exception);
            return CommandResult.FromException(correlationId(), exception);
        }
    }

    async Task<CommandResult> Authorize(UnboundCommandRefusalRequest request, AuthorizationDeclaration declaration, Activity? activity, CancellationToken cancellationToken)
    {
        try
        {
            return await Verdict(request, declaration, activity, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (InvalidAuthorizationConfiguration exception)
        {
            OperationActivity.RecordException(activity, exception);
            request.RequestServices.GetService<ILogger<CommandPipeline>>()?.AuthorizationConfigurationFailed(exception);
            return CommandResult.AuthorizationConfigurationFailure(correlationId());
        }
        catch (Exception exception)
        {
            OperationActivity.RecordException(activity, exception);
            return CommandResult.FromException(correlationId(), exception);
        }
    }

    async Task<CommandResult> Verdict(UnboundCommandRefusalRequest request, AuthorizationDeclaration declaration, Activity? activity, CancellationToken cancellationToken)
    {
        var services = request.RequestServices;
        var evaluation = Required<AuthorizationEvaluation>(services, "evaluation");
        var evaluator = Required<IAuthorizationEvaluator>(services, "evaluator");
        var (prepared, failure) = await PrepareWhenRequired(request, declaration, activity, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        var check = new TypeAuthorizationCheck(request.CommandType, services, evaluator, prepared);
        if (!await evaluation.IsAuthorizedForType(check, cancellationToken))
        {
            return CommandResult.Unauthorized(correlationId());
        }

        return new CommandResult
        {
            CorrelationId = correlationId(),
            ValidationResults = request.BindingFailure.ValidationResults
        };
    }

    async Task<(PreparedAuthorization? Authorization, CommandResult? Failure)> PrepareWhenRequired(UnboundCommandRefusalRequest request, AuthorizationDeclaration declaration, Activity? activity, CancellationToken cancellationToken)
    {
        if (!declaration.RequiresAsynchronousEvaluation)
        {
            return (null, null);
        }

        return await prepare(request.CommandType, request.RequestServices, activity, cancellationToken);
    }
}
