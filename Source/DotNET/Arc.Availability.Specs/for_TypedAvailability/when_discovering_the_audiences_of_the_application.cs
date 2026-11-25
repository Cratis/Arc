// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Types;

namespace Cratis.Arc.Availability.for_TypedAvailability;

public class when_discovering_the_audiences_of_the_application : Specification
{
    IReadOnlyList<AudienceDeclaration> _declarations;

    void Because()
    {
        var types = Substitute.For<ITypes>();
        types.All.Returns([typeof(given.TypedDeclarations.Preview), typeof(given.TypedDeclarations.Plus), typeof(given.TypedDeclarations.Foundation), typeof(Audience)]);
        _declarations = TypedAvailability.Audiences(types);
    }

    [Fact] void should_find_the_audience_types_by_name_in_order() => _declarations.Select(_ => _.Name.Value).ShouldEqual(["Plus", "Preview"]);
    [Fact] void should_not_find_the_base_type_or_other_types() => _declarations.Count.ShouldEqual(2);
}
