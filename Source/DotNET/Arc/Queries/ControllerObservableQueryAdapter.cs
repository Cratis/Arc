// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.AspNetCore.Http;
using Cratis.DependencyInjection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Queries;

/// <summary>
/// Represents an ASP.NET Core controller-specific adapter for handling observable query results.
/// </summary>
/// <param name="queryContextManager"><see cref="IQueryContextManager"/>.</param>
/// <param name="options"><see cref="JsonOptions"/>.</param>
/// <param name="logger"><see cref="ILogger"/> for logging.</param>
[Singleton]
public class ControllerObservableQueryAdapter(
    IQueryContextManager queryContextManager,
    IOptions<JsonOptions> options,
    ILogger<ControllerObservableQueryAdapter> logger)
{
    readonly JsonOptions _options = options.Value;

    /// <summary>
    /// Determines if the current request should be handled as a WebSocket connection.
    /// </summary>
    /// <param name="context">The <see cref="ActionExecutingContext"/>.</param>
    /// <returns>True if the request should be handled as WebSocket, false otherwise.</returns>
    public bool ShouldHandleAsWebSocket(ActionExecutingContext context) =>
        context.HttpContext.WebSockets.IsWebSocketRequest;

    /// <summary>
    /// Determines if the current request should be handled as a WebSocket connection.
    /// </summary>
    /// <param name="httpContext">The <see cref="HttpContext"/>.</param>
    /// <returns>True if the request should be handled as WebSocket, false otherwise.</returns>
    public bool ShouldHandleAsWebSocket(HttpContext httpContext) =>
        httpContext.WebSockets.IsWebSocketRequest;

    /// <summary>
    /// Handles streaming result for controller-based actions.
    /// </summary>
    /// <param name="context">The <see cref="ActionExecutingContext"/>.</param>
    /// <param name="actionExecutedContext">The <see cref="ActionExecutedContext"/> from the action execution.</param>
    /// <param name="objectResult">The <see cref="ObjectResult"/> containing the streaming data.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task HandleControllerStreamingResult(
        ActionExecutingContext context,
        ActionExecutedContext? actionExecutedContext,
        ObjectResult objectResult)
    {
        if (context.ActionDescriptor is not ControllerActionDescriptor controllerActionDescriptor)
        {
            return;
        }

        context.HttpContext.HandleWebSocketHeadersForMultipleProxies(logger);

        if (objectResult.IsSubjectResult())
        {
            await HandleSubjectResult(context, actionExecutedContext, objectResult, controllerActionDescriptor);
        }
        else if (objectResult.IsAsyncEnumerableResult())
        {
            await HandleAsyncEnumerableResult(context, actionExecutedContext, objectResult, controllerActionDescriptor);
        }
    }

    async Task HandleSubjectResult(
        ActionExecutingContext context,
        ActionExecutedContext? callResult,
        ObjectResult objectResult,
        ControllerActionDescriptor controllerActionDescriptor)
    {
        logger.ClientObservableReturnValue(controllerActionDescriptor.ControllerName, controllerActionDescriptor.ActionName);
        var clientObservable = ObservableQueryExtensions.CreateClientObservableFrom(
            context.HttpContext.RequestServices,
            objectResult,
            queryContextManager,
            _options.JsonSerializerOptions);

        if (ShouldHandleAsWebSocket(context))
        {
            logger.RequestIsWebSocket();
            var httpRequestContext = new AspNetCoreHttpRequestContext(context.HttpContext);
            await clientObservable.HandleConnection(httpRequestContext);
            if (callResult?.Result is ObjectResult objResult)
            {
                objResult.Value = null;
            }
        }
        else
        {
            logger.RequestIsHttp();

            // For HTTP, extract the current snapshot from the BehaviorSubject
            var snapshot = ExtractSnapshotFromClientObservable(clientObservable);

            if (callResult?.Result is ObjectResult objResult)
            {
                objResult.Value = snapshot;
            }
            else if (callResult is not null)
            {
                callResult.Result = new ObjectResult(snapshot);
            }
        }
    }

    async Task HandleAsyncEnumerableResult(
        ActionExecutingContext context,
        ActionExecutedContext? callResult,
        ObjectResult objectResult,
        ControllerActionDescriptor controllerActionDescriptor)
    {
        logger.AsyncEnumerableReturnValue(controllerActionDescriptor.ControllerName, controllerActionDescriptor.ActionName);
        var clientEnumerableObservable = ObservableQueryExtensions.CreateClientEnumerableObservableFrom(
            context.HttpContext.RequestServices,
            objectResult,
            _options.JsonSerializerOptions);

        if (ShouldHandleAsWebSocket(context))
        {
            logger.RequestIsWebSocket();
            var httpRequestContext = new AspNetCoreHttpRequestContext(context.HttpContext);
            await clientEnumerableObservable.HandleConnection(httpRequestContext);
        }
        else
        {
            logger.RequestIsHttp();
            if (callResult?.Result is ObjectResult objResult)
            {
                objResult.Value = objectResult.Value;
            }
            else if (callResult is not null)
            {
                callResult.Result = new ObjectResult(objectResult.Value);
            }
        }
    }

    QueryResult ExtractSnapshotFromClientObservable(IClientObservable clientObservable)
    {
        // ClientObservable wraps a BehaviorSubject - extract its current value
        var clientObservableType = clientObservable.GetType();

        // Primary constructor parameters in C# are stored with <parameterName>P naming convention
        var allFields = clientObservableType.GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var subjectField = allFields.FirstOrDefault(f => f.Name.Contains("subject")) ?? throw new InvalidOperationException("ClientObservable does not contain expected subject field");
        var subject = subjectField.GetValue(clientObservable) ?? throw new InvalidOperationException("ClientObservable subject is null");

        // Get the Value property from BehaviorSubject
        var valueProperty = subject.GetType().GetProperty("Value") ?? throw new InvalidOperationException("Subject does not have a Value property");
        var currentValue = valueProperty.GetValue(subject);

        return new QueryResult
        {
            Data = currentValue!,
            IsAuthorized = true,
            ValidationResults = [],
            ExceptionMessages = [],
            ExceptionStackTrace = string.Empty,
            Paging = new(queryContextManager.Current.Paging.Page, queryContextManager.Current.Paging.Size, queryContextManager.Current.TotalItems)
        };
    }
}
