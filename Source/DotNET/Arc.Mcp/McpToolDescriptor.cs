// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Mcp;

/// <summary>
/// Describes a tool that may be served over MCP.
/// </summary>
/// <param name="Name">The stable, explicit snake_case tool name.</param>
/// <param name="Description">The required tool description.</param>
/// <param name="Kind">What the tool executes.</param>
/// <param name="Operation">The command type or <see cref="Queries.FullyQualifiedQueryName"/> the tool executes; null for custom tools.</param>
/// <param name="RequiredScopes">The scopes the verified caller must hold.</param>
/// <param name="Access">Whether the tool reads or writes.</param>
/// <param name="InputSchema">The JSON schema of the tool input, if known.</param>
/// <param name="Annotations">The behavioural hints advertised to clients.</param>
public sealed record McpToolDescriptor(
    string Name,
    string Description,
    McpToolKind Kind,
    object? Operation,
    IReadOnlyList<string> RequiredScopes,
    McpToolAccess Access,
    string? InputSchema,
    McpToolAnnotations Annotations)
{
    /// <summary>
    /// Gets the alternative scope sets of a curated tool. A caller that holds every scope of
    /// <see cref="RequiredScopes"/>, or every scope of any one alternative set, may call the tool.
    /// </summary>
    /// <remarks>
    /// Every alternative set must be non-empty, and for a tool that writes it must include the write scope; otherwise
    /// startup fails. Challenges advertise <see cref="RequiredScopes"/>.
    /// </remarks>
    public IReadOnlyList<IReadOnlyList<string>> AlternativeScopes { get; init; } = [];

    /// <summary>
    /// Describes a curated tool provided by an <see cref="IMcpToolSource"/>.
    /// </summary>
    /// <param name="name">The stable, explicit snake_case tool name.</param>
    /// <param name="description">The required tool description.</param>
    /// <param name="requiredScopes">The scopes the verified caller must hold.</param>
    /// <param name="access">Whether the tool reads or writes. A tool that writes must require the write scope.</param>
    /// <param name="inputSchema">The JSON schema of the tool input; an object without declared properties when null.</param>
    /// <param name="annotations">The behavioural hints; the conservative hints for <paramref name="access"/> when null.</param>
    /// <returns>The descriptor.</returns>
    public static McpToolDescriptor Custom(
        string name,
        string description,
        IReadOnlyList<string> requiredScopes,
        McpToolAccess access,
        string? inputSchema = null,
        McpToolAnnotations? annotations = null) =>
        new(
            name,
            description,
            McpToolKind.Custom,
            null,
            requiredScopes,
            access,
            inputSchema,
            annotations ?? (access == McpToolAccess.Write ? McpToolAnnotations.ForWrite : McpToolAnnotations.ForRead));

    /// <summary>
    /// Determines whether a set of held scopes satisfies the tool: every scope of <see cref="RequiredScopes"/> or of any
    /// one of the <see cref="AlternativeScopes"/>. An empty set is never satisfied.
    /// </summary>
    /// <param name="held">The scopes the caller holds.</param>
    /// <returns>True when the held scopes satisfy the tool.</returns>
    /// <remarks>The only scope check: listing, admission and dispatch all use it so they cannot disagree.</remarks>
    internal bool IsSatisfiedBy(IReadOnlySet<string> held) =>
        Covers(RequiredScopes, held) || (AlternativeScopes ?? []).Any(_ => Covers(_, held));

    static bool Covers(IReadOnlyList<string>? scopes, IReadOnlySet<string> held) =>
        scopes is { Count: > 0 } && scopes.All(_ => !string.IsNullOrWhiteSpace(_) && held.Contains(_));
}
