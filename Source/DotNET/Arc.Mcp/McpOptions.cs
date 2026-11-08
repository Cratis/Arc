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
    /// Gets the browser origins allowed to call the MCP endpoint, in addition to <see cref="PublicOrigin"/>. A request
    /// without an <c>Origin</c> header (a non-browser client) is always allowed; a request whose <c>Origin</c> is not
    /// listed is refused. Empty by default.
    /// </summary>
    public IList<string> AllowedOrigins { get; } = [];

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
    /// Gets the other write scopes of the application. A curated write tool must require, in every scope set it declares,
    /// at least one write scope: <see cref="WriteScope"/> or one listed here. <see cref="WriteScope"/> is always a write
    /// scope, so an empty list means the write scopes are just <see cref="WriteScope"/>. A blank or repeated entry, or an
    /// entry equal to <see cref="ReadScope"/>, fails startup.
    /// </summary>
    public IList<string> WriteScopes { get; } = [];

    /// <summary>
    /// Gets or sets the accepted credential profile.
    /// </summary>
    public McpProfile Profile { get; set; } = McpProfile.PatOnly;

    /// <summary>
    /// Gets or sets the largest MCP request body, in bytes, that is read. Larger requests are refused before any tool runs.
    /// </summary>
    public long MaxRequestBodySize { get; set; } = 1024 * 1024;

    /// <summary>
    /// Gets or sets a value indicating whether only the tools of the ceiling are served. True by default: tools the
    /// application registers directly on the MCP server, and any custom tool list or call handlers, are removed.
    /// Set it to false to serve those tools next to the ceiling; they are not governed by it, so every admitted caller
    /// can list and call them without a scope check, and a warning is logged at startup.
    /// </summary>
    public bool ExclusiveTools { get; set; } = true;

    /// <summary>
    /// Gets every configured write scope: <see cref="WriteScope"/> first, when configured, followed by <see cref="WriteScopes"/>.
    /// </summary>
    /// <returns>The distinct write scopes, in configured order.</returns>
    internal IReadOnlyList<string> ConfiguredWriteScopes() =>
        [.. new[] { WriteScope }.Concat(WriteScopes).OfType<string>().Where(_ => !string.IsNullOrWhiteSpace(_)).Distinct(StringComparer.Ordinal)];

    string? DefaultScope(string suffix) => string.IsNullOrWhiteSpace(ApplicationName) ? null : $"{ApplicationName}:{suffix}";
}
