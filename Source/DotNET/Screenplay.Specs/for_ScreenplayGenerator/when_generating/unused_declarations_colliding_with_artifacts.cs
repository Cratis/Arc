// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class unused_declarations_colliding_with_artifacts : a_generated_document
{
    const string Source = """
        using System.Collections.Generic;
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Arc.Queries.ModelBound;
        using Cratis.Chronicle.Events;

        namespace Library.Authors.Registration;

        [EventType]
        public record AuthorRegistered(string Name);

        [Command]
        public record RegisterAuthor(string Name)
        {
            public AuthorRegistered Handle() => new(Name);
        }

        [ReadModel]
        public record Entry(string EntryId, string Name)
        {
            public static IEnumerable<Entry> AllEntries() => [];
        }
        """;

    [Theory]
    [InlineData("Entry", false)]
    [InlineData("Entry", true)]
    [InlineData("AuthorRegistered", true)]
    [InlineData("RegisterAuthor", true)]
    [InlineData("AllEntries", true)]
    [InlineData("Library", true)]
    [InlineData("Authors", true)]
    [InlineData("Registration", true)]
    public void should_not_change_any_referenced_artifact(string name, bool concept)
    {
        Generate((Analyzed.SlicePath, Source));
        var expected = Result.Source;
        var declaration = concept ? $"public record {name}(string Value) : ConceptAs<string>(Value);" : $"public enum {name} {{ First, Second }}";
        Generate((Analyzed.SlicePath, Source), ("Library/Unused.cs", $"using Cratis.Concepts; namespace A.Unused; {declaration}"));

        Result.Model.Concepts.ShouldBeEmpty();
        Result.Source.ShouldEqual(expected);
        Result.Source.ShouldContain("readmodel Entry");
        Result.Source.ShouldContain("query AllEntries");
        Result.Source.ShouldContain("event AuthorRegistered");
        Result.Source.ShouldContain("command RegisterAuthor");
        Result.Diagnostics.Where(diagnostic => diagnostic.Severity != ScreenplayDiagnosticSeverity.Information).ShouldBeEmpty();
        AssertDocument();
    }
}
