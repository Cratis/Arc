// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Mvc;

namespace Cratis.Arc.Queries;

/// <summary>
/// Represents an action result that executes a client observable (HTTP snapshot or WebSocket stream).
/// </summary>
public class ClientObservableResult(IClientObservable clientObservable, JsonOptions jsonOptions) : IActionResult
{
    /// <inheritdoc/>
    public async Task ExecuteResultAsync(ActionContext context)
    {
        // Check if this is a WebSocket request
        if (context.HttpContext.WebSockets.IsWebSocketRequest)
        {
            await clientObservable.HandleConnection(context.HttpContext);
        }
        else
        {
            // HTTP request - extract and return current snapshot
            var clientObservableType = clientObservable.GetType();

            // ClientObservable<T> has a 'subject' field that contains the ISubject<T>
            var subjectField = clientObservableType.GetField("subject", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (subjectField is null)
            {
                context.HttpContext.Response.StatusCode = 500;
                await context.HttpContext.Response.WriteAsJsonAsync(new { message = "Unable to extract subject from ClientObservable" }, jsonOptions.JsonSerializerOptions);
                return;
            }

            var subject = subjectField.GetValue(clientObservable);
            if (subject is null)
            {
                context.HttpContext.Response.StatusCode = 500;
                await context.HttpContext.Response.WriteAsJsonAsync(new { message = "Subject is null" }, jsonOptions.JsonSerializerOptions);
                return;
            }

            // Check if it's a BehaviorSubject which has a Value property
            var valueProperty = subject.GetType().GetProperty("Value");
            if (valueProperty is not null)
            {
                var currentValue = valueProperty.GetValue(subject);

                // DEBUG: Log what we're returning
                Console.WriteLine($"[ClientObservableResult] Returning snapshot: {System.Text.Json.JsonSerializer.Serialize(currentValue, jsonOptions.JsonSerializerOptions)}");

                // Return a full QueryResult matching the expected structure
                var result = new
                {
                    data = currentValue,
                    isSuccess = true,
                    isAuthorized = true,
                    isValid = true,
                    hasExceptions = false,
                    validationResults = Array.Empty<object>(),
                    exceptionMessages = Array.Empty<string>(),
                    exceptionStackTrace = string.Empty,
                    paging = new
                    {
                        page = 0,
                        size = 0,
                        totalItems = 0,
                        totalPages = 0
                    }
                };

                context.HttpContext.Response.StatusCode = 200;
                await context.HttpContext.Response.WriteAsJsonAsync(result, jsonOptions.JsonSerializerOptions);
            }
            else
            {
                // Observable without current state requires WebSocket
                context.HttpContext.Response.StatusCode = 400;
                await context.HttpContext.Response.WriteAsJsonAsync(new { message = "Observable queries require WebSocket connection" }, jsonOptions.JsonSerializerOptions);
            }
        }
    }
}
