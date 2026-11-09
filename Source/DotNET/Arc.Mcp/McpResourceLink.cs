// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Mcp;

/// <summary>
/// Represents a link to a resource returned by a curated tool, sent to the client as a <c>resource_link</c> content block.
/// </summary>
/// <param name="Uri">The absolute URI of the resource.</param>
/// <param name="Name">The name of the resource.</param>
public sealed record McpResourceLink(string Uri, string Name)
{
    /// <summary>
    /// Gets the title shown to people, if any.
    /// </summary>
    public string? Title { get; init; }

    /// <summary>
    /// Gets the description of the resource, if any.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets the MIME type of the resource, if known.
    /// </summary>
    public string? MimeType { get; init; }
}
