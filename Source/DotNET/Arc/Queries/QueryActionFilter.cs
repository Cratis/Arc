// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.AspNetCore.Http;
using Cratis.Arc.Authorization;
using Cratis.Arc.Http;
using Cratis.Arc.Slices;
using Cratis.Arc.Tenancy;
using Cratis.Arc.Validation;
using Cratis.Strings;
using Cratis.Traces;
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
/// <param name="readModelInterceptors"><see cref="IReadModelInterceptors"/> for intercepting read models.</param>
/// <param name="controllerAdapter"><see cref="ControllerObservableQueryAdapter"/>.</param>
/// <param name="logger"><see cref="ILogger"/> for logging.</param>
/// <param name="activitySource">The <see cref="IActivitySource{T}"/> for tracing.</param>
public class QueryActionFilter(
    IQueryContextManager queryContextManager,
    IQueryRenderers queryProviders,
    IReadModelInterceptors readModelInterceptors,
    ControllerObservableQueryAdapter controllerAdapter,
    ILogger<QueryActionFilter> logger,
    IActivitySource<QueryActionFilter> activitySource) : IAsyncActionFilter
{
    /// <inheritdoc/>
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (context.HttpContext.Request.Method == HttpMethod.Get.Method &&
            context.ActionDescriptor is ControllerActionDescriptor controllerActionDescriptor)
        {
            await ExecuteControllerQuery(context, next, controllerActionDescriptor);
        }
        else
        {
            await next();
        }
    }

    static bool AdmitTenant(IServiceProvider services)
    {
        try
        {
            if ((services.GetService(typeof(IEnumerable<ITenantMembership>)) as IEnumerable<ITenantMembership>)?.Any() == true)
            {
                _ = services.GetRequiredService<TenantIdAccessor>().Current;
            }
            return true;
        }
        catch
        {
            return false;
        }
    }

    static void DenyQuery(ActionExecutingContext context, ActionExecutedContext? executed, QueryContext queryContext)
    {
        var denied = QueryResult.Unauthorized(queryContext.CorrelationId);
        context.HttpContext.Response.SetResponseStatusCode(denied);
        SetMvcResult(context, executed, new ObjectResult(denied));
    }

    static (List<string> Messages, string StackTrace) CollectExceptionDetails(ActionExecutedContext result)
    {
        var messages = new List<string>();
        var exception = result.Exception;
        if (exception is null)
        {
            return (messages, string.Empty);
        }

        var stackTrace = exception.StackTrace ?? string.Empty;
        do
        {
            messages.Add(exception.Message);
            exception = exception.InnerException;
        }
        while (exception is not null);

        result.Exception = null;
        return (messages, stackTrace);
    }

    static bool GetIgnoreWarningsFromRequest(ActionExecutingContext context)
    {
        if (context.HttpContext.Request.Headers.TryGetValue("X-Ignore-Warnings", out var value))
        {
            return bool.TryParse(value, out var ignoreWarnings) && ignoreWarnings;
        }
        return false;
    }

    static void SetMvcResult(ActionExecutingContext context, ActionExecutedContext? executed, IActionResult result)
    {
        if (executed is not null)
        {
            executed.Result = result;
            return;
        }

        context.Result = result;
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

    static Type GetReadModelType(object? response)
    {
        if (response is null)
        {
            return typeof(object);
        }

        var responseType = response.GetType();

        var enumerableInterface = responseType
            .GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));

        if (enumerableInterface is not null)
        {
            return enumerableInterface.GetGenericArguments()[0];
        }

        var queryableInterface = responseType
            .GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IQueryable<>));

        if (queryableInterface is not null)
        {
            return queryableInterface.GetGenericArguments()[0];
        }

        return responseType;
    }

    async Task ExecuteControllerQuery(ActionExecutingContext context, ActionExecutionDelegate next, ControllerActionDescriptor controllerActionDescriptor)
    {
        context.HttpContext.HandleWebSocketHeadersForMultipleProxies();
        if (new AspNetCoreHttpRequestContext(context.HttpContext).RejectsUntrustedWebSocketOrigin())
        {
            context.Result = new StatusCodeResult(StatusCodes.Status403Forbidden);
            return;
        }

        using var receipt = OperationContextScope.BeginIfNotSet(context.HttpContext.RequestServices);
        var queryContext = EstablishQueryContext(context.HttpContext, context.ActionDescriptor.DisplayName ?? "[NotSet]", controllerActionDescriptor.ControllerTypeInfo, queryContextManager);
        var warnings = new WarningHandling(context.ShouldTreatWarningsAsErrors(), GetIgnoreWarningsFromRequest(context));
        var routeTemplate = context.ActionDescriptor.AttributeRouteInfo?.Template ?? context.ActionDescriptor.DisplayName ?? string.Empty;
        using var span = activitySource.OnQuery(routeTemplate);
        span.Activity?.SetTag(WellKnownTelemetryNames.HttpRoute, routeTemplate);

        var callResult = await CallNextAndHandleValidationAndExceptions(context, next);
        await WriteResult(context, controllerActionDescriptor, queryContext, callResult, warnings);
    }

    async Task WriteResult(ActionExecutingContext context, ControllerActionDescriptor controllerActionDescriptor, QueryContext queryContext, QueryCall callResult, WarningHandling warnings)
    {
        if (callResult.MembershipDenied)
        {
            DenyQuery(context, callResult.Result, queryContext);
            return;
        }
        if (context.IsAspNetResult()) return;

        if (callResult.Result?.Result is ObjectResult objectResult && objectResult.IsStreamingResult())
        {
            await HandleStreaming(context, callResult.Result, objectResult, queryContext);
            return;
        }

        logger.NonClientObservableReturnValue(controllerActionDescriptor.ControllerName, controllerActionDescriptor.ActionName);
        var validationResults = context.ModelState.SelectMany(_ => _.Value!.Errors.Select(p => p.ToValidationResult(_.Key.ToCamelCase()))).ToList();
        var filteredValidationResults = FilterValidationResults(validationResults, warnings.TreatWarningsAsErrors, warnings.IgnoreWarnings);
        var queryResult = await CreateQueryResult(callResult, queryContext, filteredValidationResults, context.HttpContext.RequestServices);

        ExceptionDetailRedactor.Redact(queryResult, ExceptionDetailRedactor.ExposeFor(context.HttpContext.RequestServices), logger);

        if (await ControllerQueryValidationRejection.TryWrite(context.HttpContext, queryResult))
        {
            SetMvcResult(context, callResult.Result, new EmptyResult());
            return;
        }

        context.HttpContext.Response.SetResponseStatusCode(queryResult);

        SetMvcResult(context, callResult.Result, new ObjectResult(queryResult));
    }

    async Task HandleStreaming(ActionExecutingContext context, ActionExecutedContext executed, ObjectResult objectResult, QueryContext queryContext)
    {
        if (!AdmitTenant(context.HttpContext.RequestServices))
        {
            DenyQuery(context, executed, queryContext);
            return;
        }

        // Handle streaming results - both WebSocket and HTTP JSON streaming
        await controllerAdapter.HandleControllerStreamingResult(context, executed, objectResult);
    }

    QueryContext EstablishQueryContext(HttpContext httpContext, FullyQualifiedQueryName queryName, Type? controllerType, IQueryContextManager queryContextManager)
    {
        var sorting = httpContext.GetSortingInfo();
        var paging = httpContext.GetPagingInfo();
        var correlationId = httpContext.GetCorrelationId();

        var queryContext = new QueryContext(queryName, correlationId, paging, sorting)
        {
            ReceivedAt = OperationContextScope.Current ?? default,
            SliceLocation = controllerType is null ? SliceLocation.NotSet : SliceLocation.For(controllerType)
        };
        queryContextManager.Set(queryContext);
        return queryContext;
    }

    async Task<QueryResult> CreateQueryResult(
        QueryCall call,
        QueryContext queryContext,
        IEnumerable<ValidationResult> validationResults,
        IServiceProvider serviceProvider)
    {
        var response = call.Response;
        var rendererResult = response is not null ? queryProviders.Render(queryContext.Name, response, serviceProvider) : new QueryRendererResult(0, default!);

        var interceptedData = rendererResult.Data;
        if (rendererResult.Data is not null)
        {
            var readModelType = GetReadModelType(response);
            interceptedData = await ApplyInterceptors(readModelType, rendererResult.Data, serviceProvider);
        }

        var queryResult = new QueryResult
        {
            Paging = queryContext.Paging == Paging.NotPaged ? PagingInfo.NotPaged : new PagingInfo(
                queryContext.Paging.Page,
                queryContext.Paging.Size,
                rendererResult.TotalItems),
            CorrelationId = queryContext.CorrelationId,
            ValidationResults = validationResults,
            ExceptionMessages = call.ExceptionMessages,
            ExceptionStackTrace = call.ExceptionStackTrace ?? string.Empty,
            Data = interceptedData!
        };

        if (interceptedData is null && queryResult.IsSuccess)
        {
            queryResult.ExceptionMessages = ["Null data returned"];
        }

        return queryResult;
    }

    async Task<object> ApplyInterceptors(Type readModelType, object data, IServiceProvider serviceProvider)
    {
        if (data is IQueryable queryable)
        {
            return await readModelInterceptors.Intercept(readModelType, Enumerable.Cast<object>(queryable), serviceProvider);
        }

        if (data is IEnumerable<object> enumerable)
        {
            return await readModelInterceptors.Intercept(readModelType, enumerable, serviceProvider);
        }

        var intercepted = await readModelInterceptors.Intercept(readModelType, [data], serviceProvider);
        return intercepted.First();
    }

    async Task<QueryCall> CallNextAndHandleValidationAndExceptions(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!context.ModelState.IsValid && !context.ShouldIgnoreValidation())
        {
            return QueryCall.Empty(null);
        }

        ActionExecutedContext result;
        try
        {
            result = await next();
        }
        catch (Exception exception) when (UnauthorizedCause.IsCauseOf(exception))
        {
            return QueryCall.Denied(null);
        }

        if (UnauthorizedCause.IsCauseOf(result.Exception))
        {
            result.Exception = null;
            return QueryCall.Denied(result);
        }

        if (context.IsAspNetResult())
        {
            return QueryCall.Empty(null);
        }

        var (exceptionMessages, exceptionStackTrace) = CollectExceptionDetails(result);
        var response = (result.Result as ObjectResult)?.Value;
        return new QueryCall(result, exceptionMessages, exceptionStackTrace, response, false);
    }

    readonly record struct WarningHandling(bool TreatWarningsAsErrors, bool IgnoreWarnings);

    readonly record struct QueryCall(ActionExecutedContext? Result, IEnumerable<string> ExceptionMessages, string? ExceptionStackTrace, object? Response, bool MembershipDenied)
    {
        public static QueryCall Empty(ActionExecutedContext? result) => new(result, [], string.Empty, null, false);

        public static QueryCall Denied(ActionExecutedContext? result) => new(result, [], string.Empty, null, true);
    }
}
