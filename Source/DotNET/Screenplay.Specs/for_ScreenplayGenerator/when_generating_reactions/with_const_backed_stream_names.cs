// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions;

public class with_const_backed_stream_names : a_filtered_application
{
    const string Definition = """
        [Cratis.Chronicle.EventSources.EventSource("stored-author")]
        [Cratis.Chronicle.EventSources.EventStream(AuthorEventSource.Onboarding)]
        public class AuthorEventSource : Cratis.Chronicle.EventSources.IEventSource
        {
            public const string Onboarding = "stored-onboarding";
        }
        """;
    const string Handler = "public Task<SendWelcome> Welcome(AuthorRegistered @event) => Task.FromResult(new SendWelcome(@event.Name, @event.Country));";

    [Fact]
    public void should_preserve_the_stored_names_of_a_direct_const_reference()
    {
        GenerateFilteredWith(Definition + $$"""
            [Cratis.Chronicle.EventSources.FromEventSource<AuthorEventSource>(AuthorEventSource.Onboarding)]
            public class Welcomer : IReactor { {{Handler}} }
            """);

        Result.Source.ShouldContain("from Author.Onboarding");
        Result.Source.ShouldContain("id \"stored-author\"");
        Result.Source.ShouldContain("id \"stored-onboarding\"");
        Result.Source.ShouldContain("invokes SendWelcome");
        AssertDocument();
    }

    [Theory]
    [InlineData("\"stored-onboarding\"")]
    [InlineData("AuthorEventSource.Onboarding + \"\"")]
    [InlineData("Other.Onboarding")]
    public void should_not_guess_a_stream_name_from_a_value_or_another_owner(string reference)
    {
        GenerateFilteredWith(Definition + $$"""
            public static class Other { public const string Onboarding = "stored-onboarding"; }
            [Cratis.Chronicle.EventSources.FromEventSource<AuthorEventSource>({{reference}})]
            public class Welcomer : IReactor { {{Handler}} }
            """);

        Result.Source.ShouldNotContain("from Author.Onboarding");
        Result.Source.ShouldNotContain("invokes SendWelcome");
        Result.Source.ShouldContain("file Authors/Welcoming/Welcoming.cs");
        Result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(ScreenplayDiagnosticCodes.ObserverEventSourceNotRepresentable);
        AssertDocument();
    }

    [Fact]
    public void should_recover_names_independently_of_an_unrecoverable_sibling_stream()
    {
        GenerateFilteredWith(Definition.Replace("public class AuthorEventSource", "[Cratis.Chronicle.EventSources.EventStream(AuthorEventSource.unsafe_name)] public class AuthorEventSource", StringComparison.Ordinal)
            .Replace("public const string Onboarding", "public const string unsafe_name = \"stored-unsafe\"; public const string Onboarding", StringComparison.Ordinal) + $$"""
            [Cratis.Chronicle.EventSources.FromEventSource<AuthorEventSource>(AuthorEventSource.Onboarding)]
            public class Welcomer : IReactor { {{Handler}} }
            """);

        Result.Source.ShouldContain("from Author.Onboarding");
        Result.Source.ShouldContain("invokes SendWelcome");
        Result.Source.ShouldNotContain("stored-unsafe");
        AssertDocument();
    }

    [Fact]
    public void should_recover_class_names_for_const_backed_definitions_with_grammar_safe_stored_names()
    {
        GenerateFilteredWith(Definition.Replace("stored-author", "StoredAuthor", StringComparison.Ordinal) + $$"""
            [Cratis.Chronicle.EventSources.FromEventSource<AuthorEventSource>(AuthorEventSource.Onboarding)]
            public class Welcomer : IReactor { {{Handler}} }
            """);

        Result.Source.ShouldContain("eventsource Author");
        Result.Source.ShouldContain("id \"StoredAuthor\"");
        Result.Source.ShouldContain("from Author.Onboarding");
        AssertDocument();
    }

    [Theory]
    [InlineData("Author")]
    [InlineData("unsafe_nameEventSource")]
    public void should_not_invent_model_names_for_const_backed_classes_without_identity_names(string className)
    {
        GenerateFilteredWith(Definition.Replace("AuthorEventSource", className, StringComparison.Ordinal) + $$"""
            [Cratis.Chronicle.EventSources.FromEventSource<{{className}}>({{className}}.Onboarding)]
            public class Welcomer : IReactor { {{Handler}} }
            """);

        Result.Source.ShouldNotContain("invokes SendWelcome");
        Result.Source.ShouldContain("file Authors/Welcoming/Welcoming.cs");
        Result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(ScreenplayDiagnosticCodes.ObserverEventSourceNotRepresentable);
        AssertDocument();
    }

    [Fact]
    public void should_recover_a_stored_source_filter_without_a_stream()
    {
        GenerateFilteredWith($$"""
            [Cratis.Chronicle.EventSources.EventSource("Stored-name")]
            public class AuthorEventSource : Cratis.Chronicle.EventSources.IEventSource;
            [EventSourceType("Stored-name")]
            public class Welcomer : IReactor { {{Handler}} }
            """);

        Result.Source.ShouldContain("eventsource Author");
        Result.Source.ShouldContain("id \"Stored-name\"");
        Result.Source.ShouldContain("from Author\n");
        Result.Source.ShouldContain("invokes SendWelcome");
        AssertDocument();
    }
}
