// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

/// <summary>
/// An [AvailableWith] gate on a read model gates its query, and the availability is declared without the rules the host defines.
/// </summary>
public class from_a_read_model_gated_to_an_availability : a_generated_document
{
    void Because() => Generate((AvailabilitySources.Path, AvailabilitySources.With("""
        [ReadModel]
        [AvailableWith("DataQuality")]
        public record QualityScores(Guid VesselId, int Score)
        {
            public static IEnumerable<QualityScores> All() => [];
        }
        """)));

    [Fact] void should_declare_the_availability() => Result.Source.ShouldContain("availability DataQuality");
    [Fact] void should_gate_the_query() => Result.Source.ShouldContain("available with DataQuality");
    [Fact] void should_warn_that_the_rules_are_in_the_host() => Result.Diagnostics.Count(_ => _.Code == "SP0081").ShouldEqual(1);
    [Fact] void should_compile_round_trip_and_bind() => AssertDocument();
}
