// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_stating_concept_fixture_conversions;

public class with_a_forwarding_conversion : given.a_concept_fixture
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_state_the_unchanged_value(bool collection)
    {
        GenerateFixture(collection);
        AssertStated(collection);
    }
}
