// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_stating_concept_fixture_conversions;

public class with_a_framework_event_source_id : given.a_referenced_concept_fixture
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_state_the_frameworks_metadata_only_backing_conversion(bool collection)
    {
        Result = new ScreenplayGenerator().Generate(Application(null, collection, framework: true), new());
        AssertStated(collection);
    }
}
