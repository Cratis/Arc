// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.DependencyInjection;
using Cratis.Arc.Slices;
using Cratis.Arc.Tenancy;
using Cratis.Arc.Validation;
using Cratis.Traces;
using Cratis.Types;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Arc.Commands;

/// <summary>
/// Represents a <see cref="IAsyncActionFilter"/> for providing a proper <see cref="CommandResult{T}"/> for post actions.
/// </summary>
/// <param name="contextModifier">The <see cref="ICommandContextModifier"/> to use for setting the current command context.</param>
/// <param name="contextValuesBuilder">The <see cref="ICommandContextValuesBuilder"/> to use for building command context values.</param>
/// <param name="activitySource">The <see cref="IActivitySource{T}"/> for tracing.</param>
public class CommandActionFilter(
    ICommandContextModifier contextModifier,
    ICommandContextValuesBuilder contextValuesBuilder,
    IActivitySource<CommandActionFilter> activitySource) : IAsyncActionFilter
{
    /// <inheritdoc/>
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (context.HttpContext.Request.Method != HttpMethod.Post.Method)
        {
            await next();
            return;
        }

        using var receipt = OperationContextScope.BeginIfNotSet(context.HttpContext.RequestServices);
        var commandContext = EstablishCommandContext(context);
        var routeTemplate = context.ActionDescriptor.AttributeRouteInfo?.Template ?? context.ActionDescriptor.DisplayName ?? string.Empty;
        using var span = activitySource.OnCommand(routeTemplate);
        if (ShouldExecute(context) && context.IsAspNetResult())
        {
            await next();
            return;
        }

        await ExecuteCommand(context, next, commandContext);
    }

    static async Task ExecuteCommand(ActionExecutingContext context, ActionExecutionDelegate next, CommandContext commandContext)
    {
        // An MVC action is not a pipeline decision invocation.
        var shouldExecute = ShouldExecute(context);
        var scopes = ResolveScopes(context, shouldExecute);
        List<ICommandExecutionScope> begunScopes = [];
        ActionExecutedContext? result = null;
        Exception? actionException = null;
        var validationResult = context.ShouldIgnoreValidation()
            ? []
            : context.ModelState.SelectMany(_ => _.Value!.Errors.Select(e => e.ToValidationResult(_.Key))).ToList();
        try
        {
            if (shouldExecute)
            {
                var errorsBefore = context.ModelState.SelectMany(_ => _.Value!.Errors).ToArray();

                try
                {
                    foreach (var scope in scopes)
                    {
                        begunScopes.Add(scope);
                        scope.Begin(commandContext);
                    }
                    foreach (var scope in begunScopes.OfType<IAsyncCommandExecutionScope>()) await scope.BeginAsync(commandContext);
                    result = await next();
                }
                catch (Exception exception)
                {
                    actionException = exception;
                }

                AddAdditionalValidationResultsAfterActionExecute(context, validationResult, errorsBefore);
            }

            var commandResult = CreateResult(context, result, validationResult, actionException);
            await CompleteScopes(commandContext, [.. begunScopes], commandResult);

            var logger = context.HttpContext.RequestServices?.GetService<ILoggerFactory>()?.CreateLogger<CommandActionFilter>()
                ?? NullLogger<CommandActionFilter>.Instance;
            ExceptionDetailRedactor.Redact(commandResult, ExceptionDetailRedactor.ExposeFor(context.HttpContext.RequestServices), logger);
            PublishResult(context, result, commandResult);
        }
        finally
        {
            EndScopes(commandContext, [.. begunScopes], begunScopes.Count - 1);
        }
    }

    static void PublishResult(ActionExecutingContext context, ActionExecutedContext? executed, CommandResult<object> result)
    {
        // Completion can refuse the command after the action produced a response.
        if (!result.IsSuccess)
        {
            result.Response = null;
        }
        context.HttpContext.Response.SetResponseStatusCode(result);
        var actualResult = new ObjectResult(result);
        if (executed is not null)
        {
            executed.Result = actualResult;
        }
        else
        {
            context.Result = actualResult;
        }
    }

    static bool ShouldExecute(ActionExecutingContext context) =>
        (context.ModelState.IsValid || context.ShouldIgnoreValidation()) && !IsValidationRequest(context);

    static ICommandExecutionScope[] ResolveScopes(ActionExecutingContext context, bool shouldExecute)
    {
        if (!shouldExecute) return [];
        var services = context.HttpContext.RequestServices;
        var discovered = services?.GetService<IInstancesOf<ICommandExecutionScope>>();
        return discovered is null ? [] : [.. DiscoveredInstances.ResolvedFrom(services, discovered)];
    }

    static CommandResult<object> CreateResult(ActionExecutingContext context, ActionExecutedContext? executed, List<ValidationResult> validationResults, Exception? actionException)
    {
        if (TenantMembershipDenied.IsCauseOf(actionException ?? executed?.Exception))
        {
            if (executed is not null) executed.Exception = null;
            return new CommandResult<object>
            {
                CorrelationId = context.HttpContext.GetCorrelationId(),
                IsAuthorized = false
            };
        }

        var result = new CommandResult<object>
        {
            CorrelationId = context.HttpContext.GetCorrelationId(),
            ValidationResults = FilterValidationResults(validationResults, context.ShouldTreatWarningsAsErrors(), GetIgnoreWarningsFromRequest(context)),
            Response = (executed?.Result as ObjectResult)?.Value
        };
        if ((actionException ?? executed?.Exception) is { } exception)
        {
            var messages = new List<string>();
            result.ExceptionStackTrace = exception.StackTrace ?? string.Empty;
            do
            {
                messages.Add(exception.Message);
                exception = exception.InnerException;
            }
            while (exception is not null);
            result.ExceptionMessages = [.. messages];
            if (executed is not null) executed.Exception = null;
        }
        return result;
    }

    static async Task CompleteScopes(CommandContext context, ICommandExecutionScope[] scopes, CommandResult result)
    {
        foreach (var scope in scopes.AsEnumerable().Reverse())
        {
            try
            {
                await scope.Complete(context, result);
            }
            catch (Exception exception)
            {
                result.MergeWith(CommandResult.FromException(context.CorrelationId, exception));
            }
        }
    }

    static void EndScopes(CommandContext context, ICommandExecutionScope[] scopes, int index)
    {
        if (index < 0) return;
        try
        {
            if (scopes[index] is ICommandInvocationExecutionScope scope) scope.End(context);
        }
        finally
        {
            EndScopes(context, scopes, index - 1);
        }
    }

    static bool IsValidationRequest(ActionExecutingContext context)
        => context.HttpContext.Request.Path.Value?.EndsWith("/validate", StringComparison.OrdinalIgnoreCase) == true;

    static bool GetIgnoreWarningsFromRequest(ActionExecutingContext context)
    {
        if (context.HttpContext.Request.Headers.TryGetValue("X-Ignore-Warnings", out var value))
        {
            return bool.TryParse(value, out var ignoreWarnings) && ignoreWarnings;
        }
        return false;
    }

    static ValidationResult[] FilterValidationResults(List<ValidationResult> validationResults, bool treatWarningsAsErrors, bool ignoreWarnings)
    {
        if (ignoreWarnings)
        {
            return validationResults.Where(v => v.Severity == ValidationResultSeverity.Error).ToArray();
        }

        if (treatWarningsAsErrors)
        {
            return validationResults.Where(v => v.Severity >= ValidationResultSeverity.Warning).ToArray();
        }

        return validationResults.Where(v => v.Severity == ValidationResultSeverity.Error).ToArray();
    }

    static void AddAdditionalValidationResultsAfterActionExecute(ActionExecutingContext context, List<ValidationResult> validationResult, IEnumerable<ModelError> errorsBefore)
    {
        foreach (var (member, entry) in context.ModelState)
        {
            foreach (var error in entry.Errors.Where(_ => !errorsBefore.Contains(_)))
            {
                validationResult.Add(error.ToValidationResult(member));
            }
        }
    }

    CommandContext EstablishCommandContext(ActionExecutingContext context)
    {
        var bodyParam = context.ActionDescriptor.Parameters
            .FirstOrDefault(p => p.BindingInfo?.BindingSource == BindingSource.Body);

        object command;
        Type commandType;

        if (bodyParam is not null && context.ActionArguments.TryGetValue(bodyParam.Name, out var commandObj) && commandObj is not null)
        {
            command = commandObj;
            commandType = commandObj.GetType();
        }
        else
        {
            command = new object();
            commandType = typeof(object);
        }

        // The proxy generator places a controller's commands by the controller's namespace, so the slice location follows it.
        var sliceLocation = context.ActionDescriptor is ControllerActionDescriptor { ControllerTypeInfo: { } controllerType }
            ? SliceLocation.For(controllerType)
            : SliceLocation.For(commandType);

        var values = contextValuesBuilder.Build(command);
        var commandContext = new CommandContext(
            context.HttpContext.GetCorrelationId(),
            commandType,
            command,
            [],
            values,
            ServiceProvider: context.HttpContext.RequestServices,
            CancellationToken: context.HttpContext.RequestAborted)
        {
            ReceivedAt = OperationContextScope.Current ?? default,
            IsControllerAction = true,
            SliceLocation = sliceLocation
        };

        contextModifier.SetCurrent(commandContext);
        return commandContext;
    }
}
