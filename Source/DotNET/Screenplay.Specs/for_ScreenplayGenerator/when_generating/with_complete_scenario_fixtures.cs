// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class with_complete_scenario_fixtures
{
    static (string Path, string Text)[] Sources(string properties, string creation, string given = "") =>
    [
        ("Library/Authors/Registration/Registration.cs", $$"""
            #nullable enable
            using System;
            using Cratis.Arc.Commands.ModelBound;
            using Cratis.Chronicle.Events;

            namespace Library.Authors.Registration;

            public enum Status { New, Registered }

            [EventType]
            public record AuthorRegistered(string Name);

            [Command]
            public record RegisterAuthor
            {
                public string Name { get; init; } = "Jane Austen";
                {{properties}}
                public AuthorRegistered Handle() => new(Name);
            }
            """),
        ("Library/Authors/Registration/when_registering/and_the_fixture_is_complete.cs", $$"""
            using System;
            using System.Threading.Tasks;
            using Cratis.Arc.Testing.Commands;
            using Cratis.Arc.Chronicle.Testing.Commands;
            using Cratis.Chronicle.Testing.EventSequences;
            using Library.Authors.Registration;
            using Xunit;

            namespace Library.Authors.Registration.when_registering;

            public class and_the_fixture_is_complete
            {
                readonly CommandScenario<RegisterAuthor> _scenario = new();
                Result _result = null!;

                void Establish() { {{given}} }
                async Task Because() => _result = await _scenario.Execute({{creation}});

                [Fact] void should_not_succeed() => _result.ShouldHaveValidationErrors();
            }
            """),
        (IntegrationTesting.Path, IntegrationTesting.Source)
    ];

    static ScreenplayGenerationResult Generate((string Path, string Text)[] sources)
    {
        Analyzed.ErrorsIn(sources).ShouldBeEmpty();

        return new ScreenplayGenerator().Generate(Analyzed.Compile(sources), new ScreenplayOptions());
    }

    [Fact]
    public void should_state_every_provable_value_and_bind_the_scenario()
    {
        var result = Generate(Sources("public int Age { get; init; }", "new RegisterAuthor { Name = \"Mary Shelley\", Age = 28 }"));
        result.Source.ShouldContain("specification WhenRegisteringAndTheFixtureIsComplete");
        result.Source.ShouldContain("name = \"Mary Shelley\"");
        result.Source.ShouldContain("age = 28");
        result.Diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public void should_state_proven_initializers_and_automatic_property_defaults()
    {
        var result = Generate(Sources("public int Age { get; init; } public bool Active { get; init; } public Status Status { get; init; }", "new RegisterAuthor()"));
        result.Source.ShouldContain("specification WhenRegisteringAndTheFixtureIsComplete");
        result.Source.ShouldContain("name = \"Jane Austen\"");
        result.Source.ShouldContain("age = 0");
        result.Source.ShouldContain("active = false");
        result.Source.ShouldContain("status = \"new\"");
        result.Diagnostics.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("public Guid Id { get; init; }", "id = \"00000000-0000-0000-0000-000000000000\"")]
    [InlineData("public DateOnly Date { get; init; }", "date = \"0001-01-01\"")]
    [InlineData("public DateTimeOffset Instant { get; init; }", "instant = \"0001-01-01T00:00:00+00:00\"")]
    public void should_state_stateable_framework_defaults(string properties, string stated)
    {
        var result = Generate(Sources(properties, "new RegisterAuthor()"));
        result.Source.ShouldContain("specification WhenRegisteringAndTheFixtureIsComplete");
        result.Source.ShouldContain(stated);
        result.Diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public void should_state_a_positional_constructor_default()
    {
        var sources = Sources("", "new RegisterAuthor()");
        sources[0] = (sources[0].Path, sources[0].Text.Replace("public record RegisterAuthor", "public record RegisterAuthor(int Age = 21)", StringComparison.Ordinal));
        var result = Generate(sources);
        result.Source.ShouldContain("age = 21");
        result.Diagnostics.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("Value", "Guid.NewGuid()")]
    [InlineData("Guid.NewGuid()", "Guid.Parse(value)")]
    public void should_not_invent_a_fixture_for_a_concept_conversion_that_changes_its_input(string backing, string converted)
    {
        var sources = Sources("public AuthorId Id { get; init; } = null!;", "new RegisterAuthor { Id = \"6f3c8b47-1938-4d4c-8f26-817e306a10e2\" }");
        sources[0] = (sources[0].Path, sources[0].Text + $$"""

            public record AuthorId(Guid Value) : Cratis.Concepts.ConceptAs<Guid>({{backing}})
            {
                public static implicit operator AuthorId(string value) => new({{converted}});
            }
            """);
        var result = Generate(sources);
        result.Source.ShouldNotContain("specification WhenRegisteringAndTheFixtureIsComplete");
        result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableSpecification).Message.ShouldContain("RegisterAuthor.Id");
    }

    [Theory]
    [InlineData("public Guid Id { get; init; }", "new RegisterAuthor { Id = Guid.NewGuid() }", "Id")]
    [InlineData("public int Age { get; init; }", "new RegisterAuthor { Age = DateTime.UtcNow.Year }", "Age")]
    [InlineData("public string? Alias { get; init; }", "new RegisterAuthor()", "Alias")]
    [InlineData("public string? Alias { get; init; }", "new RegisterAuthor { Alias = null }", "Alias")]
    [InlineData("public int? Age { get; init; }", "new RegisterAuthor()", "Age")]
    [InlineData("public int Age { get; init; } = DateTime.UtcNow.Year;", "new RegisterAuthor()", "Age")]
    [InlineData("public int Age => DateTime.UtcNow.Year;", "new RegisterAuthor()", "Age")]
    [InlineData("public int Age { get; init; } public RegisterAuthor() { Age = DateTime.UtcNow.Year; }", "new RegisterAuthor { Name = \"Jane Austen\" }", "Age")]
    public void should_omit_a_scenario_with_an_unstateable_property(string properties, string creation, string property)
    {
        var result = Generate(Sources(properties, creation));
        result.Source.ShouldNotContain("specification WhenRegisteringAndTheFixtureIsComplete");
        result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableSpecification).Message.ShouldContain($"RegisterAuthor.{property}");
        result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldNotContain(ScreenplayDiagnosticCodes.DocumentDidNotBind);
        result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldNotContain(ScreenplayDiagnosticCodes.UnreadableSpecificationValue);
    }

    [Fact]
    public void should_omit_a_scenario_with_an_incomplete_given_event()
    {
        var result = Generate(Sources("", "new RegisterAuthor()", "_scenario.Given.ForEventSource(\"author\").Events(new AuthorRegistered(DateTime.UtcNow.Year.ToString()));"));
        result.Source.ShouldNotContain("specification WhenRegisteringAndTheFixtureIsComplete");
        result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableSpecification).Message.ShouldContain("AuthorRegistered.Name");
        result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldNotContain(ScreenplayDiagnosticCodes.DocumentDidNotBind);
    }
}
