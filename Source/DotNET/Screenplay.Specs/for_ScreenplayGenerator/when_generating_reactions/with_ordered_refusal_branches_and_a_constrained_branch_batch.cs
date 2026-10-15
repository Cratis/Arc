// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission;
using Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions.given;
using Cratis.Screenplay.Semantics;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions;

public class with_ordered_refusal_branches_and_a_constrained_branch_batch : a_refusing_application
{
    const string Chain = ".OnRefused(Refused.ByValidation, refusal => new object[] { new AuthorWelcomed(@event.Name, \"Welcome\"), new AuthorFiled(@event.Country) }).Acknowledge(Refused.Any)";
    const string ConstraintPath = "Library/Authors/Welcoming/FilingConstraint.cs";
    const string Constraint = """
        using Cratis.Chronicle.Events.Constraints;
        using Library.Authors.Registration;
        namespace Library.Authors.Welcoming;
        public class FilingConstraint : IConstraint
        {
            public void Define(IConstraintBuilder builder) => builder.Unique<AuthorFiled>();
        }
        """;

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_state_the_branch_batch_atomically_at_v22(bool authoring)
    {
        GenerateRefusal(Chain, authoring, cap: SemanticVersion.V22, sources: [(ConstraintPath, Constraint)]);

        Result.Source.ShouldContain("reaction batches atomic");
        Result.Source.ShouldContain("on refused by validation");
        Result.Source.ShouldContain("produces AuthorWelcomed");
        Result.Source.ShouldContain("produces AuthorFiled");
        Result.Source.ShouldNotContain("file Authors/Welcoming/Welcoming.cs");
        Result.Diagnostics.Where(report => report.Code == "SP0070" || report.Code == "SP0071").ShouldBeEmpty();
        Bound.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V22);
        AssertDocument();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_report_the_batch_gap_instead_of_generic_refusal_handling_below_v22(bool authoring)
    {
        GenerateRefusal(Chain, authoring, cap: SemanticVersion.V21, sources: [(ConstraintPath, Constraint)]);

        Result.Source.ShouldNotContain("reaction batches");
        Result.Source.ShouldContain("file Authors/Welcoming/Welcoming.cs");
        Result.Source.ShouldNotContain("on refused");
        Result.Diagnostics.Single(report => report.Code == "SP0070").Message.ShouldContain("ESM v22");
        Result.Diagnostics.Where(report => report.Code == "SP0071").ShouldBeEmpty();
        AssertDocument();
    }

    [Fact]
    public void should_withhold_a_branch_if_its_rejecting_constraint_is_removed()
    {
        GenerateRefusal(Chain, sources: [(ConstraintPath, Constraint)]);
        var model = Result.Model with
        {
            Slices = Result.Model.Slices.Select(slice => slice with
            {
                Constraints = slice.Constraints.Where(constraint => !constraint.Name.StartsWith("FilingConstraint", StringComparison.Ordinal)).ToList()
            }).ToList()
        };
        var emitted = new ScreenplayEmitter().Emit(model, new ScreenplayOptions());

        emitted.Source.ShouldNotContain("reaction batches atomic");
        emitted.Source.ShouldNotContain("on refused");
        emitted.Source.ShouldContain("file Authors/Welcoming/Welcoming.cs");
        emitted.Diagnostics.Single(report => report.Code == "SP0070").Message.ShouldContain("not represented in the document");
        emitted.Diagnostics.Where(report => report.Code == "SP0071").ShouldBeEmpty();
    }

    [Theory]
    [InlineData("builder.ClosesStreamOn<AuthorWelcomed>()", "Cratis/Screenplay#770")]
    [InlineData("Configure(builder)", "cannot be read")]
    public void should_keep_closing_and_unreadable_branch_batches_as_code(string definition, string reason)
    {
        var constraint = Constraint.Replace("=> builder.Unique<AuthorFiled>();", $"{{ builder.Unique<AuthorRegistered>(); {definition}; }}", StringComparison.Ordinal)
            .Replace("public void Define", "static void Configure(IConstraintBuilder builder) => builder.Unique<AuthorFiled>(); public void Define", StringComparison.Ordinal);
        GenerateRefusal(Chain, sources: [(ConstraintPath, constraint)]);

        Result.Source.ShouldNotContain("reaction batches");
        Result.Source.ShouldContain("file Authors/Welcoming/Welcoming.cs");
        Result.Diagnostics.Single(report => report.Code == "SP0070").Message.ShouldContain(reason);
        Result.Diagnostics.Where(report => report.Code == "SP0071").ShouldBeEmpty();
        AssertDocument();
    }
}
