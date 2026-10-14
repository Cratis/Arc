// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions;

public class with_semantically_resolved_refusal_symbols : a_refusing_application
{
    [Fact]
    public void should_resolve_constant_constraint_names_and_named_arguments()
    {
        GenerateRefusal(".OnRefused(handler: refusal => new AuthorFiled(refusal.Message), selector: Refused.ByConstraint(ConstraintName))", members: "const string ConstraintName = \"UniqueAuthor\";");
        Result.Source.ShouldContain("on refused by constraint UniqueAuthor");
        AssertRefusalExtraction();
    }

    [Fact]
    public void should_resolve_fully_qualified_arc_methods()
    {
        GenerateWith("""
            public class Welcomer : IReactor
            {
                public CommandInvocation Welcome(AuthorRegistered @event) => Cratis.Arc.Chronicle.Reactors.Invoke.Command(new SendWelcome(@event.Name, @event.Country))
                    .OnRefused(Cratis.Arc.Chronicle.Reactors.Refused.Any, refusal => new AuthorFiled(refusal.Message));
            }
            """);
        AssertRefusalExtraction();
    }

    [Theory]
    [InlineData(".OnRefused(Selectors.ByValidation, refusal => null)", "static class Selectors { public static Refused ByValidation => Refused.ByValidation; }")]
    [InlineData(".Acknowledge(Selectors.ByConstraint(\"UniqueAuthor\"))", "static class Selectors { public static Refused ByConstraint(string name) => Refused.ByConstraint(name); }")]
    [InlineData(".Acknowledge(Selector)", "Refused Selector => Refused.ByValidation;")]
    [InlineData(".Acknowledge(Refused.ByValidation)", "static class Invoke { public static CommandInvocation Command(object command) => Cratis.Arc.Chronicle.Reactors.Invoke.Command(command); }")]
    public void should_not_extract_lookalike_factories_or_opaque_selector_values(string chain, string members)
    {
        GenerateRefusal(chain, members: members);
        AssertRefusalFallback();
    }
}
