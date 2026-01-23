// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using Cratis.Strings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Queries;

/// <summary>
/// Represents a <see cref="IAsyncActionFilter"/> for providing a proper <see cref="QueryResult"/> for post actions.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="QueryActionFilter"/> class.
/// </remarks>
/// <param name="queryContextManager"><see cref="IQueryContextManager"/>.</param>
/// <param name="queryProviders"><see cref="IQueryRenderers"/>.</param>
/// <param name="controllerAdapter"><see cref="ControllerObservableQueryAdapter"/>.</param>
/// <param name="logger"><see cref="ILogger"/> for logging.</param>
public class QueryActionFilter(
    IQueryContextManager queryContextManager,
    IQueryRenderers queryProviders,
    ControllerObservableQueryAdapter controllerAdapter,
    ILogger<QueryActionFilter> logger) : IAsyncActionFilter
{
    /// <inheritdoc/>
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (context.HttpContext.Request.Method == HttpMethod.Get.Method &&
            context.ActionDescriptor is ControllerActionDescriptor controllerActionDescriptor)
        {
            var queryContext = EstablishQueryContext(context.HttpContext, context.ActionDescriptor.DisplayName ?? "[NotSet]", queryContextManager);

            var callResult = await CallNextAndHandleValidationAndExceptions(context, next);
            if (context.IsAspNetResult()) return;

            if (callResult.Result?.Result is ObjectResult objectResult && objectResult.IsStreamingResult())
            {
                // Handle streaming results - both WebSocket and HTTP JSON streaming
                await controllerAdapter.HandleControllerStreamingResult(context, callResult.Result, objectResult);
                return; // Early return to avoid processing as regular query result
            }

            logger.NonClientObservableReturnValue(controllerActionDescriptor.ControllerName, controllerActionDescriptor.ActionName);
            var validationResults = context.ModelState.SelectMany(_ => _.Value!.Errors.Select(p => p.ToValidationResult(_.Key.ToCamelCase())));
            var queryResult = CreateQueryResult(
                callResult.Response,
                queryContext.Name,
                queryContext,
                callResult.ExceptionMessages,
                callResult.ExceptionStackTrace ?? string.Empty,
                validationResults,
                queryProviders,
                context.HttpContext.RequestServices);

            context.HttpContext.Response.SetResponseStatusCode(queryResult);

            var actualResult = new ObjectResult(queryResult);

            if (callResult.Result is not null)
            {
                callResult.Result.Result = actualResult;
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

    QueryContext EstablishQueryContext(HttpContext httpContext, FullyQualifiedQueryName queryName, IQueryContextManager queryContextManager)
    {
        var sorting = httpContext.GetSortingInfo();
        var paging = httpContext.GetPagingInfo();
        var correlationId = httpContext.GetCorrelationId();

        var queryContext = new QueryContext(queryName, correlationId, paging, sorting);
        queryContextManager.Set(queryContext);
        return queryContext;
    }

    QueryResult CreateQueryResult(
        object? response,
        FullyQualifiedQueryName queryName,
        QueryContext queryContext,
        IEnumerable<string> exceptionMessages,
        string exceptionStackTrace,
        IEnumerable<ValidationResult> validationResults,
        IQueryRenderers queryProviders,
        IServiceProvider serviceProvider)
    {
        var rendererResult = response is not null ? queryProviders.Render(queryName, response, serviceProvider) : new QueryRendererResult(0, default!);

        var queryResult = new QueryResult
        {
            Paging = queryContext.Paging == Paging.NotPaged ? PagingInfo.NotPaged : new PagingInfo(
                queryContext.Paging.Page,
                queryContext.Paging.Size,
                rendererResult.TotalItems),
            CorrelationId = queryContext.CorrelationId,
            ValidationResults = validationResults,
            ExceptionMessages = exceptionMessages,
            ExceptionStackTrace = exceptionStackTrace,
            Data = rendererResult.Data
        };

        if (rendererResult.Data is null && queryResult.IsSuccess)
        {
            queryResult.ExceptionMessages = ["Null data returned"];
        }

        return queryResult;
    }

    async Task<(ActionExecutedContext? Result, IEnumerable<string> ExceptionMessages, string? ExceptionStackTrace, object? Response)> CallNextAndHandleValidationAndExceptions(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var exceptionMessages = new List<string>();
        var exceptionStackTrace = string.Empty;
        object? response = null;
        ActionExecutedContext? result = null;

        if (context.ModelState.IsValid || context.ShouldIgnoreValidation())
        {
            result = await next();

            if (context.IsAspNetResult())
            {
                return (null, exceptionMessages, exceptionStackTrace, response);
            }

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

                result.Exception = null;
            }

            if (result.Result is ObjectResult objectResult)
            {
                response = objectResult.Value;
            }
        }

        return (result, exceptionMessages, exceptionStackTrace, response);
    }
}
