// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_two_aggregate_behaviors_producing_the_same_event : a_generated_document
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_merge_duplicate_productions_without_promoting_eligibility(bool reverse)
    {
        var calls = reverse ? "await other.RegisterAgain(Name); await author.Register(Name);" : "await author.Register(Name); await other.RegisterAgain(Name);";
        var source = AggregateRoutingSources.With($$"""
            public async Task Handle(IAggregateRootFactory factory)
            {
                var author = await factory.Get<Author>(Id);
                var other = await factory.Get<Author>(OtherId);
                {{calls}}
                await author.Commit();
                await other.Commit();
            }
            """).Replace(
                "public void OnRegistered",
                "public Task RegisterAgain(string name) { if (name.Length == 0) { } return Apply(new AuthorRegistered(name)); } public void OnRegistered",
                StringComparison.Ordinal);
        Generate((Analyzed.SlicePath, source));
        var production = Result.Model.Slices.SelectMany(slice => slice.Commands).Single().Produces.Single();
        production.CanInline.ShouldBeFalse();
        production.UsesCommandContext.ShouldBeFalse();
        Result.Source.Split("produces AuthorRegistered", StringSplitOptions.None).Length.ShouldEqual(2);
        Result.Source.ShouldNotContain("produces event AuthorRegistered");
        Result.Source.ShouldNotContain("for id");
        AssertDocument();
    }
}
