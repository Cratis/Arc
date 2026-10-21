// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission;
using Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions.given;
using Cratis.Screenplay.Semantics;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions;

public class from_unrepresented_batch_constraints : a_returned_event_batch
{
    [Theory]
    [InlineData("AuthorFiled")]
    [InlineData("AuthorWelcomed")]
    public void should_withhold_an_unemitted_rule_even_with_another_represented_rule(string target)
    {
        var property = target == "AuthorFiled" ? "Country" : "Name";
        GenerateBatch("new AuthorWelcomed(@event.Name, \"Welcome\"), new AuthorFiled(@event.Country)", Constraint($$"""
            {
                builder.Unique<{{target}}>();
                builder.Unique(unique => unique.On<{{target}}>(e => e.{{property}})
                    .WithMode(Cratis.Chronicle.Events.Constraints.UniqueConstraintMode.PerValue));
            }
            """));

        AssertWithheld();
        Result.Source.ShouldContain($"unique event {target}");
        Result.Source.ShouldNotContain("reaction batches atomic");
        Result.Diagnostics.Single(report => report.Code == "SP0070").Message.ShouldContain("constraint 'FilingConstraint' is not represented in the document");
    }

    [Fact]
    public void should_check_every_required_declaration_against_the_model_being_emitted()
    {
        GenerateBatch("new AuthorWelcomed(@event.Name, \"Welcome\"), new AuthorFiled(@event.Country)", Constraint("""
            {
                builder.Unique<AuthorFiled>();
                builder.Unique(unique => unique.On<AuthorFiled>(e => e.Country).WithName("CountryClaim"));
            }
            """));
        AssertAtomicBatch();
        var model = Result.Model with
        {
            Slices = Result.Model.Slices.Select(slice => slice with
            {
                Constraints = slice.Constraints.Where(constraint => constraint.Name != "CountryClaim").ToList()
            }).ToList()
        };
        var emitted = new ScreenplayEmitter().Emit(model, new ScreenplayOptions());

        emitted.Source.ShouldNotContain("reaction batches atomic");
        emitted.Source.ShouldContain("file Authors/Welcoming/Welcoming.cs");
        emitted.Source.ShouldNotContain($"specification {ScenarioName}");
        emitted.Diagnostics.Single(report => report.Code == "SP0070").Message.ShouldContain("constraint 'CountryClaim' is not represented in the document");
    }

    [Fact]
    public void should_keep_legacy_first_event_admission_below_v22()
    {
        GenerateBatch("new AuthorWelcomed(@event.Name, \"Welcome\"), new AuthorFiled(@event.Country)", Constraint("""
            => builder.Unique(unique => unique.On<AuthorWelcomed>(e => e.Name)
                .WithMode(Cratis.Chronicle.Events.Constraints.UniqueConstraintMode.PerValue));
            """));
        AssertWithheld();
        var emitted = new ScreenplayEmitter().Emit(Result.Model, new ScreenplayOptions { MaximumExecutableModelVersion = SemanticVersion.V21 });

        emitted.Source.ShouldNotContain("reaction batches");
        emitted.Source.ShouldContain("produces AuthorWelcomed");
        emitted.Source.ShouldContain($"specification {ScenarioName}");
        emitted.Diagnostics.Where(report => report.Code == "SP0070").ShouldBeEmpty();
    }
}
