// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Validation.for_ModelGraphValidator.when_traversing;

/// <summary>
/// Nothing in the graph has a validator, yet the traversal must still descend — a validator can be declared for a
/// type anywhere below the root, and refusing to look is what made query validation silently weaker than command
/// validation.
/// </summary>
public class with_a_nested_model : given.a_model_graph_validator
{
    void Because() => _validator.Validate(new ModelGraphValidationRequest(new Root(new Branch(new Leaf())))).GetAwaiter().GetResult();

    [Fact] void should_consider_the_root() => _typesAskedFor.ShouldContain(typeof(Root));
    [Fact] void should_descend_one_level() => _typesAskedFor.ShouldContain(typeof(Branch));
    [Fact] void should_descend_all_the_way_down() => _typesAskedFor.ShouldContain(typeof(Leaf));

    record Leaf;
    record Branch(Leaf Leaf);
    record Root(Branch Branch);
}
