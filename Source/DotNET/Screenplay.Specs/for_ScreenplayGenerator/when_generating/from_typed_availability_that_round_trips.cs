// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

/// <summary>
/// The Screenplay example, declared as types: audiences, an availability block, a scope marker, a gated command and a provided read model.
/// </summary>
public class from_typed_availability_that_round_trips : a_generated_document
{
    const string Declarations = """
        /// <summary>Customers on the Plus commercial package</summary>
        public class Plus : Audience;

        /// <summary>Tenants taking part in the preview</summary>
        public class Preview : Audience;

        /// <summary>Sold with Plus, once the vessel has reported</summary>
        public class DataQuality : AvailabilityBlock
        {
            protected override IReadOnlyList<AvailabilityTerm> Lines =>
            [
                For<Plus>(),
                When.On<Vessel>(_ => _.HasReported).IsTrue()
            ];
        }

        [Available<DataQuality>]
        public class Feature : AvailabilityScope;

        [ReadModel]
        [FromEvent<VesselRegistered>]
        [ProvideContext<Vessel>(nameof(Vessel.VesselByKey), "arguments.vesselId", Required = false)]
        public record Vessel([Key] Guid VesselId, string Name, bool HasReported)
        {
            public static Vessel? VesselByKey(Guid vesselId) => null;
        }

        [Command]
        [AvailableFor<Plus, Preview>]
        public record ResetScore(VesselId Vessel)
        {
            public ScoreReset Handle() => new(Vessel);
        }
        """;

    void Because() => Generate(
        ("Library/Hosts.cs", AvailabilitySources.TypedLibraries),
        (AvailabilitySources.Path, AvailabilitySources.With(Declarations)));

    [Fact] void should_declare_the_first_audience() => Result.Source.ShouldContain("audience Plus");
    [Fact] void should_declare_the_second_audience() => Result.Source.ShouldContain("audience Preview");
    [Fact] void should_describe_the_audience_from_its_documentation() => Result.Source.ShouldContain("Customers on the Plus commercial package");
    [Fact] void should_declare_the_availability_with_its_rules() => Result.Source.ShouldContain("availability DataQuality");
    [Fact] void should_state_the_audience_of_the_availability() => Result.Source.ShouldContain("for Plus");
    [Fact] void should_state_the_condition_of_the_availability() => Result.Source.ShouldContain("when $context.vessel.hasReported == true");
    [Fact] void should_gate_the_feature_with_the_availability() => Result.Source.ShouldContain("available with DataQuality");
    [Fact] void should_gate_the_command_to_either_audience() => Result.Source.ShouldContain("available for Plus or Preview");
    [Fact] void should_provide_the_read_model() => Result.Source.ShouldContain("provide vessel Vessel optional from query VesselByKey by $context.arguments.vesselId");
    [Fact] void should_not_warn() => Result.Diagnostics.Where(_ => _.Code == "SP0080" || _.Code == "SP0081").ShouldBeEmpty();
    [Fact] void should_compile_round_trip_and_bind() => AssertDocument();
}
