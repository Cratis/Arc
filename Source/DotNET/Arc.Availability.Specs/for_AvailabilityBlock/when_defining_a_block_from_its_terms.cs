// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability.for_AvailabilityBlock;

public class when_defining_a_block_from_its_terms : Specification
{
    AvailabilityDefinition _definition;
    IReadOnlyList<AvailabilityAlternative> _alternatives;

    void Because()
    {
        _definition = new given.TypedDeclarations.EitherOrBlock().Define(_ => throw new InvalidOperationException());
        _alternatives = _definition.Expression.Alternatives();
    }

    [Fact] void should_name_the_block_after_its_type() => _definition.Name.ShouldEqual("EitherOrBlock");
    [Fact] void should_give_and_precedence_over_or() => _alternatives.Count.ShouldEqual(2);
    [Fact] void should_expand_the_first_alternative_to_the_audience() => _alternatives[0].Audiences.Select(_ => _.Value).ShouldEqual(["Plus"]);
    [Fact] void should_expand_the_second_alternative_to_an_audience_that_excludes_another() => _alternatives[1].Audiences.Select(_ => _.Value).ShouldEqual(["Preview"]);
    [Fact] void should_carry_the_excluded_audience() => _alternatives[1].NotHeld.Select(_ => _.Value).ShouldEqual(["Incident"]);
}
