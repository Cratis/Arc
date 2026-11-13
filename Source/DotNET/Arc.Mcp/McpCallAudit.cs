// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Execution;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Mcp;

/// <summary>
/// Records identifiers and outcomes only, never credentials, arguments or results.
/// </summary>
/// <param name="logger">The audit logger.</param>
/// <param name="correlationIds">The current request correlation identifier.</param>
internal sealed class McpCallAudit(ILogger<McpCallAudit> logger, ICorrelationIdAccessor correlationIds)
{
    /// <summary>Records one tool call.</summary>
    /// <param name="tool">The tool called.</param>
    /// <param name="caller">The verified caller.</param>
    /// <param name="outcome">The outcome of the call.</param>
    internal void Record(McpToolDescriptor tool, McpCaller caller, string outcome) => logger.ToolCalled(
        tool.Name,
        tool.Kind,
        caller.Verified.Person?.Value ?? string.Empty,
        caller.Verified.Tenant?.Value ?? string.Empty,
        caller.Verified.CredentialId.Value,
        caller.Verified.CredentialKind,
        outcome,
        correlationIds.Current.ToString());
}
