// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Arc.Http;
using Cratis.Arc.Validation;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Commands;

/// <summary>
/// Maps command endpoints using the provided endpoint mapper.
/// </summary>
public static class CommandEndpointMapper
{
    /// <summary>
    /// Maps all command endpoints.
    /// </summary>
    /// <param name="mapper">The <see cref="IEndpointMapper"/> to use.</param>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/>.</param>
    public static void MapCommandEndpoints(this IEndpointMapper mapper, IServiceProvider serviceProvider)
    {
        var arcOptions = serviceProvider.GetRequiredService<IOptions<ArcOptions>>().Value;
        var options = arcOptions.GeneratedApis;
        var commandHandlerProviders = serviceProvider.GetRequiredService<ICommandHandlerProviders>();

        var handlersByNamespace = EndpointRouteHelper.GroupByNamespace(
            commandHandlerProviders.Handlers,
            h => h.Location,
            options.SegmentsToSkipForRoute);

        foreach (var handler in commandHandlerProviders.Handlers)
        {
            var location = handler.Location.Skip(options.SegmentsToSkipForRoute);
            var includeCommandName = EndpointRouteHelper.ShouldIncludeNameInRoute(
                options.IncludeCommandNameInRoute,
                location,
                handlersByNamespace);
            var url = EndpointRouteHelper.BuildRouteUrl(options, handler.Location, options.SegmentsToSkipForRoute, handler.CommandType.Name, includeCommandName);

            MapCommandEndpoint(
                mapper,
                url,
                $"Execute{handler.CommandType.FullName}",
                $"Execute {handler.CommandType.Name} command in {handler.CommandType.Namespace}",
                handler.CommandType,
                location,
                handler.AllowsAnonymousAccess);

            MapCommandEndpoint(
                mapper,
                $"{url}/validate",
                $"Validate{handler.CommandType.FullName}",
                $"Validate {handler.CommandType.Name} command without executing it",
                handler.CommandType,
                location,
                handler.AllowsAnonymousAccess,
                validateOnly: true);
        }
    }

    /// <summary>
    /// Reads and deserializes the request body into a command instance, mapping a deserialization failure
    /// (malformed or wrong-typed body) to a validation failure (HTTP 400) instead of a server error (HTTP 500).
    /// </summary>
    /// <param name="context">The <see cref="IHttpRequestContext"/> to read the body from.</param>
    /// <param name="commandType">The type of command to deserialize.</param>
    /// <param name="correlationId">The <see cref="CorrelationId"/> associated with the request.</param>
    /// <param name="logger">The <see cref="ILogger"/> used to log the underlying parser detail server-side.</param>
    /// <returns>
    /// A tuple with the deserialized command (when successful) or a <see cref="CommandResult"/> describing the
    /// failure (when the body could not be read). Exactly one of the two is non-null.
    /// </returns>
    internal static async Task<(object? Command, CommandResult? Failure)> ReadCommandBody(
        IHttpRequestContext context,
        Type commandType,
        CorrelationId correlationId,
        ILogger logger)
    {
        try
        {
            var command = await context.ReadBodyAsJson(commandType, context.RequestAborted);
            if (command is null)
            {
                logger.FailedToReadCommandBody(commandType.FullName ?? commandType.Name, null);
                return (null, CommandResult.InvalidBody(correlationId));
            }

            return (command, null);
        }
        catch (JsonException ex)
        {
            logger.FailedToReadCommandBody(commandType.FullName ?? commandType.Name, ex);
            return (null, CommandResult.InvalidBody(correlationId));
        }
    }

    static void MapCommandEndpoint(
        IEndpointMapper mapper,
        string url,
        string endpointName,
        string summary,
        Type commandType,
        IEnumerable<string> location,
        bool allowAnonymous,
        bool validateOnly = false)
    {
        if (mapper.EndpointExists(endpointName))
        {
            return;
        }

        var metadata = new EndpointMetadata(
            endpointName,
            summary,
            [string.Join('.', location)],
            allowAnonymous,
            RequestBodyType: commandType,
            ResponseType: typeof(CommandResult));

        mapper.MapPost(
            url,
            async context =>
            {
                var correlationIdAccessor = context.RequestServices.GetRequiredService<ICorrelationIdAccessor>();
                var commandPipeline = context.RequestServices.GetRequiredService<ICommandPipeline>();
                var arcOptions = context.RequestServices.GetRequiredService<IOptions<ArcOptions>>().Value;
                var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(CommandEndpointMapper).FullName!);

                context.HandleCorrelationId(correlationIdAccessor, arcOptions.CorrelationId);

                ValidationResultSeverity? allowedSeverity = default;
                if (context.Headers.TryGetValue("X-Allowed-Severity", out var severityHeader) &&
                    int.TryParse(severityHeader, out var severityValue))
                {
                    allowedSeverity = (ValidationResultSeverity)severityValue;
                }

                CommandResult commandResult;
                try
                {
                    var (command, bodyFailure) = await ReadCommandBody(context, commandType, correlationIdAccessor.Current, logger);
                    commandResult = bodyFailure ?? (validateOnly
                        ? await commandPipeline.Validate(command!, context.RequestServices, allowedSeverity, context.RequestAborted)
                        : await commandPipeline.Execute(command!, context.RequestServices, allowedSeverity, context.RequestAborted));
                }
                catch (Exception ex)
                {
                    commandResult = CommandResult.Error(correlationIdAccessor.Current, ex);
                }

                ExceptionDetailRedactor.Redact(commandResult, arcOptions.ExposeExceptionDetails, logger);

                var statusCode = EndpointRouteHelper.GetStatusCode(commandResult.IsSuccess, commandResult.IsAuthorized, commandResult.IsValid);
                context.SetStatusCode(statusCode);
                await context.WriteResponseAsJson(commandResult, commandResult.GetType(), context.RequestAborted);
            },
            metadata);
    }
}
