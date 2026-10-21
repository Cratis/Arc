// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions.given;
using Cratis.Screenplay.Semantics;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions;

public class with_opaque_refusal_callbacks : a_refusing_application
{
    [Theory]
    [InlineData("refusal => { Console.WriteLine(refusal.Message); return new AuthorFiled(refusal.Message); }")]
    [InlineData("refusal => refusal.Message.Length > 0 ? new AuthorFiled(refusal.Message) : null")]
    [InlineData("refusal => { if (refusal.Message.Length > 0) return new AuthorFiled(refusal.Message); return null; }")]
    [InlineData("refusal => new AuthorFiled(refusal.Kind.ToString())")]
    [InlineData("refusal => new AuthorFiled(refusal.ConstraintName)")]
    [InlineData("refusal => new AuthorFiled(refusal.ConstraintNames[0])")]
    [InlineData("refusal => new AuthorFiled(refusal.Message.Trim())")]
    [InlineData("refusal => new object[] { }")]
    [InlineData("refusal => new object[] { new AuthorFiled(refusal.Message), new SendWelcome(\"x\", \"y\") }")]
    [InlineData("refusal => new Cratis.Chronicle.EventSequences.EventForEventSourceId(refusal.Message, new AuthorFiled(refusal.Message))")]
    [InlineData("refusal => new AuthorFiled(\" leading whitespace \" )")]
    public void should_keep_the_whole_trigger_when_any_callback_is_unproven(string callback)
    {
        GenerateRefusal($".Acknowledge(Refused.ByAuthorization).OnRefused(Refused.ByValidation, {callback})");
        AssertRefusalFallback();
    }

    [Theory]
    [InlineData("static object RecordRefusal(Cratis.Arc.Commands.CommandRefusal refusal) { Console.WriteLine(refusal.Message); return new AuthorFiled(refusal.Message); }")]
    [InlineData("object RecordRefusal(Cratis.Arc.Commands.CommandRefusal refusal) => new AuthorFiled(refusal.Message);")]
    public void should_not_recover_an_effectful_or_instance_method_group(string members)
    {
        GenerateRefusal(".OnRefused(Refused.ByValidation, RecordRefusal)", members: members);
        AssertRefusalFallback();
    }

    [Fact]
    public void should_not_map_the_kind_enum_to_a_string_reason()
    {
        GenerateRefusal(".OnRefused(Refused.Any, refusal => new RefusalRecorded(refusal.Kind))", sources:
        [
            ("Library/Authors/Welcoming/RefusalRecorded.cs", """
                using Cratis.Chronicle.Events;
                using Cratis.Arc.Commands;
                namespace Library.Authors.Welcoming;
                [EventType] public record RefusalRecorded(CommandRefusalKind Kind);
                """)
        ]);
        Result.Source.ShouldNotContain("$refusal.reason");
        AssertRefusalFallback();
    }

    [Fact]
    public void should_recover_a_constrained_refusal_event_batch_atomically()
    {
        GenerateRefusal(".OnRefused(Refused.Any, refusal => new object[] { new AuthorFiled(refusal.Message), new WelcomeSent(@event.Name) })");
        Result.Source.ShouldContain("produces WelcomeSent");
        Result.Diagnostics.Where(report => report.Code == "SP0070").ShouldBeEmpty();
        AssertRefusalExtraction(atomic: true);
    }

    [Fact]
    public void should_report_the_constrained_refusal_batch_below_v22()
    {
        GenerateRefusal(".OnRefused(Refused.Any, refusal => new object[] { new AuthorFiled(refusal.Message), new WelcomeSent(@event.Name) })", cap: SemanticVersion.V21);
        Result.Source.ShouldContain("file Authors/Welcoming/Welcoming.cs");
        Result.Source.ShouldNotContain("reaction batches");
        Result.Source.ShouldNotContain("on refused");
        Result.Diagnostics.Single(report => report.Code == "SP0070").Message.ShouldContain("ESM v22");
        Result.Diagnostics.Where(report => report.Code == "SP0071").ShouldBeEmpty();
        AssertDocument();
    }

    [Fact]
    public void should_not_drop_a_context_append_route_from_branch_events()
    {
        GenerateRefusal(".OnRefused(Refused.Any, refusal => new AuthorFiled(refusal.Message))", attributes: "[EventStreamId(\"Other\")]");
        AssertRefusalFallback();
    }
}
