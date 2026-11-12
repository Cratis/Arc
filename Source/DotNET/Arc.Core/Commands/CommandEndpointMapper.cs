// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Arc.Authorization;
using Cratis.Arc.EndpointGroups;
using Cratis.Arc.Http;
using Cratis.Arc.Identity.Approvals;
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
            var endpointGroup = EndpointGroup.EnsureServed(handler.CommandType, arcOptions.EndpointGroups);
            var location = handler.Location.Skip(options.SegmentsToSkipForRoute);
            var includeCommandName = EndpointRouteHelper.ShouldIncludeNameInRoute(
                options.IncludeCommandNameInRoute,
                location,
                handlersByNamespace);
            var url = CommandRoute.CustomRoute(handler) ??
                EndpointRouteHelper.BuildRouteUrl(arcOptions.EndpointGroups.RoutePrefixFor(endpointGroup, options.RoutePrefix), handler.Location, options.SegmentsToSkipForRoute, handler.CommandType.Name, includeCommandName);

            var excludeFromApiDescription = endpointGroup is not null || handler.CommandType.IsDefined(typeof(ExcludeFromDiscoveryAttribute), true);
            var allowAnonymous = handler.AllowsAnonymousAccess || GuestPolicyEndpointAccess.ForCommand(handler.CommandType, serviceProvider);

            var shape = new CommandEndpointShape(handler.CommandType, location, allowAnonymous, endpointGroup, excludeFromApiDescription);

            MapCommandEndpoint(
                mapper,
                shape,
                new CommandEndpointRoute(
                    url,
                    $"Execute{handler.CommandType.FullName}",
                    $"Execute {handler.CommandType.Name} command in {handler.CommandType.Namespace}",
                    ValidateOnly: false));

            MapCommandEndpoint(
                mapper,
                shape,
                new CommandEndpointRoute(
                    $"{url}/validate",
                    $"Validate{handler.CommandType.FullName}",
                    $"Validate {handler.CommandType.Name} command without executing it",
                    ValidateOnly: true));
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
                return (null, CommandBodyBindingFailure.From(correlationId, null));
            }

            return (command, null);
        }
        catch (Exception ex) when (ex is JsonException or FormatException or OverflowException or ArgumentException)
        {
            // A malformed body (JsonException) or a value the concept/type converters cannot parse is invalid
            // client input, not a server fault: FormatException/OverflowException from a Guid- or numeric-backed
            // concept property fed a wrong-type string, and ArgumentException from an enum-backed concept. These
            // escape the JSON deserializer unwrapped, so without this they would surface as an HTTP 500 rather than
            // the 400 the body-read contract promises. Cancellation and genuine server faults are not matched here
            // and continue to surface as a server error.
            logger.FailedToReadCommandBody(commandType.FullName ?? commandType.Name, ex);
            return (null, CommandBodyBindingFailure.From(correlationId, ex));
        }
    }

    static void MapCommandEndpoint(IEndpointMapper mapper, CommandEndpointShape shape, CommandEndpointRoute route)
    {
        if (mapper.EndpointExists(route.Name))
        {
            return;
        }

        var metadata = new EndpointMetadata(
            route.Name,
            route.Summary,
            [string.Join('.', shape.Location)],
            shape.AllowAnonymous,
            RequestBodyType: shape.CommandType,
            ResponseType: typeof(CommandResult),
            ExcludeFromApiDescription: shape.ExcludeFromApiDescription)
        {
            EndpointGroup = shape.EndpointGroup
        };

        mapper.MapPost(route.Url, context => HandleCommandRequest(context, shape.CommandType, route.ValidateOnly), metadata);
    }

    static async Task HandleCommandRequest(IHttpRequestContext context, Type commandType, bool validateOnly)
    {
        using var receipt = OperationContextScope.Begin(context.RequestServices);
        var correlationIdAccessor = context.RequestServices.GetRequiredService<ICorrelationIdAccessor>();
        var commandPipeline = context.RequestServices.GetRequiredService<ICommandPipeline>();
        var arcOptions = context.RequestServices.GetRequiredService<IOptions<ArcOptions>>().Value;
        var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(CommandEndpointMapper).FullName!);

        context.HandleCorrelationId(correlationIdAccessor, arcOptions.CorrelationId);

        var request = new CommandRequest(
            context,
            commandType,
            validateOnly,
            commandPipeline,
            correlationIdAccessor,
            CallerValidationSeverity.Cap(commandType, CallerValidationSeverity.FromHeaders(context.Headers), arcOptions.Command),
            logger);

        var commandResult = await ProcessRequest(request);

        ExceptionDetailRedactor.Redact(commandResult, arcOptions.ExposeExceptionDetails, logger);

        var statusCode = EndpointRouteHelper.GetStatusCode(commandResult.IsSuccess, commandResult.IsAuthorized, commandResult.IsValid);
        context.SetStatusCode(statusCode);
        await context.WriteResponseAsJson(commandResult, commandResult.GetType(), context.RequestAborted);
    }

    static async Task<CommandResult> ProcessRequest(CommandRequest request)
    {
        try
        {
            var (command, bodyFailure) = await ReadCommandBody(request.Context, request.CommandType, request.CorrelationIds.Current, request.Logger);
            if (bodyFailure is not null)
            {
                var refusal = new UnboundCommandRefusalRequest(request.CommandType, bodyFailure, request.Context.RequestServices, request.ValidateOnly);
                return await RefuseUnboundBody(request.Pipeline, refusal, request.Logger, request.Context.RequestAborted);
            }

            return request.Pipeline is CommandPipeline builtInPipeline
                ? await DispatchToBuiltInPipeline(builtInPipeline, request, command!)
                : await DispatchToCustomPipeline(request, command!);
        }
        catch (Exception ex)
        {
            return CommandResult.Error(request.CorrelationIds.Current, ex);
        }
    }

    static Task<CommandResult> DispatchToBuiltInPipeline(CommandPipeline pipeline, CommandRequest request, object command)
    {
        var services = request.Context.RequestServices;
        return request.ValidateOnly
            ? pipeline.ValidateHosted(command, services, request.AllowedSeverity, request.Context.RequestAborted)
            : pipeline.ExecuteHosted(command, services, request.AllowedSeverity, request.Context.RequestAborted);
    }

    static Task<CommandResult> DispatchToCustomPipeline(CommandRequest request, object command)
    {
        var services = request.Context.RequestServices;
        if (!CanExecuteWithCustomPipeline(services, request.CommandType, request.Logger))
        {
            return Task.FromResult(CommandResult.Unauthorized(request.CorrelationIds.Current));
        }

        if (ApprovalGate.RequiresApproval(request.CommandType))
        {
            return Task.FromResult(services.GetService<IAuthorizationEvaluator>()?.IsAuthorized(request.CommandType) == true
                ? ApprovalGate.Refusal(request.CorrelationIds.Current)
                : CommandResult.Unauthorized(request.CorrelationIds.Current));
        }

        return RunCustomPipeline(request, command);
    }

    static async Task<CommandResult> RunCustomPipeline(CommandRequest request, object command)
    {
        using var forwardedReceipt = OperationContextScope.ForwardTransportReceipt();
        var services = request.Context.RequestServices;
        return request.ValidateOnly
            ? await request.Pipeline.Validate(command, services, request.AllowedSeverity, request.Context.RequestAborted)
            : await request.Pipeline.Execute(command, services, request.AllowedSeverity, request.Context.RequestAborted);
    }

    static Task<CommandResult> RefuseUnboundBody(ICommandPipeline pipeline, UnboundCommandRefusalRequest request, ILogger logger, CancellationToken cancellationToken)
    {
        if (pipeline is CommandPipeline builtIn)
        {
            return builtIn.RefuseUnboundHosted(request, cancellationToken);
        }

        var services = request.RequestServices;
        var allowed = CanExecuteWithCustomPipeline(services, request.CommandType, logger) &&
            services.GetService<IAuthorizationEvaluator>()?.IsAuthorized(request.CommandType) == true;
        return Task.FromResult(allowed ? request.BindingFailure : CommandResult.Unauthorized(request.BindingFailure.CorrelationId));
    }

    static bool CanExecuteWithCustomPipeline(IServiceProvider services, Type commandType, ILogger logger)
    {
        try
        {
            var declarations = services.GetService<AuthorizationDeclarations>() ??
                throw new InvalidAuthorizationConfiguration("Authorization declarations are unavailable.");
            if (declarations.For(commandType).RequiresAsynchronousEvaluation)
            {
                throw new InvalidAuthorizationConfiguration($"Command '{commandType}' requires an Arc pipeline that can prepare authorization before execution.");
            }

            return true;
        }
        catch (Exception exception) when (exception is InvalidAuthorizationConfiguration or AmbiguousAuthorizationLevel)
        {
            logger.AuthorizationConfigurationFailed(exception);
            return false;
        }
    }

    sealed record CommandEndpointShape(Type CommandType, IEnumerable<string> Location, bool AllowAnonymous, string? EndpointGroup, bool ExcludeFromApiDescription);

    sealed record CommandEndpointRoute(string Url, string Name, string Summary, bool ValidateOnly);

    sealed record CommandRequest(
        IHttpRequestContext Context,
        Type CommandType,
        bool ValidateOnly,
        ICommandPipeline Pipeline,
        ICorrelationIdAccessor CorrelationIds,
        ValidationResultSeverity? AllowedSeverity,
        ILogger Logger);
}
