// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Cratis.Arc.Commands;

/// <summary>
/// Represents a <see cref="IAsyncActionFilter"/> for providing a proper <see cref="CommandResult{T}"/> for post actions.
/// </summary>
/// <param name="contextModifier">The <see cref="ICommandContextModifier"/> to use for setting the current command context.</param>
/// <param name="contextValuesBuilder">The <see cref="ICommandContextValuesBuilder"/> to use for building command context values.</param>
public class CommandActionFilter(
    ICommandContextModifier contextModifier,
    ICommandContextValuesBuilder contextValuesBuilder) : IAsyncActionFilter
{
    /// <inheritdoc/>
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (context.HttpContext.Request.Method == HttpMethod.Post.Method)
        {
            EstablishCommandContext(context);

            var exceptionMessages = new List<string>();
            var exceptionStackTrace = string.Empty;
            ActionExecutedContext? result = null;
            object? response = null;

            var ignoreValidation = context.ShouldIgnoreValidation();
            var isValidationRequest = IsValidationRequest(context);
            var treatWarningsAsErrors = context.ShouldTreatWarningsAsErrors();
            var ignoreWarnings = GetIgnoreWarningsFromRequest(context);

            var validationResult = ignoreValidation ?
                                        [] :
                                        context.ModelState.SelectMany(_ => _.Value!.Errors.Select(e => e.ToValidationResult(_.Key))).ToList();

            if ((context.ModelState.IsValid || ignoreValidation) && !isValidationRequest)
            {
                var errorsBefore = context.ModelState.SelectMany(_ => _.Value!.Errors).ToArray();

                result = await next();

                AddAdditionalValidationResultsAfterActionExecute(context, validationResult, errorsBefore);

                if (context.IsAspNetResult()) return;

                if (result.Exception is not null)
                {
                    var exception = result.Exception;
                    exceptionStackTrace = exception.StackTrace;

                    do
                    {
                        exceptionMessages.Add(exception.Message);
                        exception = exception.InnerException;
                    }
                    while (exception is not null);

                    result.Exception = null!;
                }

                if (result.Result is ObjectResult objectResult)
                {
                    response = objectResult.Value;
                }
            }

            var commandResult = new CommandResult<object>
            {
                CorrelationId = context.HttpContext.GetCorrelationId(),
                ValidationResults = FilterValidationResults(validationResult, treatWarningsAsErrors, ignoreWarnings),
                ExceptionMessages = [.. exceptionMessages],
                ExceptionStackTrace = exceptionStackTrace ?? string.Empty,
                Response = response
            };

            context.HttpContext.Response.SetResponseStatusCode(commandResult);

            var actualResult = new ObjectResult(commandResult);

            if (result is not null)
            {
                result.Result = actualResult;
            }
            else
            {
                context.Result = actualResult;
            }
        }
        else
        {
            await next();
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

    void EstablishCommandContext(ActionExecutingContext context)
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

        var values = contextValuesBuilder.Build(command);
        var commandContext = new CommandContext(
            context.HttpContext.GetCorrelationId(),
            commandType,
            command,
            [],
            values);

        contextModifier.SetCurrent(commandContext);
    }

    void AddAdditionalValidationResultsAfterActionExecute(ActionExecutingContext context, List<ValidationResult> validationResult, IEnumerable<ModelError> errorsBefore)
    {
        foreach (var (member, entry) in context.ModelState)
        {
            foreach (var error in entry.Errors.Where(_ => !errorsBefore.Contains(_)))
            {
                validationResult.Add(error.ToValidationResult(member));
            }
        }
    }
}
