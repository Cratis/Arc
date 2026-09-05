// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.ModelBound;

namespace Cratis.Arc.Chronicle.ReadModels.for_ModelBoundReadModelRoots;

public class when_constructor_parameters_shadow_properties : Specification
{
    Type[] _candidates;
    IEnumerable<Type> _roots;

    void Establish() => _candidates = [typeof(Parent), typeof(PropertyOnlyChild), typeof(ParameterChild), typeof(PropertyDetails)];

    void Because() => _roots = ModelBoundReadModelRoots.Discover(_candidates).ToArray();

    [Fact] void should_ignore_children_from_on_a_same_named_property() => _roots.ShouldContain(typeof(PropertyOnlyChild));
    [Fact] void should_use_children_from_on_the_constructor_parameter() => _roots.ShouldNotContain(typeof(ParameterChild));
    [Fact] void should_ignore_a_complex_property_shadowed_case_insensitively() => _roots.ShouldContain(typeof(PropertyDetails));
    [Fact] void should_keep_the_parent() => _roots.ShouldContain(typeof(Parent));

    class Parent(
        IEnumerable<PropertyOnlyChild> children,
        [ChildrenFrom<ChildAdded>(key: nameof(ChildAdded.Id))] IEnumerable<ParameterChild> selected,
        string details)
    {
        [ChildrenFrom<ChildAdded>(key: nameof(ChildAdded.Id))]
        public IEnumerable<PropertyOnlyChild> Children { get; } = children;

        public IEnumerable<ParameterChild> Selected { get; } = selected;

        public PropertyDetails Details { get; } = new(details);
    }

    record PropertyOnlyChild(string Name);
    record ParameterChild(string Name);
    record PropertyDetails(string Name);
    record ChildAdded(Guid Id);
}
