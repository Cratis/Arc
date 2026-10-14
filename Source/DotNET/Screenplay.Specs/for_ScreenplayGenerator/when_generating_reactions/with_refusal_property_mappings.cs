// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions;

public class with_refusal_property_mappings : a_refusing_application
{
    [Theory]
    [InlineData("Refused.ByAnyConstraint", "refusal.ConstraintName")]
    [InlineData("Refused.ByConstraint(\"UniqueAuthor\")", "refusal.ConstraintName!")]
    public void should_map_the_first_constraint_name_only_in_constraint_branches(string selector, string source)
    {
        GenerateRefusal($".OnRefused({selector}, refusal => new AuthorFiled({source}))");
        Result.Source.ShouldContain("country = $refusal.constraint");
        AssertRefusalExtraction();
    }

    [Theory]
    [InlineData("refusal => new AuthorWelcomed(@event.Name, refusal.Message)", "name = name", "greeting = $refusal.message")]
    [InlineData("refusal => new AuthorGreeted(refusal.Message, context.Occurred)", "name = $refusal.message", "greetedAt = $context.occurred")]
    public void should_keep_captured_trigger_and_context_mappings(string callback, string first, string second)
    {
        GenerateRefusal($".OnRefused(Refused.Any, {callback})");
        Result.Source.ShouldContain(first);
        Result.Source.ShouldContain(second);
        AssertRefusalExtraction();
    }

    [Theory]
    [InlineData("refusal => new object[] { new AuthorFiled(refusal.Message), new AuthorWelcomed(@event.Name, \"Welcome\") }")]
    [InlineData("refusal => new[] { new AuthorFiled(refusal.Message), new AuthorFiled(@event.Country) }")]
    public void should_keep_literal_event_batches_in_append_order(string callback)
    {
        GenerateRefusal($".OnRefused(Refused.ByValidation, {callback})");
        Result.Model.Slices.SelectMany(slice => slice.Reactors).Single().Reactions.Single().Invokes.Single().OnRefused.Single().Produces.Count.ShouldEqual(2);
        AssertRefusalExtraction();
    }

    [Fact]
    public void should_keep_an_explicit_event_source_destination()
    {
        GenerateRefusal(".OnRefused(Refused.Any, refusal => new Cratis.Chronicle.EventSequences.EventForEventSourceId(@event.Name, new AuthorFiled(refusal.Message)))");
        Result.Source.ShouldContain("for name");
        Result.Source.ShouldContain("country = $refusal.message");
        AssertRefusalExtraction();
    }

    [Theory]
    [InlineData("refusal => { return new AuthorFiled(refusal.Message); }", "")]
    [InlineData("RecordRefusal", "static object RecordRefusal(Cratis.Arc.Commands.CommandRefusal refusal) => new AuthorFiled(refusal.Message);")]
    [InlineData("RecordRefusal", "static object RecordRefusal(Cratis.Arc.Commands.CommandRefusal refusal) { return new AuthorFiled(refusal.Message); }")]
    [InlineData("anonymous", "")]
    public void should_read_pure_callback_bodies_and_static_method_groups(string callback, string members)
    {
        if (callback == "anonymous")
        {
            callback = "delegate(Cratis.Arc.Commands.CommandRefusal refusal) { return new AuthorFiled(refusal.Message); }";
        }
        GenerateRefusal($".OnRefused(Refused.ByValidation, {callback})", members: members);
        Result.Source.ShouldContain("country = $refusal.message");
        AssertRefusalExtraction();
    }
}
