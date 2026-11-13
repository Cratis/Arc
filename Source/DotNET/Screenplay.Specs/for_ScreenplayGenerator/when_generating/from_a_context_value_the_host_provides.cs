// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

/// <summary>
/// A value the host supplies has no type the code states, so it is reported and left out.
/// </summary>
public class from_a_context_value_the_host_provides : a_generated_document
{
    const string Registration = """
        public static class Composition
        {
            public static void Configure(Cratis.Choreography.Availability.Context.ContextValueCatalog catalog) => catalog.Declare(
                new Cratis.Choreography.Availability.Context.ContextValueDeclaration(
                    "region",
                    new Cratis.Choreography.Availability.Context.Providers.HostContextValueProvider(null!, "region"),
                    Cratis.Choreography.Availability.Context.ContextScope.Application));
        }
        """;

    void Because() => Generate(("Library/Hosts.cs", AvailabilitySources.HostLibraries), (AvailabilitySources.Path, AvailabilitySources.With(Registration)));

    [Fact] void should_warn() => Result.Diagnostics.Count(_ => _.Code == "SP0080" && _.Message.Contains("region")).ShouldEqual(1);
    [Fact] void should_provide_nothing() => Result.Source.ShouldNotContain("provide ");
}
