// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

/// <summary>
/// Declares the explicit generation contract and the scenario pin surface used by recovery specs.
/// </summary>
public static class GeneratedValueSources
{
    const string Concept = """
        public record AuthorId(Guid Value) : EventSourceId<Guid>(Value), IGeneratable<AuthorId>
        {
            public static AuthorId New() => new(Guid.NewGuid());
        }
        """;

    static readonly SourceTextChange _usings = new("using Cratis.Chronicle.Events;", "using Cratis.Chronicle.Events;\nusing Cratis.Concepts;");
    static readonly SourceTextChange _concept = new("public record AuthorId(Guid Value) : EventSourceId<Guid>(Value);", Concept);

    /// <summary>
    /// Wraps a command with a UUID-generating identity concept.
    /// </summary>
    /// <param name="command">The command declaration.</param>
    /// <returns>The source containing the concept and event.</returns>
    public static string With(string command) => _concept.Apply(_usings.Apply(IdentifierSources.With(command)));

    /// <summary>
    /// Gets the real extension-block shape for pins, without installing a runtime fixture.
    /// </summary>
    public const string Pins = """
        namespace Cratis.Arc.Testing.Commands;
        public static class CommandScenarioGeneratedValueExtensions
        {
            extension<TCommand>(CommandScenario<TCommand> scenario)
            {
                public CommandScenario<TCommand> PinGeneratedValue(string name, object value) => scenario;
                public CommandScenario<TCommand> PinGeneratedValue<TOtherCommand>(string name, object value) => scenario;
                public CommandScenario<TCommand> AllowGeneration() => scenario;
            }
        }
        """;
}
