// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions.given;

/// <summary>
/// An application observing facts without assuming that a known command produces their routed occurrences.
/// </summary>
public class a_filtered_application : a_reacting_application
{
    const string ObservedEvent = """
        using Cratis.Chronicle.Events;
        namespace Library.Authors.Registration;
        [EventType] public record AuthorRegistered(string Name, string Country);
        """;

    /// <summary>
    /// Generates a filter fixture with no guessed producer route.
    /// </summary>
    /// <param name="reactor">The reactor and actual source definitions.</param>
    /// <param name="sources">Additional specification source files.</param>
    protected void GenerateFilteredWith(string reactor, params (string Path, string Text)[] sources) => Generate(
        [
            ("Library/Authors/Registration/Registration.cs", ObservedEvent),
            ("Library/Authors/Welcoming/Welcoming.cs", Welcoming + reactor),
            (IntegrationTesting.Path, IntegrationTesting.Source),
            .. sources
        ]);
}
