// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// A scenario that starts from an event and then issues a command about the same thing has to agree with itself
/// about which thing that is, so the identity is made once and held in a field rather than written twice. Following
/// it back to where it was made is what keeps the recognition about the value rather than about how close to the
/// step it happens to be written - and a concept declaring the factory is how an identity is really written.
/// </summary>
public class a_specification_holding_the_identity_two_steps_agree_on : Specification
{
    const string Slice = """
        using System;
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Chronicle.Events;
        using Cratis.Concepts;

        namespace Library.Authors.Registration;

        public record AuthorId(Guid Value) : ConceptAs<Guid>(Value)
        {
            public static AuthorId New() => new(Guid.NewGuid());

            public static implicit operator Guid(AuthorId id) => id.Value;
        }

        [EventType]
        public record AuthorRegistered(AuthorId Id, string Name);

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
            readonly AuthorId _id = AuthorId.New();
            Result _result = null!;

            void Establish() => _scenario.Given.ForEventSource("author").Events(new AuthorRegistered(_id, "Jane Austen"));

            async Task Because() => _result = await _scenario.Execute(new RegisterAuthor(_id, "Mary Shelley"));

            [Fact] void should_not_succeed() => _result.ShouldNotBeSuccessful();
        }
        """;

    static readonly (string Path, string Text)[] _sources =
    [
        ("Library/Authors/Registration/Registration.cs", Slice),
        ("Library/Authors/Registration/when_registering/and_the_author_was_registered_before.cs", Scenario),
        (IntegrationTesting.Path, IntegrationTesting.Source)
    ];

    ApplicationModelAnalysis _analysis;
    SpecificationModel _specification;

    void Establish()
    {
        _analysis = Analyzed.Source(_sources);
        _specification = _analysis.Model.Slices.Single(_ => _.Name == "Registration").Specifications.Single();
    }

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(_sources).ShouldBeEmpty();
    [Fact] void should_state_the_values_of_what_it_starts_from() => _specification.Given.Single().Values.ShouldContainOnly([new PropertyMappingModel("Name", new LiteralSource("Jane Austen"))]);
    [Fact] void should_state_the_values_the_command_was_issued_with() => _specification.When.Values.ShouldContainOnly([new PropertyMappingModel("Name", new LiteralSource("Mary Shelley"))]);
    [Fact] void should_report_nothing() => _analysis.Diagnostics.ShouldBeEmpty();
}
