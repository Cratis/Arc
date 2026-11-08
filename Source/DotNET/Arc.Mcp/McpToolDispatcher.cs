// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Arc.Capabilities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;

namespace Cratis.Arc.Mcp;

/// <summary>
/// Runs a tool from the ceiling as the verified caller of the current request.
/// </summary>
/// <remarks>
/// The caller comes only from the request the capability gate admitted. Without one nothing is resolved and the call is a
/// protocol error. Identity and tenant are never read from the arguments: they only become command or query input.
/// </remarks>
/// <param name="requests">The accessor for the current request.</param>
/// <param name="ceiling">The factory of the tool ceiling.</param>
/// <param name="operations">Runs commands, queries and curated tools.</param>
/// <param name="logger">The logger.</param>
internal sealed class McpToolDispatcher(
    IHttpContextAccessor requests,
    McpToolCeilingFactory ceiling,
    McpOperations operations,
    ILogger<McpToolDispatcher> logger)
{
    /// <summary>
    /// The message for arguments that do not fit the tool.
    /// </summary>
    internal const string InvalidArguments = "The arguments are not valid for this tool.";

    /// <summary>
    /// The message for a tool the caller's scopes do not cover.
    /// </summary>
    internal const string InsufficientScope = "The caller lacks a scope this tool requires.";

    /// <summary>
    /// The message for a tool an authorizer denied.
    /// </summary>
    internal const string NotAllowed = "The caller is not allowed to call this tool.";

    /// <summary>
    /// The message for a tool that failed.
    /// </summary>
    internal const string Failed = "The tool failed.";

    /// <summary>
    /// Dispatches a tool call.
    /// </summary>
    /// <param name="tool">The tool being called.</param>
    /// <param name="arguments">The arguments the client sent.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The tool result.</returns>
    /// <exception cref="McpProtocolException">The request carries no verified caller, or the tool is not in the ceiling.</exception>
    internal async ValueTask<CallToolResult> Dispatch(McpToolDescriptor tool, IDictionary<string, JsonElement>? arguments, CancellationToken cancellationToken)
    {
        var context = requests.HttpContext;
        if (McpCaller.From(context) is not { Capability.Protocol: CapabilityProtocol.Mcp } caller)
        {
            throw new McpProtocolException("The request carries no verified caller.", McpErrorCode.InvalidRequest);
        }

        if (!ceiling.Ceiling.TryGet(tool.Name, out var known) || !ReferenceEquals(known, tool))
        {
            throw new McpProtocolException($"Unknown tool: '{tool.Name}'", McpErrorCode.InvalidParams);
        }

        if (!caller.HoldsScopesFor(tool))
        {
            return Error(InsufficientScope);
        }

        if (!await McpToolAuthorizers.Allow(tool, caller, context!.RequestServices, cancellationToken))
        {
            return Error(NotAllowed);
        }

        var call = new McpOperationCall(tool, caller.Capability, arguments?.AsReadOnly() ?? new Dictionary<string, JsonElement>().AsReadOnly(), context!.RequestServices, cancellationToken);

        // The correlation id is already set for this request by Arc's startup middleware, which runs for every request, the
        // same as REST. Receipt is captured here the way the REST endpoints do before they enter a pipeline.
        using var receipt = OperationContextScope.Begin(context!.RequestServices);
        try
        {
            var outcome = await operations.Run(call);
            if (!IsWellFormed(tool, outcome))
            {
                logger.ToolResultMalformed(tool.Name);
                return Error(Failed);
            }

            return new CallToolResult
            {
                Content = [new TextContentBlock { Text = outcome.Text }, .. outcome.ResourceLinks.Select(ToBlock)],
                StructuredContent = outcome.StructuredContent?.Clone(),
                IsError = outcome.IsError
            };
        }
        catch (McpArgumentsInvalid)
        {
            return Error(InvalidArguments);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.ToolFailed(tool.Name, ex);
            return Error(Failed);
        }
    }

    static bool IsWellFormed(McpToolDescriptor tool, McpToolResult outcome)
    {
        if (outcome.StructuredContent is { ValueKind: not JsonValueKind.Object })
        {
            return false;
        }
        if (tool.OutputSchema is not null && !outcome.IsError && outcome.StructuredContent is null)
        {
            return false;
        }

        return (outcome.ResourceLinks ?? []).All(link =>
            link is not null &&
            !string.IsNullOrWhiteSpace(link.Name) &&
            Uri.TryCreate(link.Uri, UriKind.Absolute, out _));
    }

    static ResourceLinkBlock ToBlock(McpResourceLink link) => new()
    {
        Uri = link.Uri,
        Name = link.Name,
        Title = link.Title,
        Description = link.Description,
        MimeType = link.MimeType
    };

    static CallToolResult Error(string message) => new() { Content = [new TextContentBlock { Text = message }], IsError = true };
}
