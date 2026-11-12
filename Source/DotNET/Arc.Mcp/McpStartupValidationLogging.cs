// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Mcp;

/// <summary>Actionable startup warnings without credential data.</summary>
internal static partial class McpStartupValidationLogging
{
    [LoggerMessage(5103, LogLevel.Warning, "MCP requires scope '{Scope}', which PatOptions.AllowedScopes does not allow. PATs cannot satisfy this tool scope. Add it to PatOptions.AllowedScopes or adjust McpOptions.ReadScope/WriteScope and the tool's scope declarations. See https://www.cratis.io/arc/backend/csharp/mcp/troubleshooting")]
    internal static partial void PatScopeMissing(this ILogger<McpStartupValidation> logger, string scope);
}
