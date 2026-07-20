// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Validation.for_ModelGraphValidator.when_traversing;

/// <summary>
/// Some types hold a back-reference from every child to its parent — <c>JsonNode</c> is the one that reaches a
/// command or query in practice. Walking such a graph without a guard recurses until the stack overflows, which
/// takes the process down rather than failing a request.
/// </summary>
public class with_a_cycle_in_the_graph : given.a_model_graph_validator
{
    Exception _error;
    Parent _parent;

    void Establish()
    {
        _parent = new Parent();
        _parent.Child = new Child { Parent = _parent };
    }

    async Task Because() => _error = await Catch.Exception(() => _validator.Validate(new ModelGraphValidationRequest(_parent)));

    [Fact] void should_not_fail() => _error.ShouldBeNull();
    [Fact] void should_have_visited_the_parent() => _typesAskedFor.ShouldContain(typeof(Parent));
    [Fact] void should_have_visited_the_child() => _typesAskedFor.ShouldContain(typeof(Child));
    [Fact] void should_only_consider_the_parent_once() => _typesAskedFor.Count(_ => _ == typeof(Parent)).ShouldEqual(1);

    class Parent
    {
        public Child? Child { get; set; }
    }

    class Child
    {
        public Parent? Parent { get; set; }
    }
}
