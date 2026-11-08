// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Mcp;

/// <summary>
/// Represents the result of a curated tool.
/// </summary>
/// <param name="Text">The text returned to the client, typically JSON.</param>
/// <param name="IsError">Whether the tool failed. The text of a failure is shown to the client, so keep it free of internals.</param>
public sealed record McpToolResult(string Text, bool IsError = false)
{
    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="text">The text returned to the client.</param>
    /// <returns>The result.</returns>
    public static McpToolResult Success(string text) => new(text);

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    /// <param name="text">A message for the client, free of internals.</param>
    /// <returns>The result.</returns>
    public static McpToolResult Error(string text) => new(text, true);
}
