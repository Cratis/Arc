// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions;

public class with_provable_explicit_destinations : a_filtered_application
{
    const string Wrapper = "Cratis.Chronicle.EventSequences.EventForEventSourceId";
    const string Effect = "new Cratis.Chronicle.EventSequences.EventForEventSourceId(@event.Country, new AuthorWelcomed(@event.Name, \"Welcome\"))";

    [Theory]
    [InlineData("Task<Cratis.Chronicle.EventSequences.EventForEventSourceId>", "=> Task.FromResult(EFFECT);")]
    [InlineData(Wrapper, "=> EFFECT;")]
    [InlineData("System.Collections.Generic.IEnumerable<object>", "=> [EFFECT];")]
    [InlineData("System.Collections.Generic.IEnumerable<Cratis.Chronicle.EventSequences.EventForEventSourceId>", "=> [EFFECT];")]
    public void should_preserve_destinations_in_single_and_collection_returns(string returnType, string body)
    {
        GenerateFilteredWith($$"""
            public class Welcomer : IReactor
            {
                public {{returnType}} Welcome(AuthorRegistered @event) {{body.Replace("EFFECT", Effect, StringComparison.Ordinal)}}
            }
            """);

        Result.Source.ShouldContain("produces AuthorWelcomed");
        Result.Source.ShouldContain("for country");
        Result.Source.ShouldNotContain("file Authors/Welcoming/Welcoming.cs");
        AssertDocument();
    }

    [Theory]
    [InlineData("@event.Country.ToLowerInvariant()")]
    [InlineData("@event.Country + \"suffix\"")]
    [InlineData("System.Guid.NewGuid().ToString()")]
    public void should_keep_unprovable_destinations_as_code(string target)
    {
        GenerateFilteredWith($$"""
            public class Welcomer : IReactor
            {
                public Task<{{Wrapper}}> Welcome(AuthorRegistered @event) =>
                    Task.FromResult(new {{Wrapper}}({{target}}, new AuthorWelcomed(@event.Name, "Welcome")));
            }
            """);

        Result.Source.ShouldNotContain("produces AuthorWelcomed");
        Result.Source.ShouldContain("file Authors/Welcoming/Welcoming.cs");
        AssertDocument();
    }
}
