// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Mcp;

/// <summary>
/// Log messages for <see cref="McpToolAuthorizers"/>.
/// </summary>
internal static partial class McpToolAuthorizersLogging
{
    [LoggerMessage(LogLevel.Warning, "An MCP tool authorizer failed for tool '{Tool}'; access is denied")]
    internal static partial void AuthorizerFailed(this ILogger logger, string tool, Exception exception);
}
