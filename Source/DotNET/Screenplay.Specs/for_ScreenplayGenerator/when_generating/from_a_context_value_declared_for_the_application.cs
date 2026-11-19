// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

/// <summary>
/// A value declared at the application is stated beneath no module, at the top of the document.
/// </summary>
public class from_a_context_value_declared_for_the_application : a_generated_document
{
    const string Registration = """

        public static class Composition
        {
            public static void Configure(Cratis.Choreography.Availability.Context.ContextValueCatalog catalog) => catalog.Declare(
                new Cratis.Choreography.Availability.Context.ContextValueDeclaration(
                    "vessel",
                    new Cratis.Choreography.Availability.Context.Providers.ReadModelContextValueProvider(null!, "VesselByKey", "arguments.vesselId"),
                    Cratis.Choreography.Availability.Context.ContextScope.Application,
                    false));
        }
        """;

    void Because() => Generate(("Library/Hosts.cs", AvailabilitySources.HostLibraries), (AvailabilitySources.Path, AvailabilitySources.With(AvailabilitySources.Vessels + Registration)));

    [Fact] void should_provide_the_value_at_the_application() => Result.Source.ShouldContain("provide vessel Vessel optional from query VesselByKey by $context.arguments.vesselId");
    [Fact] void should_not_warn() => Result.Diagnostics.Where(_ => _.Code == "SP0080").ShouldBeEmpty();
    [Fact] void should_compile_round_trip_and_bind() => AssertDocument();
}
