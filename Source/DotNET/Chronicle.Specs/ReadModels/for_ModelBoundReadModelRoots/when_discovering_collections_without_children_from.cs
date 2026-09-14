// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.ReadModels.for_ModelBoundReadModelRoots;

public class when_discovering_collections_without_children_from : Specification
{
    Type[] _candidates;
    IEnumerable<Type> _roots;

    void Establish() => _candidates = [typeof(Parent), typeof(EnumerableChild), typeof(ArrayChild), typeof(ListChild)];

    void Because() => _roots = ModelBoundReadModelRoots.Discover(_candidates).ToArray();

    [Fact] void should_not_unwrap_an_unannotated_enumerable_interface() => _roots.ShouldContain(typeof(EnumerableChild));
    [Fact] void should_not_unwrap_an_unannotated_array() => _roots.ShouldContain(typeof(ArrayChild));
    [Fact] void should_traverse_a_concrete_list_as_a_complex_object_through_its_indexer() => _roots.ShouldNotContain(typeof(ListChild));
    [Fact] void should_keep_the_parent() => _roots.ShouldContain(typeof(Parent));

    record Parent(IEnumerable<EnumerableChild> EnumerableChildren, ArrayChild[] ArrayChildren, List<ListChild> ListChildren);
    record EnumerableChild(string Name);
    record ArrayChild(string Name);
    record ListChild(string Name);
}
