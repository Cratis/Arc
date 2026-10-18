// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.ReadModels.for_ProtectedReadModelKeyTraversal;

public class when_a_key_graph_contains_cycles : given.a_traversal
{
    static when_a_key_graph_contains_cycles() => ProtectedReadModelKeyAccessors.Register<Node>(
        new(nameof(Node.Part), node => node.Part), new(nameof(Node.Next), node => node.Next));
    bool _valid;
    bool _invalid;

    void Establish() => _options.Strict = true;
    void Because()
    {
        var first = new Node { Part = "first" };
        var second = new Node { Part = "second", Next = first };
        first.Next = second;
        _valid = _traversal.IsValid(first, "wire");
        second.Part = "*";
        _invalid = _traversal.IsValid(first, "wire");
    }

    [Fact] void should_terminate_a_reference_cycle() => _valid.ShouldBeTrue();
    [Fact] void should_still_inspect_each_distinct_instance() => _invalid.ShouldBeFalse();

    sealed class Node
    {
        public string Part { get; set; } = string.Empty;
        public Node Next { get; set; } = null!;
    }
}
