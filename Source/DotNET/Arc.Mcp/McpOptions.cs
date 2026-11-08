// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Mcp;

/// <summary>
/// Configures MCP serving.
/// </summary>
public class McpOptions
{
    string? _readScope;
    string? _writeScope;

    /// <summary>
    /// Gets or sets the path the MCP endpoint is served on.
    /// </summary>
    public string Path { get; set; } = "/mcp";

    /// <summary>
    /// Gets or sets the canonical public resource URI. Required for OAuth and combined profiles.
    /// </summary>
    public string? Resource { get; set; }

    /// <summary>
    /// Gets or sets the configured public origin used in metadata. Never derived from the request host.
    /// Required for OAuth and combined profiles.
    /// </summary>
    public string? PublicOrigin { get; set; }

    /// <summary>
    /// Gets the issuer URLs of the authorization servers. Required for OAuth and combined profiles.
    /// </summary>
    public IList<string> AuthorizationServers { get; } = [];

    /// <summary>
    /// Gets the scopes advertised as supported.
    /// </summary>
    public IList<string> ScopesSupported { get; } = [];

    /// <summary>
    /// Gets or sets the application name used for the default <c>&lt;app&gt;:read</c> and <c>&lt;app&gt;:write</c> scopes.
    /// </summary>
    public string? ApplicationName { get; set; }

    /// <summary>
    /// Gets or sets the scope required by read tools. Defaults to <c>&lt;ApplicationName&gt;:read</c>; null when neither is configured.
    /// </summary>
    public string? ReadScope
    {
        get => _readScope ?? DefaultScope("read");
        set => _readScope = value;
    }

    /// <summary>
    /// Gets or sets the scope required by write tools. Defaults to <c>&lt;ApplicationName&gt;:write</c>; null when neither is configured.
    /// </summary>
    public string? WriteScope
    {
        get => _writeScope ?? DefaultScope("write");
        set => _writeScope = value;
    }

    /// <summary>
    /// Gets or sets the accepted credential profile.
    /// </summary>
    public McpProfile Profile { get; set; } = McpProfile.PatOnly;

    /// <summary>
    /// Gets or sets the largest MCP request body, in bytes, that is read. Larger requests are refused before any tool runs.
    /// </summary>
    public long MaxRequestBodySize { get; set; } = 1024 * 1024;

    string? DefaultScope(string suffix) => string.IsNullOrWhiteSpace(ApplicationName) ? null : $"{ApplicationName}:{suffix}";
}
