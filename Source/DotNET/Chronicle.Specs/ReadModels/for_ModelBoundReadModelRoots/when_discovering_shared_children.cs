// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.ModelBound;

namespace Cratis.Arc.Chronicle.ReadModels.for_ModelBoundReadModelRoots;

public class when_discovering_shared_children : Specification
{
    Type[] _candidates;
    IEnumerable<Type> _roots;

    void Establish() => _candidates = [typeof(Child), typeof(FirstParent), typeof(SecondParent)];

    void Because() => _roots = ModelBoundReadModelRoots.Discover(_candidates).ToArray();

    [Fact] void should_keep_both_parents_but_not_the_shared_child() => _roots.ShouldContainOnly(typeof(FirstParent), typeof(SecondParent));

    record FirstParent([ChildrenFrom<ChildAdded>(key: nameof(ChildAdded.Id))] Child[] Children);
    record SecondParent([ChildrenFrom<ChildAdded>(key: nameof(ChildAdded.Id))] List<Child> Children);
    record Child(string Name);
    record ChildAdded(Guid Id);
}
