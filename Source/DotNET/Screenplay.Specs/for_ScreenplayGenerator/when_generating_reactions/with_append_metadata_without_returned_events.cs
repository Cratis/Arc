// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions;

public class with_append_metadata_without_returned_events : a_filtered_application
{
    void Because() => GenerateFilteredWith("""
        [EventSourceType("Unknown")]
        public class Welcomer : IReactor
        {
            public void Welcome(AuthorRegistered @event) { }
            public Task File(AuthorFiled @event) => Task.CompletedTask;
            public Task<SendWelcome> Send(AuthorWelcomed @event) => Task.FromResult(new SendWelcome(@event.Name, "UK"));
            public IEnumerable<SendWelcome> Repeat(WelcomeSent @event) => [new SendWelcome(@event.Name, "UK")];
        }
        """);

    [Fact] void should_not_report_an_append_routing_refusal() => Result.Diagnostics.Where(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnrepresentableReactionAppendRoute).ShouldBeEmpty();
    [Fact] void should_keep_the_original_file_backed_reaction() => Result.Source.ShouldContain("""
              reaction Welcomer
                when AuthorFiled
                  file Authors/Welcoming/Welcoming.cs
                when AuthorRegistered
                  file Authors/Welcoming/Welcoming.cs
                when AuthorWelcomed
                  file Authors/Welcoming/Welcoming.cs
                when WelcomeSent
                  file Authors/Welcoming/Welcoming.cs
        """);
    [Fact] void should_not_substitute_executable_commands() => Result.Source.ShouldNotContain("invokes SendWelcome");
    [Fact] void should_not_invent_an_observation_filter() => Result.Source.ShouldNotContain("from Unknown");
    [Fact] void should_compile_and_round_trip() => AssertDocument();
}
