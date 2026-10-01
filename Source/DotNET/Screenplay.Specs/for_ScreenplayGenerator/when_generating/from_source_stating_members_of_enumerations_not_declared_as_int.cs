// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

/// <summary>
/// The compiler hands a constant over as an integer whatever the enumeration it is cast to is declared over, while
/// the members of the enumeration hold constants of the type it is declared over. A member named through a cast to an
/// enumeration declared over a byte or a long is still that member, so it is stated by name rather than left out as
/// a value no member is declared with.
/// </summary>
public class from_source_stating_members_of_enumerations_not_declared_as_int : Specification
{
    const string Slice = """
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Chronicle.Events;

        namespace Library.Contracts.Signing;

        public enum Urgency : byte
        {
            Normal = 0,
            High = 1
        }

        public enum Weight : long
        {
            Light = 0,
            Heavy = 2
        }

        [EventType]
        public record SigningPrioritized(Urgency Urgency, Weight Weight);

        [Command]
        public record PrioritizeSigning(Urgency Urgency, Weight Weight)
        {
            public SigningPrioritized Handle() => new(Urgency, Weight);
        }
        """;

    const string Scenario = """
        using System.Threading.Tasks;
        using Cratis.Arc.Testing.Commands;
        using Cratis.Arc.Chronicle.Testing.Commands;
        using Cratis.Chronicle.Testing.EventSequences;
        using Library.Contracts.Signing;
        using Xunit;

        namespace Library.Contracts.Signing.when_prioritizing;

        public class and_the_members_are_named_through_casts
        {
            readonly CommandScenario<PrioritizeSigning> _scenario = new();

            async Task Because() => await _scenario.Execute(new PrioritizeSigning((Urgency)1, (Weight)2));

            [Fact] Task should_prioritize_the_signing() => _scenario.EventSequence.ShouldHaveAppendedEvent<SigningPrioritized>(
                "signing",
                @event => @event.Urgency == (Urgency)1 && @event.Weight == (Weight)2);
        }

        public class and_the_zero_members_are_written_bare
        {
            readonly CommandScenario<PrioritizeSigning> _scenario = new();

            async Task Because() => await _scenario.Execute(new PrioritizeSigning(0, 0));

            [Fact] Task should_prioritize_the_signing() => _scenario.EventSequence.ShouldHaveAppendedEvent<SigningPrioritized>(
                "signing",
                @event => @event.Urgency == 0 && @event.Weight == 0);
        }
        """;

    static readonly (string Path, string Text)[] _sources =
    [
        ("Library/Contracts/Signing/Signing.cs", Slice),
        ("Library/Contracts/Signing/when_prioritizing/and_the_members_are_named_through_casts.cs", Scenario),
        (IntegrationTesting.Path, IntegrationTesting.Source)
    ];

    ScreenplayGenerationResult _result;
    CompilationResult<Cratis.Screenplay.Syntax.ApplicationSyntax> _compiled;

    void Because()
    {
        _result = new ScreenplayGenerator().Generate(Analyzed.Compile(_sources), new ScreenplayOptions());
        _compiled = new ScreenplayCompiler().Compile(_result.Source);
    }

    IEnumerable<string> Lines() =>
        _result.Source.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(_ => _.Trim());

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(_sources).ShouldBeEmpty();
    [Fact] void should_produce_a_document_that_compiles() => _compiled.Success.ShouldBeTrue();
    [Fact] void should_state_both_scenarios() => Lines().Count(_ => _.StartsWith("specification ", StringComparison.Ordinal)).ShouldEqual(2);
    [Fact] void should_name_the_byte_member_for_the_command_and_the_event_alike() => Lines().Count(_ => _ == @"urgency = ""high""").ShouldEqual(2);
    [Fact] void should_name_the_long_member_for_the_command_and_the_event_alike() => Lines().Count(_ => _ == @"weight = ""heavy""").ShouldEqual(2);
    [Fact] void should_name_the_zero_members_for_the_command_and_the_event_alike() => Lines().Count(_ => _ == @"urgency = ""normal""" || _ == @"weight = ""light""").ShouldEqual(4);
    [Fact] void should_leave_nothing_out() => _result.Diagnostics.Any(_ => _.Code == ScreenplayDiagnosticCodes.UnreadableSpecificationValue || _.Code == ScreenplayDiagnosticCodes.UnreadableSpecification).ShouldBeFalse();
}
