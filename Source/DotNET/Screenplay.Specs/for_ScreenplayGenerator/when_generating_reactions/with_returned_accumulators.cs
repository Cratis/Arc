// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions;

public class with_returned_accumulators : a_filtered_application
{
    const string Event = "new AuthorWelcomed(@event.Name, \"Welcome\")";

    [Theory]
    [InlineData("var effects = new System.Collections.Generic.List<object>();")]
    [InlineData("System.Collections.Generic.List<object> effects = [];")]
    [InlineData("System.Collections.Generic.List<object> effects = new();")]
    public void should_recover_unconditional_additions_to_an_empty_list(string declaration)
    {
        GenerateFilteredWith($$"""
            public class Welcomer : IReactor
            {
                public System.Collections.Generic.IEnumerable<object> Welcome(AuthorRegistered @event)
                {
                    {{declaration}}
                    effects.Add({{Event}});
                    return effects;
                }
            }
            """);

        Result.Source.ShouldContain("produces AuthorWelcomed");
        Result.Source.ShouldNotContain("file Authors/Welcoming/Welcoming.cs");
        AssertDocument();
    }

    [Theory]
    [InlineData("if (@event.Name.Length > 0) effects.Add(new AuthorWelcomed(@event.Name, \"Welcome\"));")]
    [InlineData("foreach (var name in new[] { @event.Name }) effects.Add(new AuthorWelcomed(name, \"Welcome\"));")]
    [InlineData("effects.Clear(); effects.Add(new AuthorWelcomed(@event.Name, \"Welcome\"));")]
    [InlineData("effects.Add(new AuthorWelcomed(@event.Name, \"Welcome\")); effects = new();")]
    [InlineData("var alias = effects; effects.Add(new AuthorWelcomed(@event.Name, \"Welcome\"));")]
    [InlineData("effects.Add(effects);")]
    public void should_keep_control_flow_and_other_list_uses_as_code(string statements)
    {
        GenerateFilteredWith($$"""
            public class Welcomer : IReactor
            {
                public System.Collections.Generic.IEnumerable<object> Welcome(AuthorRegistered @event)
                {
                    var effects = new System.Collections.Generic.List<object>();
                    {{statements}}
                    return effects;
                }
            }
            """);

        Result.Source.ShouldNotContain("produces AuthorWelcomed");
        Result.Source.ShouldContain("file Authors/Welcoming/Welcoming.cs");
        AssertDocument();
    }
}
