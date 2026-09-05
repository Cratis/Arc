// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// An identity derived from something is a value the source really states, however concept shaped the type it is
/// stated as. The document cannot read it and therefore does not carry it, which is a difference between the two -
/// so it is reported, and only a factory taking nothing is passed over.
/// </summary>
public class a_specification_stating_an_identity_a_factory_worked_out : Specification
{
    const string Slice = """
        using System;
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Chronicle.Events;
        using Cratis.Concepts;

        namespace Library.Authors.Registration;

        public record AuthorId(Guid Value) : ConceptAs<Guid>(Value)
        {
            public static AuthorId From(string name) => new(new Guid(name));

            public static implicit operator Guid(AuthorId id) => id.Value;
        }

        [EventType]
        public record AuthorRegistered(string Name);

        [Command]
        public record RegisterAuthor(AuthorId Id, string Name)
        {
            public AuthorRegistered Handle() => new(Name);
        }
        """;

    const string Scenario = """
        using System.Threading.Tasks;
        using Cratis.Arc.Testing.Commands;
        using Cratis.Chronicle.Testing.EventSequences;
        using Library.Authors.Registration;
        using Xunit;

        namespace Library.Authors.Registration.when_registering;

        public class and_the_identity_is_derived_from_the_name
        {
            readonly CommandScenario<RegisterAuthor> _scenario = new();
            readonly AuthorId _id = AuthorId.From("d5cba81a-3f5a-4a1d-9f4e-2c1d6ba7c0f1");
            Result _result = null!;

            async Task Because() => _result = await _scenario.Execute(new RegisterAuthor(_id, "Jane Austen"));

            [Fact] void should_not_succeed() => _result.ShouldNotBeSuccessful();
        }
        """;

    static readonly (string Path, string Text)[] _sources =
    [
        ("Library/Authors/Registration/Registration.cs", Slice),
        ("Library/Authors/Registration/when_registering/and_the_identity_is_derived_from_the_name.cs", Scenario),
        (IntegrationTesting.Path, IntegrationTesting.Source)
    ];

    ApplicationModelAnalysis _analysis;

    void Establish() => _analysis = Analyzed.Source(_sources);

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(_sources).ShouldBeEmpty();
    [Fact] void should_say_it_left_the_value_out() => _analysis.Diagnostics.Select(_ => _.Code).ShouldContain(ScreenplayDiagnosticCodes.UnreadableSpecificationValue);
}
