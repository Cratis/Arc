// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class with_a_scenario_stating_an_unnameable_property : given.a_generated_document
{
    void Because() => Generate([
        (Analyzed.SlicePath, """
            using Cratis.Arc.Commands.ModelBound;
            using Cratis.Chronicle.Events;
            using Cratis.Chronicle.Keys;
            namespace Library.Authors.Registration;
            public interface IRule;
            [EventType] public record AuthorRegistered(string Name, IRule Rule);
            [Command] public record RegisterAuthor([Key] string Id, string Name, IRule Rule)
            {
                public AuthorRegistered Handle() => new(Name, Rule);
            }
            """),
        ("Library/Authors/Registration/Specs/registering.cs", """
            using System.Threading.Tasks;
            using Cratis.Arc.Testing.Commands;
            using Cratis.Arc.Chronicle.Testing.Commands;
            using Cratis.Chronicle.Testing.EventSequences;
            using Cratis.Specifications;
            using Xunit;
            namespace Library.Authors.Registration.when_registering;
            public class with_a_rule : Specification
            {
                readonly CommandScenario<RegisterAuthor> _scenario = new();
                async Task Because() => await _scenario.Execute(new RegisterAuthor("author", "Jane", null!));
                [Fact] Task should_append() => _scenario.EventSequence.ShouldHaveAppendedEvent<AuthorRegistered>("author", e => e.Name == "Jane" && e.Rule == null);
            }
            """),
        (IntegrationTesting.Path, IntegrationTesting.Source)]);

    [Fact] void should_not_state_a_weaker_scenario() => Result.Source.ShouldNotContain("specification ");
    [Fact] void should_report_the_unstatable_property() => Result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableSpecification && diagnostic.Message.Contains("RegisterAuthor.Rule", StringComparison.Ordinal));
    [Fact] void should_not_keep_the_dependent_production() => Result.Source.ShouldNotContain("produces AuthorRegistered");
    [Fact] void should_round_trip_and_bind() => AssertDocument();
}
