// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

/// <summary>
/// A gate composed in code cannot be read, so it is reported instead of being dropped silently.
/// </summary>
public class from_a_gate_registered_in_code : a_generated_document
{
    const string Registration = """
        [Command]
        public record ResetScore(VesselId Vessel)
        {
            public ScoreReset Handle() => new(Vessel);
        }

        public static class Composition
        {
            public static void Configure(ArcAvailabilityOptions options) =>
                options.GateNamespace("Library.Fleet", "Plus");
        }
        """;

    void Because() => Generate(("Library/Hosts.cs", AvailabilitySources.HostLibraries), (AvailabilitySources.Path, AvailabilitySources.With(Registration)));

    [Fact] void should_warn() => Result.Diagnostics.Count(_ => _.Code == "SP0080" && _.Message.Contains("GateNamespace")).ShouldEqual(1);
    [Fact] void should_state_no_gate() => Result.Source.ShouldNotContain("available ");
}
