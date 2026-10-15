// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions.given;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions;

public class from_a_class_level_unique_event_in_a_returned_batch : a_returned_event_batch
{
    const string CommandScenario = """
        using System.Threading.Tasks;
        using Cratis.Arc.Testing.Commands;
        using Cratis.Arc.Chronicle.Testing.Commands;
        using Cratis.Chronicle.Testing.EventSequences;
        using Library.Authors.Registration;
        using Xunit;
        namespace Library.Authors.Registration.when_registering;
        public class and_a_name_is_supplied
        {
            readonly CommandScenario<RegisterAuthor> _scenario = new();
            async Task Because() => await _scenario.Execute(new RegisterAuthor("jane", "Jane Austen", "UK"));
            [Fact] Task should_register_the_author() => _scenario.EventSequence.ShouldHaveAppendedEvent<AuthorRegistered>("jane", e => e.Name == "Jane Austen" && e.Country == "UK");
        }
        """;
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void should_withhold_unrepresented_class_level_uniqueness_even_with_atomic_batches(bool repeated, bool authoring)
    {
        var events = repeated
            ? "new AuthorFiled(@event.Country), new AuthorFiled(@event.Country)"
            : "new AuthorWelcomed(@event.Name, \"Welcome\"), new AuthorFiled(@event.Country)";
        Generate(new ScreenplayOptions { AuthoringOnlyConstructs = authoring },
        [
            ("Library/Authors/Registration/Registration.cs", Registration),
            ("Library/Authors/Welcoming/Welcoming.cs", Welcoming.Replace("public record AuthorFiled", "[Cratis.Chronicle.Events.Constraints.Unique]\npublic record AuthorFiled", StringComparison.Ordinal) + $$"""
                public class Welcomer : IReactor
                {
                    public IEnumerable<object> Welcome(AuthorRegistered @event) => [{{events}}];
                }
                """),
            (IntegrationTesting.Path, IntegrationTesting.Source),
            (ScenarioPath, Scenario),
            ("Library/Authors/Registration/when_registering/and_a_name_is_supplied.cs", CommandScenario)
        ]);

        AssertWithheld();
        Result.Source.ShouldNotContain("reaction batches atomic");
        Result.Diagnostics.Single(report => report.Code == "SP0070").Message.ShouldContain("class-level uniqueness constraint on 'AuthorFiled' is not represented in the document");
        var run = Run("WhenRegisteringAndANameIsSupplied");
        run.Passed.ShouldBeFalse();
        var unsupported = Assert.IsType<SemanticUnsupported>(run.Execution);
        unsupported.Details.ShouldContain("Reaction 'Welcomer' has an opaque body");
    }
}
