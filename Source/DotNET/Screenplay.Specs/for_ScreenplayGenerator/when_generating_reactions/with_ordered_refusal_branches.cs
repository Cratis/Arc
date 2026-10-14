// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions;

public class with_ordered_refusal_branches : a_refusing_application
{
    [Fact]
    public void should_preserve_first_match_declaration_order()
    {
        GenerateRefusal(".OnRefused(Refused.ByConstraint(\"UniqueAuthor\"), refusal => new AuthorFiled(refusal.ConstraintName!))" +
            ".Acknowledge(Refused.ByAnyConstraint).Acknowledge(Refused.ByValidation).Acknowledge(Refused.ByAuthorization)");
        var branches = Result.Model.Slices.SelectMany(slice => slice.Reactors).Single().Reactions.Single().Invokes.Single().OnRefused;
        branches.Select(branch => branch.Selector).ShouldEqual(["constraint", "constraint", "validation", "authorization"]);
        var named = Result.Source.IndexOf("on refused by constraint UniqueAuthor", StringComparison.Ordinal);
        var any = Result.Source.IndexOf("on refused by constraint\n", StringComparison.Ordinal);
        var validation = Result.Source.IndexOf("on refused by validation", StringComparison.Ordinal);
        var authorization = Result.Source.IndexOf("on refused by authorization", StringComparison.Ordinal);
        named.ShouldBeLessThan(any);
        any.ShouldBeLessThan(validation);
        validation.ShouldBeLessThan(authorization);
        AssertRefusalExtraction();
    }

    [Theory]
    [InlineData(".Acknowledge(Refused.Any).Acknowledge(Refused.ByValidation)")]
    [InlineData(".Acknowledge(Refused.ByAnyConstraint).Acknowledge(Refused.ByConstraint(\"UniqueAuthor\"))")]
    [InlineData(".Acknowledge(Refused.ByAuthorization).Acknowledge(Refused.ByAuthorization)")]
    [InlineData(".Acknowledge(Refused.ByConstraint(\"Missing\"))")]
    [InlineData(".Acknowledge(Refused.ByConstraint(\"unique-author\"))")]
    public void should_keep_unresolvable_or_shadowed_branches_as_code(string chain)
    {
        GenerateRefusal(chain);
        AssertRefusalFallback();
    }

    [Fact]
    public void should_not_let_bare_refusal_shadow_authorization()
    {
        GenerateRefusal(".Acknowledge(Refused.Any).Acknowledge(Refused.ByAuthorization)");
        AssertRefusalExtraction();
    }

    [Fact]
    public void should_keep_a_named_constraint_for_an_unrelated_event_as_code()
    {
        GenerateRefusal(".Acknowledge(Refused.ByConstraint(\"Other\"))", sources:
        [
            ("Library/Authors/Welcoming/OtherEvent.cs", """
                using Cratis.Chronicle.Events;
                using Cratis.Chronicle.Events.Constraints;
                namespace Library.Authors.Welcoming;
                [EventType] public record OtherEvent([property: Unique("Other")] string Value);
                """)
        ]);
        AssertRefusalFallback();
    }
}
