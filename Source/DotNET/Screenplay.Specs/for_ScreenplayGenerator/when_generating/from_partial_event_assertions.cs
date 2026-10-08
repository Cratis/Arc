// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_partial_event_assertions : a_generated_document
{
    [Theory]
    [InlineData("string", "e => e.Name == \"Jane\"", false)]
    [InlineData("string", null, false)]
    [InlineData("string", "e => e.Name == \"Jane\" && e.Country == \"UK\"", true)]
    [InlineData("string?", "e => e.Name == \"Jane\"", false)]
    [InlineData("int?", "e => e.Name == \"Jane\"", false)]
    public void should_state_only_whole_command_scenarios(string countryType, string? predicate, bool kept)
    {
        GenerateScenario(countryType, predicate, append: false);

        if (kept)
        {
            Result.Source.ShouldContain("specification WhenRegisteringAndItSucceeds");
            Result.Source.ShouldContain("then AuthorRegistered");
            Result.Source.ShouldContain("name = \"Jane\"");
            Result.Diagnostics.Where(_ => _.Code == ScreenplayDiagnosticCodes.UnreadableSpecification).ShouldBeEmpty();
        }
        else
        {
            AssertOmitted();
        }

        AssertBoundDocument();
    }

    [Theory]
    [InlineData("string", "e => e.Name == \"Jane\"")]
    [InlineData("string", null)]
    public void should_omit_partial_append_scenarios(string countryType, string? predicate)
    {
        GenerateScenario(countryType, predicate, append: true);
        AssertOmitted();
        Result.Source.ShouldNotContain("when append AuthorRegistered");
        AssertBoundDocument();
    }

    void GenerateScenario(string countryType, string? predicate, bool append)
    {
        var country = countryType == "int?" ? "42" : "\"UK\"";
        var source = """
            #nullable enable
            using Cratis.Arc.Commands.ModelBound;
            using Cratis.Chronicle.Events;
            using Cratis.Chronicle.Keys;

            namespace Library.Authors.Registration;

            [EventType]
            public record AuthorRegistered(string Name, COUNTRY Country);

            [Command]
            public record RegisterAuthor([Key] string Id, string Name)
            {
                public AuthorRegistered Handle() => new(Name, VALUE);
            }
            """.Replace("COUNTRY", countryType, StringComparison.Ordinal).Replace("VALUE", country, StringComparison.Ordinal);
        var scenario = $$"""
            using System.Threading.Tasks;
            using Cratis.Arc.Testing.Commands;
            using Cratis.Arc.Chronicle.Testing.Commands;
            using Cratis.Chronicle.Events;
            using Cratis.Chronicle.Testing.EventSequences;
            using Library.Authors.Registration;
            using Xunit;

            namespace Library.Authors.Registration.when_registering;

            public class and_it_succeeds
            {
                readonly {{(append ? "EventScenario" : "CommandScenario<RegisterAuthor>")}} _scenario = new();

                async Task Because() => await {{(append ? $"_scenario.When.ForEventSource(\"author\").Events(new AuthorRegistered(\"Jane\", {country}))" : "_scenario.Execute(new RegisterAuthor(\"author\", \"Jane\"))")}};

                [Fact] {{(predicate is null ? "void" : "Task")}} should_append() => _scenario.EventSequence.ShouldHaveAppendedEvent<AuthorRegistered>("author"{{(predicate is null ? string.Empty : $", {predicate}")}});
            }
            """;

        Generate(
            (Analyzed.SlicePath, source),
            ("Library/Authors/Registration/when_registering/and_it_succeeds.cs", scenario),
            (IntegrationTesting.Path, IntegrationTesting.Source));
    }

    void AssertOmitted()
    {
        Result.Source.ShouldNotContain("specification WhenRegisteringAndItSucceeds");
        var diagnostic = Result.Diagnostics.Single(_ => _.Code == ScreenplayDiagnosticCodes.UnreadableSpecification);
        diagnostic.Severity.ShouldEqual(ScreenplayDiagnosticSeverity.Warning);
        diagnostic.Location.ShouldEqual("Library.Authors.Registration.when_registering.and_it_succeeds");
        diagnostic.Message.ShouldContain("AuthorRegistered");
        diagnostic.Message.ShouldContain("Country");
        diagnostic.Message.ShouldContain("a specification states an event whole");
    }

    void AssertBoundDocument()
    {
        Result.Diagnostics.Where(_ => _.Code == ScreenplayDiagnosticCodes.DocumentDidNotBind).ShouldBeEmpty();
        AssertDocument();
    }
}
