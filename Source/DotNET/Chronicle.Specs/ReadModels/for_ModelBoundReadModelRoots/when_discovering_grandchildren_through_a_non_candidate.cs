// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.ModelBound;

namespace Cratis.Arc.Chronicle.ReadModels.for_ModelBoundReadModelRoots;

public class when_discovering_grandchildren_through_a_non_candidate : Specification
{
    Type[] _candidates;
    IEnumerable<Type> _roots;

    void Establish() => _candidates = [typeof(Parent), typeof(Grandchild), typeof(Details)];

    void Because() => _roots = ModelBoundReadModelRoots.Discover(_candidates).ToArray();

    [Fact] void should_traverse_the_child_even_when_it_is_not_a_candidate() => _roots.ShouldContainOnly(typeof(Parent));

    record Parent([ChildrenFrom<ChildAdded>(key: nameof(ChildAdded.Id))] IEnumerable<Child> Children);
    record Child([ChildrenFrom<ChildAdded>(key: nameof(ChildAdded.Id))] IEnumerable<Grandchild> Children);
    record Grandchild(Details Details);
    record Details(string Name);
    record ChildAdded(Guid Id);
}
