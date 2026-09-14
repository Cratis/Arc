// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.ModelBound;

namespace Cratis.Arc.Chronicle.ReadModels.for_ModelBoundReadModelRoots;

public class when_selecting_the_constructor_with_most_parameters : Specification
{
    Type[] _candidates;
    IEnumerable<Type> _roots;

    void Establish() => _candidates = [typeof(Parent), typeof(SelectedChild), typeof(ShortConstructorChild), typeof(PrivateConstructorChild)];

    void Because() => _roots = ModelBoundReadModelRoots.Discover(_candidates).ToArray();

    [Fact] void should_traverse_only_the_longest_public_constructor() => _roots.ShouldContainOnly(typeof(Parent), typeof(ShortConstructorChild), typeof(PrivateConstructorChild));

    class Parent([ChildrenFrom<ChildAdded>(key: nameof(ChildAdded.Id))] IEnumerable<SelectedChild> children, string name)
    {
        public Parent([ChildrenFrom<ChildAdded>(key: nameof(ChildAdded.Id))] IEnumerable<ShortConstructorChild> children)
            : this([], string.Empty)
        {
        }

        Parent([ChildrenFrom<ChildAdded>(key: nameof(ChildAdded.Id))] IEnumerable<PrivateConstructorChild> children, string name, int count)
            : this([], name)
        {
        }
    }

    record SelectedChild(string Name);
    record ShortConstructorChild(string Name);
    record PrivateConstructorChild(string Name);
    record ChildAdded(Guid Id);
}
