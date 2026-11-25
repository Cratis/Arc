// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using System.Text.Json;
using Cratis.Arc.Commands;
using Cratis.Arc.Exceptions;
using Cratis.Arc.Queries;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Mcp;

/// <summary>
/// Runs the operation behind a tool through the same hosted pipeline entry points REST uses, as the verified caller.
/// </summary>
/// <remarks>
/// Only Arc's own pipelines prepare authorization the way REST does. With a replaced pipeline, tools fail closed.
/// </remarks>
/// <param name="commands">The command pipeline.</param>
/// <param name="queries">The query pipeline.</param>
/// <param name="performers">The query performers.</param>
/// <param name="arcOptions">The Arc options, for serialization.</param>
internal sealed class McpOperations(
    ICommandPipeline commands,
    IQueryPipeline queries,
    IQueryPerformerProviders performers,
    IOptions<ArcOptions> arcOptions)
{
    /// <summary>
    /// The message for a tool whose pipeline cannot run it the way REST does.
    /// </summary>
    internal const string Unsupported = "The tool is not available on this host.";

    /// <summary>
    /// Runs the operation.
    /// </summary>
    /// <param name="call">The call.</param>
    /// <returns>The tool result.</returns>
    /// <exception cref="McpArgumentsInvalid">The arguments do not fit the operation.</exception>
    internal ValueTask<McpToolResult> Run(McpOperationCall call) => call.Tool.Kind switch
    {
        McpToolKind.Command => RunCommand(call),
        McpToolKind.Query => RunQuery(call),
        _ => ((IMcpToolSource)call.Tool.Operation!).Invoke(new McpToolInvocation(call.Tool, call.Caller, call.Arguments, call.Services, call.CancellationToken) { Request = call.Request })
    };

    static object Outcome(CommandResult result) => Outcome(
        result.IsSuccess, result.IsAuthorized, result.AuthorizationFailureReason, result.IsRetryable, result.IsValid, result.ValidationResults, result.ErrorCode);

    static object Outcome(QueryResult result) => Outcome(
        result.IsSuccess, result.IsAuthorized, result.AuthorizationFailureReason, result.IsRetryable, result.IsValid, result.ValidationResults, result.ErrorCode);

    static object Outcome(bool isSuccess, bool isAuthorized, string authorizationFailureReason, bool isRetryable, bool isValid, IEnumerable<Cratis.Arc.Validation.ValidationResult> results, string? errorCode)
    {
        var reason = string.IsNullOrEmpty(authorizationFailureReason) ? null : authorizationFailureReason;
        var validationResults = results.Select(_ => new { severity = _.Severity.ToString(), message = _.Message, members = _.Members });
        return errorCode is null
            ? new { isSuccess, isAuthorized, reason, isRetryable, isValid, validationResults }
            : new { isSuccess, isAuthorized, reason, isRetryable, isValid, validationResults, errorCode };
    }

    static object QueryEnvelope(QueryResult result) => new
    {
        outcome = Outcome(result),
        data = result.IsSuccess ? result.Data : null,
        paging = result.IsSuccess ? result.Paging : null
    };

    static ILogger ResponseLogger(IServiceProvider services) => services.GetService<ILoggerFactory>()?.CreateLogger<McpOperations>() ?? NullLogger<McpOperations>.Instance;

    static void RejectUndeclared(McpOperationCall call)
    {
        if (call.Tool.InputSchema is null)
        {
            return;
        }

        using var schema = JsonDocument.Parse(call.Tool.InputSchema);
        if (!schema.RootElement.TryGetProperty("properties", out var properties) || properties.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        foreach (var key in call.Arguments.Keys)
        {
            if (!properties.TryGetProperty(key, out _))
            {
                throw new McpArgumentsInvalid(call.Tool.Name);
            }
        }
    }

    static bool DeclaresStreamingResult(IQueryPerformer performer) =>
        performer is IAuthorizationQueryTarget target && IsStreamingType(target.AuthorizationMethod.ReturnType);

    static bool IsStreamingType(Type type)
    {
        if (IsResultWrapper(type))
        {
            return IsStreamingType(type.GetGenericArguments()[0]);
        }

        // Match ObservableQueryHandler's stream shapes, including an interface declared as the return type.
        return type.GetInterfaces().Append(type).Any(candidate =>
            HasGenericDefinition(candidate, typeof(ISubject<>)) || HasGenericDefinition(candidate, typeof(IAsyncEnumerable<>)));
    }

    static bool IsResultWrapper(Type type) =>
        HasGenericDefinition(type, typeof(Task<>)) ||
        HasGenericDefinition(type, typeof(ValueTask<>)) ||
        HasGenericDefinition(type, typeof(ActionResult<>));

    static bool HasGenericDefinition(Type type, Type definition) => type.IsGenericType && type.GetGenericTypeDefinition() == definition;

    async ValueTask<McpToolResult> RunCommand(McpOperationCall call)
    {
        var commandType = (Type)call.Tool.Operation!;
        var command = Deserialize(call, commandType);
        if (commands is not CommandPipeline hosted)
        {
            return McpToolResult.Error(Unsupported);
        }

        var result = await hosted.ExecuteHosted(command, call.Services, null, call.CancellationToken);
        ExceptionResponseMapping.Apply(result, call.Services, ResponseLogger(call.Services));
        var outcome = Outcome(result);
        return McpOperationResult.From(new { outcome, response = result.IsSuccess ? result.ResponsePayload : null }, !result.IsSuccess, arcOptions.Value.JsonSerializerOptions);
    }

    async ValueTask<McpToolResult> RunQuery(McpOperationCall call)
    {
        var name = (FullyQualifiedQueryName)call.Tool.Operation!;
        if (!performers.TryGetPerformersFor(name, out var performer))
        {
            throw new McpArgumentsInvalid(call.Tool.Name);
        }

        RejectUndeclared(call);
        var (arguments, paging) = McpQueryArguments.From(call, performer, arcOptions.Value.JsonSerializerOptions);
        if (DeclaresStreamingResult(performer) || queries is not QueryPipeline hosted)
        {
            return McpToolResult.Error(Unsupported);
        }

        var result = await hosted.PerformHosted(name, arguments, paging, Sorting.None, call.Services, call.CancellationToken);
        try
        {
            if (result.IsSuccess && call.Services.GetService<IObservableQueryHandler>()?.IsStreamingResult(result.Data) == true)
            {
                // Opaque declared returns (such as object) cannot be checked before execution; keep this fallback.
                // A tool call is one request and one response: a stream would outlive the caller's admission and emission identity.
                return McpToolResult.Error(Unsupported);
            }

            var services = result.OwnedScope?.ServiceProvider ?? call.Services;
            ExceptionResponseMapping.Apply(result, services, ResponseLogger(services));
            return McpOperationResult.From(QueryEnvelope(result), !result.IsSuccess, arcOptions.Value.JsonSerializerOptions);
        }
        finally
        {
            result.ReleaseScope();
        }
    }

    object Deserialize(McpOperationCall call, Type commandType)
    {
        try
        {
            using var buffer = new MemoryStream();
            using (var writer = new Utf8JsonWriter(buffer))
            {
                writer.WriteStartObject();
                foreach (var (key, value) in call.Arguments)
                {
                    writer.WritePropertyName(key);
                    value.WriteTo(writer);
                }
                writer.WriteEndObject();
            }

            using var input = JsonDocument.Parse(buffer.ToArray());
            using var schema = JsonDocument.Parse(call.Tool.InputSchema!);
            if (!McpCommandArguments.Matches(input.RootElement, schema.RootElement, arcOptions.Value.JsonSerializerOptions))
            {
                throw new McpArgumentsInvalid(call.Tool.Name);
            }

            return JsonSerializer.Deserialize(buffer.ToArray(), commandType, arcOptions.Value.JsonSerializerOptions)
                ?? throw new McpArgumentsInvalid(call.Tool.Name);
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException or InvalidOperationException or ArgumentException or FormatException)
        {
            throw new McpArgumentsInvalid(call.Tool.Name, ex);
        }
    }
}
