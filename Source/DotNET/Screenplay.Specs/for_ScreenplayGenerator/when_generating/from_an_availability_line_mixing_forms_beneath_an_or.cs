// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

/// <summary>
/// A line holds one form, so a for and a when beneath an or cannot be stated and is reported rather than guessed.
/// </summary>
public class from_an_availability_line_mixing_forms_beneath_an_or : a_generated_document
{
    const string Declarations = """
        public class Plus : Audience;

        public class Mixed : AvailabilityBlock
        {
            protected override IReadOnlyList<AvailabilityTerm> Lines =>
            [
                For<Plus>() | When.On<Vessel>(_ => _.HasReported).IsTrue()
            ];
        }

        [ReadModel]
        [FromEvent<VesselRegistered>]
        [ProvideContext<Vessel>(nameof(Vessel.VesselByKey), "arguments.vesselId")]
        public record Vessel([Key] Guid VesselId, string Name, bool HasReported)
        {
            public static Vessel? VesselByKey(Guid vesselId) => null;
        }

        [Command]
        [Available<Mixed>]
        public record ResetScore(VesselId Vessel)
        {
            public ScoreReset Handle() => new(Vessel);
        }
        """;

    void Because() => Generate(
        ("Library/Hosts.cs", AvailabilitySources.TypedLibraries),
        (AvailabilitySources.Path, AvailabilitySources.With(Declarations)));

    [Fact] void should_warn_that_the_line_cannot_be_stated() => Result.Diagnostics.Count(_ => _.Code == "SP0080" && _.Message.Contains("mixes forms")).ShouldEqual(1);
    [Fact] void should_still_declare_the_availability() => Result.Source.ShouldContain("availability Mixed");
    [Fact] void should_not_state_the_mixed_line() => Result.Source.ShouldNotContain("hasReported == true");
}
