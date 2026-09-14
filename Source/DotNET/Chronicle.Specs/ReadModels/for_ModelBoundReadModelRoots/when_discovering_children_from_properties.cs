// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.ModelBound;

namespace Cratis.Arc.Chronicle.ReadModels.for_ModelBoundReadModelRoots;

public class when_discovering_children_from_properties : Specification
{
    Type[] _candidates;
    IEnumerable<Type> _roots;

    void Establish() => _candidates = [typeof(Parent), typeof(ArrayChild), typeof(ListChild), typeof(EnumerableChild)];

    void Because() => _roots = ModelBoundReadModelRoots.Discover(_candidates).ToArray();

    [Fact] void should_keep_only_the_parent() => _roots.ShouldContainOnly(typeof(Parent));

    record Parent
    {
        [ChildrenFrom<ChildAdded>(key: nameof(ChildAdded.Id))]
        public ArrayChild[] ArrayChildren { get; init; }

        [ChildrenFrom<ChildAdded>(key: nameof(ChildAdded.Id))]
        public List<ListChild> ListChildren { get; init; }

        [ChildrenFrom<ChildAdded>(key: nameof(ChildAdded.Id))]
        public IEnumerable<EnumerableChild> EnumerableChildren { get; init; }
    }

    record ArrayChild(string Name);
    record ListChild(string Name);
    record EnumerableChild(string Name);
    record ChildAdded(Guid Id);
}
