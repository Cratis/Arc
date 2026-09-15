// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.ModelBound;

namespace Cratis.Arc.Chronicle.ReadModels.for_ModelBoundReadModelRoots;

public class when_discovering_recursive_children : Specification
{
    Type[] _candidates;
    IEnumerable<Type> _roots;

    void Establish() => _candidates = [typeof(Node), typeof(Details)];

    void Because() => _roots = ModelBoundReadModelRoots.Discover(_candidates).ToArray();

    [Fact] void should_retain_the_self_referencing_root_and_exclude_its_subobject() => _roots.ShouldContainOnly(typeof(Node));

    record Node(Details Details, [ChildrenFrom<NodeAdded>(key: nameof(NodeAdded.Id))] IEnumerable<Node> Children);
    record Details(string Name);
    record NodeAdded(Guid Id);
}
