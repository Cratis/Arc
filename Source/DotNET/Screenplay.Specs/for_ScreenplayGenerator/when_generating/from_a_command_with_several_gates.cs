// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

/// <summary>
/// Every gate a command carries is a line of its own, so all of them have to hold.
/// </summary>
public class from_a_command_with_several_gates : a_generated_document
{
    void Because() => Generate((AvailabilitySources.Path, AvailabilitySources.With("""
        [Command]
        [AvailableFor("Plus")]
        [AvailableFor("Preview")]
        [AvailableWith("DataQuality")]
        public record ResetScore(VesselId Vessel)
        {
            public ScoreReset Handle() => new(Vessel);
        }
        """)));

    [Fact] void should_state_the_first_audience_gate() => Result.Source.ShouldContain("available for Plus\n");
    [Fact] void should_state_the_second_audience_gate() => Result.Source.ShouldContain("available for Preview\n");
    [Fact] void should_state_the_availability_gate() => Result.Source.ShouldContain("available with DataQuality");
    [Fact] void should_compile_round_trip_and_bind() => AssertDocument();
}
