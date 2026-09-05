// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.ModelBound;

namespace Cratis.Arc.Chronicle.ReadModels.for_ModelBoundReadModelRoots;

public class when_discovering_children_from_constructor_parameters : Specification
{
    Type[] _candidates;
    IEnumerable<Type> _roots;

    void Establish() => _candidates = [typeof(Parent), typeof(Child), typeof(Unrelated)];

    void Because() => _roots = ModelBoundReadModelRoots.Discover(_candidates).ToArray();

    [Fact] void should_keep_only_the_parent_and_unrelated_root() => _roots.ShouldContainOnly(typeof(Parent), typeof(Unrelated));

    record Parent([ChildrenFrom<ChildAdded>(key: nameof(ChildAdded.Id))] IEnumerable<Child> Children);
    record Child(string Name);
    record Unrelated(string Name);
    record ChildAdded(Guid Id);
}
