// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

/// <summary>
/// A value declared at a scope that names nothing of the generated model is reported instead of being placed somewhere else.
/// </summary>
public class from_a_context_value_at_an_unknown_scope : a_generated_document
{
    const string Registration = """

        public static class Composition
        {
            public static void Configure(Cratis.Choreography.Availability.Context.ContextValueCatalog catalog) => catalog.Declare(
                new Cratis.Choreography.Availability.Context.ContextValueDeclaration(
                    "vessel",
                    new Cratis.Choreography.Availability.Context.Providers.ReadModelContextValueProvider(null!, "VesselByKey", "arguments.vesselId"),
                    Cratis.Choreography.Availability.Context.ContextScope.Of("Nowhere"),
                    false));
        }
        """;

    void Because() => Generate(("Library/Hosts.cs", AvailabilitySources.HostLibraries), (AvailabilitySources.Path, AvailabilitySources.With(AvailabilitySources.Vessels + Registration)));

    [Fact] void should_warn() => Result.Diagnostics.Count(_ => _.Code == "SP0080" && _.Message.Contains("Nowhere")).ShouldEqual(1);
    [Fact] void should_provide_nothing() => Result.Source.ShouldNotContain("provide ");
    [Fact] void should_compile_round_trip_and_bind() => AssertDocument();
}
