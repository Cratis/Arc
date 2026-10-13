// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;
using Cratis.Screenplay.Semantics;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_uuidv7_generation_strategy : a_generation_strategy
{
    [Theory]
    [InlineData("GenerateValue.UuidV7()")]
#if NET9_0_OR_GREATER
    [InlineData("Guid.CreateVersion7()")]
#endif
    public void should_bind_and_round_trip_the_time_ordered_strategy(string call)
    {
        Generate((Analyzed.SlicePath, Source(call, "Guid")));
        AssertStrategy("generate uuidv7");
        Bound.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V15);
    }
}
