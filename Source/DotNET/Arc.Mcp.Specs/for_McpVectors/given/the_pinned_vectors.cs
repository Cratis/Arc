// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Arc.Mcp.for_McpVectors.given;

/// <summary>
/// Loads the pinned MCP authorization vectors.
/// </summary>
/// <remarks>
/// The vectors are pinned from the Cratis Identity repository at the vectors/arc-mcp-authorization/v2.0.0 release tag.
/// They are byte-identical to that tag; the checksum spec fails when they drift.
/// </remarks>
public class the_pinned_vectors : Specification
{
    internal const string FileName = "arc-mcp-authorization-v1.profiles-and-discovery.json";
    internal const string PinnedSha256 = "66d49418691ce5486f613d50de84e6429247fd3acac0295f49e6a7c93c52e08b";

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
