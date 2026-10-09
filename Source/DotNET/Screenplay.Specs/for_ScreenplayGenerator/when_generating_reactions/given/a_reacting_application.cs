// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions.given;

/// <summary>
/// An application registering authors, with a slice reacting to the registration.
/// </summary>
public class a_reacting_application : a_generated_document
{
    /// <summary>
    /// The slice registering an author, which the reactions react to.
    /// </summary>
    protected const string Registration = """
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.Keys;

        namespace Library.Authors.Registration;

        [EventType]
        public record AuthorRegistered(string Name, string Country);

        [Command]
        public record RegisterAuthor([Key] string Id, string Name, string Country)
        {
            public AuthorRegistered Handle() => new(Name, Country);
        }
        """;

    /// <summary>
    /// The usings and namespace every reacting slice starts with.
    /// </summary>
    protected const string Welcoming = """
        using System;
        using System.Collections.Generic;
        using System.Threading.Tasks;
        using Cratis.Arc.Chronicle.Reactors;
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.Reactors;
        using Library.Authors.Registration;

        namespace Library.Authors.Welcoming;

        [EventType]
        public record AuthorWelcomed(string Name, string Greeting);

        [EventType]
        public record AuthorFiled(string Country);

        [EventType]
        public record WelcomeSent(string Name);

        [EventType]
        public record AuthorGreeted(string Name, DateTimeOffset GreetedAt);

        [Command]
        public record SendWelcome(string Name, string Country)
        {
            public WelcomeSent Handle() => new(Name);
        }

        """;

    /// <summary>
    /// Gets what was reported about scenarios left out.
    /// </summary>
    protected IEnumerable<ScreenplayDiagnostic> ScenarioReports => Result.Diagnostics.Where(_ =>
        string.Equals(_.Code, ScreenplayDiagnosticCodes.ScenarioWithoutCounterpart, StringComparison.Ordinal) ||
        string.Equals(_.Code, ScreenplayDiagnosticCodes.UnreadableSpecification, StringComparison.Ordinal));

    /// <summary>
    /// Generates the document of the application with a reactor and the sources beside it.
    /// </summary>
    /// <param name="reactor">The reactor declaration.</param>
    /// <param name="sources">The other sources.</param>
    protected void GenerateWith(string reactor, params (string Path, string Text)[] sources) => Generate(
        [
            ("Library/Authors/Registration/Registration.cs", Registration),
            ("Library/Authors/Welcoming/Welcoming.cs", Welcoming + reactor),
            (IntegrationTesting.Path, IntegrationTesting.Source),
            .. sources
        ]);
}
