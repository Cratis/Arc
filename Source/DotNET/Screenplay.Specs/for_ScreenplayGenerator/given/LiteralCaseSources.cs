// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

internal static class LiteralCaseSources
{
    internal const string Slice = """
        #nullable enable
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Arc.Queries.ModelBound;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.Keys;
        using Cratis.Concepts;

        namespace Library.Authors.Registration;

        public record AuthorName(string Value) : ConceptAs<string>(Value)
        {
            public static implicit operator AuthorName(string value) => new(value);
        }
        [EventType] public record AuthorRegistered(AuthorName Name);
        [ReadModel] public record Notes(string Id, string? Note)
        {
            public static Notes? ById(string id) => new(id, null);
        }
        [Command] public record RegisterAuthor([Key] string Id, AuthorName Name)
        {
            public AuthorRegistered Handle() => new(Name);
        }
        """;

    internal const string Scenario = """
        #nullable enable
        using System.Threading.Tasks;
        using Cratis.Arc.Testing.Commands;
        using Cratis.Arc.Chronicle.Testing.Commands;
        using Cratis.Chronicle.Testing.EventSequences;
        using Cratis.Specifications;
        using Xunit;

        namespace Library.Authors.Registration.given;

        public abstract class Registering(string name, string? note) : Specification
        {
            protected readonly CommandScenario<RegisterAuthor> _scenario = new();
            void Establish() => _scenario.Given.ForEventSource("author").ReadModel(new Notes("author", note));
            async Task Because() => await _scenario.Execute(new RegisterAuthor("author", name));
            [Fact] Task should_append() => _scenario.EventSequence.ShouldHaveAppendedEvent<AuthorRegistered>("author", e => e.Name == name);
        }

        public class Jane() : Registering("Jane", "first");
        public class John() : Registering("John", "second");
        public class Empty() : Registering("", null);
        """;

    internal static (string Path, string Text)[] With(string scenario) =>
    [
        (Analyzed.SlicePath, Slice),
        ("Library/Authors/Registration/Cases.cs", scenario),
        (IntegrationTesting.Path, IntegrationTesting.Source)
    ];
}
