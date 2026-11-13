// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

/// <summary>
/// An audience the language cannot name is reported and the gate is left out rather than written wrongly.
/// </summary>
public class from_an_audience_that_is_not_an_identifier : a_generated_document
{
    void Because() => Generate((AvailabilitySources.Path, AvailabilitySources.With("""
        [Command]
        [AvailableFor("Plus tier")]
        public record ResetScore(VesselId Vessel)
        {
            public ScoreReset Handle() => new(Vessel);
        }
        """)));

    [Fact] void should_warn() => Result.Diagnostics.Count(_ => _.Code == "SP0080" && _.Message.Contains("Plus tier")).ShouldEqual(1);
    [Fact] void should_state_no_gate() => Result.Source.ShouldNotContain("available ");
    [Fact] void should_declare_no_audience() => Result.Source.ShouldNotContain("audience ");
    [Fact] void should_compile_round_trip_and_bind() => AssertDocument();
}
