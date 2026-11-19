// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

/// <summary>
/// An [AvailableFor] gate is stated as an available line over declared audiences.
/// </summary>
public class from_a_command_gated_to_audiences : a_generated_document
{
    void Because() => Generate((AvailabilitySources.Path, AvailabilitySources.With("""
        [Command]
        [AvailableFor("Plus", "Preview")]
        public record ResetScore(VesselId Vessel)
        {
            public ScoreReset Handle() => new(Vessel);
        }
        """)));

    [Fact] void should_declare_the_first_audience() => Result.Source.ShouldContain("audience Plus");
    [Fact] void should_declare_the_second_audience() => Result.Source.ShouldContain("audience Preview");
    [Fact] void should_state_the_gate_over_either_audience() => Result.Source.ShouldContain("available for Plus or Preview");
    [Fact] void should_not_warn() => Result.Diagnostics.Where(_ => _.Code == "SP0080" || _.Code == "SP0081").ShouldBeEmpty();
    [Fact] void should_compile_round_trip_and_bind() => AssertDocument();
}
