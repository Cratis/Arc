// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing_the_projects_of_an_application;

/// <summary>
/// A command declared in a sibling project may rest a property on an initializer. The scenario states only what it
/// writes, so nothing is derived from the initializer and the incomplete scenario is omitted.
/// </summary>
public class a_scenario_leaving_a_property_to_an_initializer_a_sibling_project_declares : Specification
{
    const string Contracts = """
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Chronicle.Events;

        namespace Library.Authors.Registration;

        [EventType]
        public record AuthorRegistered(string Name);

        [Command]
        public record RegisterAuthor
        {
            public string Name { get; init; } = "Jane Austen";

            public int Age { get; init; } = 42;

            public AuthorRegistered Handle() => new(Name);
        }
        """;

    const string Scenario = """
        using System.Threading.Tasks;
        using Cratis.Arc.Testing.Commands;
        using Cratis.Arc.Chronicle.Testing.Commands;
        using Cratis.Chronicle.Testing.EventSequences;
        using Library.Authors.Registration;
        using Xunit;

        namespace Library.Authors.Registration.when_registering;

        public class and_the_author_leaves_the_age_to_its_initializer
        {
            readonly CommandScenario<RegisterAuthor> _scenario = new();
            Result _result = null!;

            async Task Because() => _result = await _scenario.Execute(new RegisterAuthor { Name = "Mary Shelley" });

            [Fact] void should_not_succeed() => _result.ShouldHaveValidationErrors();
        }
        """;

    Compilation _contracts;
    Compilation _application;
    ApplicationModelAnalysis _analysis;

    void Establish()
    {
        _contracts = Analyzed.Project(
            "Library.Contracts",
            [],
            ("Source/Library.Contracts/Contracts.cs", "namespace Library;"),
            ("Source/Library.Contracts/Authors/Registration/Registration.cs", Contracts));

        _application = Analyzed.Project(
            "Library",
            [_contracts.ToMetadataReference()],
            ("Source/Library/Program.cs", "namespace Library;"),
            ("Source/Library/Authors/Registration/when_registering/and_the_author_leaves_the_age_to_its_initializer.cs", Scenario),
            ("Source/Library/Testing/IntegrationTesting.cs", IntegrationTesting.Source));
    }

    void Because() => _analysis = Analyzed.Projects(_application, _contracts);

    [Fact] void should_compile_the_contracts_project() => Analyzed.ErrorsIn(_contracts).ShouldBeEmpty();
    [Fact] void should_compile_the_application_project() => Analyzed.ErrorsIn(_application).ShouldBeEmpty();
    [Fact] void should_not_state_any_scenario() => _analysis.Model.Slices.SelectMany(_ => _.Specifications).ShouldBeEmpty();
    [Fact] void should_name_the_property_it_did_not_state() => _analysis.Diagnostics.Single(_ => _.Code == ScreenplayDiagnosticCodes.UnreadableSpecification).Message.ShouldContain("RegisterAuthor.Age");
    [Fact] void should_not_report_a_value_derived_from_the_initializer() => _analysis.Diagnostics.Select(_ => _.Code).ShouldNotContain(ScreenplayDiagnosticCodes.UnreadableSpecificationValue);
}
