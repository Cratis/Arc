// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Mcp;

/// <summary>
/// The stable, identifier-only MCP call audit event.
/// </summary>
internal static partial class McpCallAuditLogging
{
    [LoggerMessage(5102, LogLevel.Information, "MCP tool '{Tool}' ({Kind}) called by person '{Person}' in tenant '{Tenant}' with credential '{CredentialId}' ({CredentialKind}): {Outcome} (correlation {CorrelationId})")]
    internal static partial void ToolCalled(this ILogger<McpCallAudit> logger, string tool, McpToolKind kind, string person, string tenant, string credentialId, CallerCredentialKind credentialKind, string outcome, string correlationId);
}
