// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing_the_projects_of_an_application;

/// <summary>
/// A value a scenario states is followed back to the member holding it, and the project declaring that member is not
/// the project the scenario is written in - a well known identity, a sentinel or a member of an enumeration lives in
/// the contracts project below the handlers, which is where an application really puts them.
/// </summary>
/// <remarks>
/// A project reference is handed to the analyzer as the compilation the workspace built rather than as an assembly on
/// disk, so the member is a symbol with real source behind it and that source belongs to another compilation.
/// Reading it through the compilation of the project doing the naming is not a wrong answer but a crash, which is why
/// every body is read through the models of the whole application.
/// <para>
/// Not crashing is the smaller half of this. Reading the declaration through the project that actually holds it is
/// what lets the identity still be recognized as one made on the spot, and therefore still be left out of the
/// document rather than reported as a value nothing was recovered from - so the assertions are about the values, and
/// a guard that merely declined to read across the boundary would fail them.
/// </para>
/// </remarks>
public class a_scenario_stating_an_identity_a_sibling_project_declares : Specification
{
    const string Contracts = """
        using System;
        using Cratis.Chronicle.Events;
        using Cratis.Concepts;

        namespace Library.Authors.Registration;

        public record AuthorId(Guid Value) : ConceptAs<Guid>(Value)
        {
            public static AuthorId New() => new(Guid.NewGuid());

            public static implicit operator Guid(AuthorId id) => id.Value;
        }

        public static class KnownAuthors
        {
            public static readonly AuthorId Jane = AuthorId.New();
        }

        [EventType]
        public record AuthorRegistered(AuthorId Id, string Name);
        """;

    const string Slice = """
        using Cratis.Arc.Commands.ModelBound;

        namespace Library.Authors.Registration;

        [Command]
        public record RegisterAuthor(AuthorId Id, string Name)
        {
            public AuthorRegistered Handle() => new(Id, Name);
        }
        """;

    const string Scenario = """
        using System.Threading.Tasks;
        using Cratis.Arc.Testing.Commands;
        using Cratis.Chronicle.Testing.EventSequences;
        using Library.Authors.Registration;
        using Xunit;

        namespace Library.Authors.Registration.when_registering;

        public class and_the_author_was_registered_before
        {
            readonly CommandScenario<RegisterAuthor> _scenario = new();
            Result _result = null!;

            void Establish() => _scenario.Given.ForEventSource("author").Events(new AuthorRegistered(KnownAuthors.Jane, "Jane Austen"));

            async Task Because() => _result = await _scenario.Execute(new RegisterAuthor(KnownAuthors.Jane, "Mary Shelley"));

            [Fact] void should_not_succeed() => _result.ShouldNotBeSuccessful();
        }
        """;

    Compilation _contracts;
    Compilation _application;
    ApplicationModelAnalysis _analysis;
    SpecificationModel _specification;

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
            ("Source/Library/Authors/Registration/Registration.cs", Slice),
            ("Source/Library/Authors/Registration/when_registering/and_the_author_was_registered_before.cs", Scenario),
            ("Source/Library/Testing/IntegrationTesting.cs", IntegrationTesting.Source));
    }

    void Because()
    {
        _analysis = Analyzed.Projects(_application, _contracts);
        _specification = _analysis.Model.Slices.Single(_ => _.Name == "Registration").Specifications.Single();
    }

    [Fact] void should_compile_the_contracts_project() => Analyzed.ErrorsIn(_contracts).ShouldBeEmpty();
    [Fact] void should_compile_the_application_project() => Analyzed.ErrorsIn(_application).ShouldBeEmpty();
    [Fact] void should_state_the_values_of_what_it_starts_from() => _specification.Given.Single().Values.ShouldContainOnly([new PropertyMappingModel("Name", new LiteralSource("Jane Austen"))]);
    [Fact] void should_state_the_values_the_command_was_issued_with() => _specification.When.Values.ShouldContainOnly([new PropertyMappingModel("Name", new LiteralSource("Mary Shelley"))]);
    [Fact] void should_report_nothing() => _analysis.Diagnostics.ShouldBeEmpty();
}
