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
                public string Name { get; init; }
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
    public void should_state_a_pass_through_concept_value_as_it_was_read_before()
    {
        var sources = Sources("public Quantity Amount { get; init; } = null!;", "new RegisterAuthor { Name = \"Jane Austen\", Amount = 5 }");
        sources[0] = (sources[0].Path, sources[0].Text + "\npublic record Quantity(int Value) : Cratis.Concepts.ConceptAs<int>(Value)\n{\n    public static implicit operator Quantity(int value) => new(value);\n}\n");
        var result = Generate(sources);
        result.Source.ShouldContain("specification WhenRegisteringAndTheFixtureIsComplete");
        result.Source.ShouldContain("amount = 5");
    }

    [Theory]
    [InlineData("public int Age { get; init; } = 42;")]
    [InlineData("public bool Active { get; init; }")]
    [InlineData("public Guid Id { get; init; }")]
    public void should_not_state_a_value_the_creation_does_not_state(string properties)
    {
        var result = Generate(Sources(properties, "new RegisterAuthor { Name = \"Jane Austen\" }"));
        result.Source.ShouldNotContain("specification WhenRegisteringAndTheFixtureIsComplete");
        result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableSpecification).Message.ShouldContain("RegisterAuthor.");
    }

    [Fact]
    public void should_omit_a_scenario_whose_concept_property_only_has_an_initializer_converted_to_a_generated_identity()
    {
        var sources = Sources("public AuthorId Id { get; init; } = \"6f3c8b47-1938-4d4c-8f26-817e306a10e2\";", "new RegisterAuthor { Name = \"Jane Austen\" }");
        sources[0] = (sources[0].Path, sources[0].Text + """

            public record AuthorId(Guid Value) : Cratis.Concepts.ConceptAs<Guid>(Value)
            {
                public static implicit operator AuthorId(string value) => new(Guid.NewGuid());
            }
            """);
        var result = Generate(sources);
        result.Source.ShouldNotContain("specification WhenRegisteringAndTheFixtureIsComplete");
        result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableSpecification).Message.ShouldContain("RegisterAuthor.Id");
    }

    [Theory]
    [InlineData("public Guid Id { get; init; }", "new RegisterAuthor { Name = \"Jane Austen\", Id = Guid.NewGuid() }", "Id")]
    [InlineData("public int Age { get; init; }", "new RegisterAuthor { Name = \"Jane Austen\", Age = DateTime.UtcNow.Year }", "Age")]
    [InlineData("public string? Alias { get; init; }", "new RegisterAuthor { Name = \"Jane Austen\" }", "Alias")]
    [InlineData("public string? Alias { get; init; }", "new RegisterAuthor { Name = \"Jane Austen\", Alias = null }", "Alias")]
    [InlineData("public int? Age { get; init; }", "new RegisterAuthor { Name = \"Jane Austen\" }", "Age")]
    [InlineData("public int Age { get; init; } = DateTime.UtcNow.Year;", "new RegisterAuthor { Name = \"Jane Austen\" }", "Age")]
    [InlineData("public int Age => DateTime.UtcNow.Year;", "new RegisterAuthor { Name = \"Jane Austen\" }", "Age")]
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
        var result = Generate(Sources("", "new RegisterAuthor { Name = \"Jane Austen\" }", "_scenario.Given.ForEventSource(\"author\").Events(new AuthorRegistered(DateTime.UtcNow.Year.ToString()));"));
        result.Source.ShouldNotContain("specification WhenRegisteringAndTheFixtureIsComplete");
        result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableSpecification).Message.ShouldContain("AuthorRegistered.Name");
        result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldNotContain(ScreenplayDiagnosticCodes.DocumentDidNotBind);
    }
}
