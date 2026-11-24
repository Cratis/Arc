// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

/// <summary>
/// A block whose lines use for, not, with and when, and combine them with the operators the language has.
/// </summary>
public class from_an_availability_block_using_every_operator : a_generated_document
{
    const string Declarations = """
        public class Plus : Audience;

        public class Preview : Audience;

        public class Incident : Audience;

        public enum Package
        {
            Basic,
            Standard
        }

        public class Foundation : AvailabilityBlock
        {
            protected override IReadOnlyList<AvailabilityTerm> Lines => [For<Plus>()];
        }

        public class HasPaidInvoice : IAvailabilityCondition;

        public class Everything : AvailabilityBlock
        {
            protected override IReadOnlyList<AvailabilityTerm> Lines =>
            [
                For<Plus>() | For<Preview>(),
                Not<Incident>(),
                For<Plus>() & When.On<Vessel>(_ => _.CommercialPackage).IsNot(Package.Basic),
                With<Foundation>(),
                When.On<Vessel>(_ => _.Score).IsGreaterThan(3) | When.On<Vessel>(_ => _.HasReported).IsTrue(),
                When.On<Vessel>(_ => _.CommercialPackage).In(Package.Basic, Package.Standard),
                When.Condition<HasPaidInvoice>()
            ];
        }

        [ReadModel]
        [FromEvent<VesselRegistered>]
        [ProvideContext<Vessel>(nameof(Vessel.VesselByKey), "arguments.vesselId")]
        public record Vessel([Key] Guid VesselId, string Name, bool HasReported, int Score, Package CommercialPackage)
        {
            public static Vessel? VesselByKey(Guid vesselId) => null;
        }

        [Command]
        [Available<Everything>]
        [AvailableWhen<HasPaidInvoice>]
        public record ResetScore(VesselId Vessel)
        {
            public ScoreReset Handle() => new(Vessel);
        }
        """;

    void Because() => Generate(
        ("Library/Hosts.cs", AvailabilitySources.TypedLibraries),
        (AvailabilitySources.Path, AvailabilitySources.With(Declarations)));

    [Fact] void should_state_for_over_either_audience() => Result.Source.ShouldContain("for Plus or Preview");
    [Fact] void should_state_not_over_an_audience() => Result.Source.ShouldContain("for not Incident");
    [Fact] void should_split_a_line_that_mixes_forms_into_lines() => Result.Source.ShouldContain("when $context.vessel.commercialPackage != \"Basic\"");
    [Fact] void should_state_with_over_a_block() => Result.Source.ShouldContain("with Foundation");
    [Fact] void should_state_when_with_its_own_or() => Result.Source.ShouldContain("when $context.vessel.score > 3 or $context.vessel.hasReported == true");
    [Fact] void should_state_a_set_as_or_over_equals() => Result.Source.ShouldContain("when $context.vessel.commercialPackage == \"Basic\" or $context.vessel.commercialPackage == \"Standard\"");
    [Fact] void should_state_a_condition_class_as_an_opaque_condition() => Result.Source.ShouldContain("when $context.hasPaidInvoice == true");
    [Fact] void should_provide_what_the_opaque_condition_reads() => Result.Source.ShouldContain("provide hasPaidInvoice Bool from host");
    [Fact] void should_gate_the_command_with_the_block() => Result.Source.ShouldContain("available with Everything");
    [Fact] void should_not_warn() => Result.Diagnostics.Where(_ => _.Code == "SP0080" || _.Code == "SP0081").ShouldBeEmpty();
    [Fact] void should_compile_round_trip_and_bind() => AssertDocument();
}
