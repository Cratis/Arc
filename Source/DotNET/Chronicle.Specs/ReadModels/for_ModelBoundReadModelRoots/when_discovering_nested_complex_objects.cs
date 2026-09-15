// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.ReadModels.for_ModelBoundReadModelRoots;

public class when_discovering_nested_complex_objects : Specification
{
    Type[] _candidates;
    IEnumerable<Type> _roots;

    void Establish() => _candidates = [typeof(Parent), typeof(Details), typeof(Address), typeof(Location)];

    void Because() => _roots = ModelBoundReadModelRoots.Discover(_candidates).ToArray();

    [Fact] void should_remove_unannotated_constructor_and_property_subobjects() => _roots.ShouldContainOnly(typeof(Parent));

    record Parent(Details Details);
    record Details
    {
        public Address Address { get; init; }
    }

    record Address(Location Location);
    record Location(string Name);
}
