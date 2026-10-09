// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// An identity made where it is stated has no provable fixture value. Since the binder requires every command
/// property, the scenario is omitted instead of being written with an incomplete command.
/// </summary>
public class a_specification_making_an_identity_where_it_states_it : Specification
{
    const string Slice = """
        using System;
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Chronicle.Events;

        namespace Library.Authors.Registration;

        [EventType]
        public record AuthorRegistered(string Name);

        [Command]
        public record RegisterAuthor(Guid Id, string Name)
        {
            public AuthorRegistered Handle() => new(Name);
        }
        """;

    const string Scenario = """
        using System;
        using System.Threading.Tasks;
        using Cratis.Arc.Testing.Commands;
        using Cratis.Chronicle.Testing.EventSequences;
        using Library.Authors.Registration;
        using Xunit;

        namespace Library.Authors.Registration.when_registering;

        public class and_the_identity_is_made_on_the_spot
        {
            readonly CommandScenario<RegisterAuthor> _scenario = new();
            Result _result = null!;

            async Task Because() => _result = await _scenario.Execute(new RegisterAuthor(Guid.NewGuid(), "Jane Austen"));

            [Fact] void should_not_succeed() => _result.ShouldHaveValidationErrors();
        }
        """;

    static readonly (string Path, string Text)[] _sources =
    [
        ("Library/Authors/Registration/Registration.cs", Slice),
        ("Library/Authors/Registration/when_registering/and_the_identity_is_made_on_the_spot.cs", Scenario),
        (IntegrationTesting.Path, IntegrationTesting.Source)
    ];

    ApplicationModelAnalysis _analysis;

    void Establish()
    {
        _analysis = Analyzed.Source(_sources);
    }

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(_sources).ShouldBeEmpty();
    [Fact] void should_omit_the_scenario() => _analysis.Model.Slices.Single(_ => _.Name == "Registration").Specifications.ShouldBeEmpty();
    [Fact] void should_report_the_unprovable_identity() => _analysis.Diagnostics.Single(_ => _.Code == ScreenplayDiagnosticCodes.UnreadableSpecification).Message.ShouldContain("RegisterAuthor.Id");
    [Fact] void should_not_report_partial_values() => _analysis.Diagnostics.Select(_ => _.Code).ShouldNotContain(ScreenplayDiagnosticCodes.UnreadableSpecificationValue);
}
