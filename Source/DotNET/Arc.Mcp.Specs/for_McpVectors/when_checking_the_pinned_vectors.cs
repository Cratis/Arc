// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using Cratis.Arc.Mcp.for_McpVectors.given;

namespace Cratis.Arc.Mcp.for_McpVectors;

public class when_checking_the_pinned_vectors : the_pinned_vectors
{
    string _sha256;

    void Because() => _sha256 = Convert.ToHexString(SHA256.HashData(_bytes)).ToLowerInvariant();

    [Fact] void should_match_the_pinned_checksum() => _sha256.ShouldEqual(PinnedSha256);
    [Fact] void should_hold_every_vector() => _vectors.Count.ShouldEqual(McpVectorRunner.VectorCount);
}
