// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

/// <summary>
/// A scope marker in a namespace gates the module, feature or slice of that namespace.
/// </summary>
public class from_a_scope_marker_gating_a_feature : a_generated_document
{
    const string Declarations = """
        public class Plus : Audience;

        [AvailableFor<Plus>]
        public class Feature : AvailabilityScope;

        [Command]
        public record ResetScore(VesselId Vessel)
        {
            public ScoreReset Handle() => new(Vessel);
        }
        """;

    void Because() => Generate(
        ("Library/Hosts.cs", AvailabilitySources.TypedLibraries),
        (AvailabilitySources.Path, AvailabilitySources.With(Declarations)));

    [Fact] void should_declare_the_audience() => Result.Source.ShouldContain("audience Plus");
    [Fact] void should_gate_what_the_namespace_names() => Result.Source.ShouldContain("available for Plus");
    [Fact] void should_not_gate_the_command_itself() => Result.Source.Split("available for Plus").Length.ShouldEqual(2);
    [Fact] void should_not_warn() => Result.Diagnostics.Where(_ => _.Code == "SP0080" || _.Code == "SP0081").ShouldBeEmpty();
    [Fact] void should_compile_round_trip_and_bind() => AssertDocument();
}
