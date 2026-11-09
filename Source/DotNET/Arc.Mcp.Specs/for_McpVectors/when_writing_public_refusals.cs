// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Mcp.for_McpVectors;

public class when_writing_public_refusals : given.the_pinned_vectors
{
    readonly List<string> _mismatches = [];

    async Task Because()
    {
        foreach (var index in new[] { 0, 4, 10, 18, 19, 25, 26, 27 })
        {
            _mismatches.AddRange(await given.McpVectorRunner.Check(index, _vectors[index]!, publicRefusal: true));
        }
    }

    [Fact] void should_match_the_pinned_refusals_and_existing_body_bytes() => _mismatches.ShouldBeEmpty();
}
