// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Mcp.for_McpVectors.given;

namespace Cratis.Arc.Mcp.for_McpVectors;

public class when_applying_the_pinned_vectors : the_pinned_vectors
{
    List<string> _mismatches;
    int[] _classified;
    int[] _overlap;

    async Task Because()
    {
        _classified = [.. McpVectorRunner.Applicable.Concat(McpVectorRunner.NotApplicable.Keys).Order()];
        _overlap = [.. McpVectorRunner.Applicable.Intersect(McpVectorRunner.NotApplicable.Keys)];
        _mismatches = [];
        foreach (var index in McpVectorRunner.Applicable)
        {
            _mismatches.AddRange(await McpVectorRunner.Check(index, _vectors[index]!));
        }
    }

    [Fact] void should_classify_every_vector_exactly_once() => _classified.SequenceEqual(Enumerable.Range(0, _vectors.Count)).ShouldBeTrue();
    [Fact] void should_not_classify_a_vector_twice() => _overlap.ShouldBeEmpty();
    [Fact] void should_give_a_reason_for_every_vector_it_does_not_apply() => McpVectorRunner.NotApplicable.Values.All(_ => !string.IsNullOrWhiteSpace(_)).ShouldBeTrue();
    [Fact] void should_satisfy_every_applicable_vector() => string.Join(Environment.NewLine, _mismatches).ShouldEqual(string.Empty);
}
