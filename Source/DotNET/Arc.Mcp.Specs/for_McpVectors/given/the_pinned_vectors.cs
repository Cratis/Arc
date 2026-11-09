// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Arc.Mcp.for_McpVectors.given;

/// <summary>
/// Loads the pinned MCP authorization vectors.
/// </summary>
/// <remarks>
/// The vectors are pinned from the Cratis Identity repository at commit 6d7f3ee59411dca252165c084df8ad837ba07b1c.
/// They are byte-identical to that commit; the checksum spec fails when they drift.
/// </remarks>
public class the_pinned_vectors : Specification
{
    internal const string FileName = "arc-mcp-authorization-v1.profiles-and-discovery.json";
    internal const string PinnedSha256 = "77f118f6f86c85f0e6d89dc87960ddbbfc802b72e69e775dbc3c529b3044e826";

    protected byte[] _bytes;
    protected JsonArray _vectors;

    void Establish()
    {
        var assembly = typeof(the_pinned_vectors).Assembly;
        var name = assembly.GetManifestResourceNames().Single(_ => _.EndsWith(FileName, StringComparison.Ordinal));
        using var stream = assembly.GetManifestResourceStream(name)!;
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        _bytes = buffer.ToArray();
        _vectors = JsonNode.Parse(_bytes)!["given"]!["vectors"]!.AsArray();
    }
}
