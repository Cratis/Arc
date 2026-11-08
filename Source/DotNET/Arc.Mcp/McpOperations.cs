// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Arc.Commands;
using Cratis.Arc.Queries;
using Cratis.Arc.Validation;
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
        _ => ((IMcpToolSource)call.Tool.Operation!).Invoke(new McpToolInvocation(call.Tool, call.Caller, call.Arguments, call.Services, call.CancellationToken))
    };

    static object Outcome(bool isSuccess, bool isAuthorized, bool isValid, IEnumerable<ValidationResult> validationResults) => new
    {
        isSuccess,
        isAuthorized,
        isValid,
        validationResults = validationResults.Select(_ => new { severity = _.Severity.ToString(), message = _.Message, members = _.Members })
    };

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

    async ValueTask<McpToolResult> RunCommand(McpOperationCall call)
    {
        var commandType = (Type)call.Tool.Operation!;
        var command = Deserialize(call, commandType);
        if (commands is not CommandPipeline hosted)
        {
            return McpToolResult.Error(Unsupported);
        }

        var result = await hosted.ExecuteHosted(command, call.Services, null, call.CancellationToken);
        var outcome = Outcome(result.IsSuccess, result.IsAuthorized, result.IsValid, result.ValidationResults);
        var text = JsonSerializer.Serialize(new { outcome, response = result.IsSuccess ? result.ResponseValue : null }, arcOptions.Value.JsonSerializerOptions);
        return new McpToolResult(text, !result.IsSuccess);
    }

    async ValueTask<McpToolResult> RunQuery(McpOperationCall call)
    {
        var name = (FullyQualifiedQueryName)call.Tool.Operation!;
        if (!performers.TryGetPerformersFor(name, out var performer))
        {
            throw new McpArgumentsInvalid(call.Tool.Name);
        }

        RejectUndeclared(call);
        var (arguments, paging) = McpQueryArguments.From(call, performer);
        if (queries is not QueryPipeline hosted)
        {
            return McpToolResult.Error(Unsupported);
        }

        var result = await hosted.PerformHosted(name, arguments, paging, Sorting.None, call.Services, call.CancellationToken);
        using var ownedScope = result.OwnedScope;
        var outcome = Outcome(result.IsSuccess, result.IsAuthorized, result.IsValid, result.ValidationResults);
        var text = JsonSerializer.Serialize(
            new { outcome, data = result.IsSuccess ? result.Data : null, paging = result.IsSuccess ? result.Paging : null },
            arcOptions.Value.JsonSerializerOptions);
        return new McpToolResult(text, !result.IsSuccess);
    }

    object Deserialize(McpOperationCall call, Type commandType)
    {
        RejectUndeclared(call);
        try
        {
            using var buffer = new MemoryStream();
            using (var writer = new Utf8JsonWriter(buffer))
            {
                writer.WriteStartObject();
                foreach (var (key, value) in call.Arguments.Where(_ => _.Key != McpSchemaBuilder.IdempotencyKeyName))
                {
                    writer.WritePropertyName(key);
                    value.WriteTo(writer);
                }
                writer.WriteEndObject();
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
