// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_slice_rejection : a_generated_document
{
    [Theory]
    [InlineData("_outcome = await _slice.Run()", "_outcome.ShouldBeRejected()", "", SliceRejectionSources.Members)]
    [InlineData("await _slice.Run()", "_slice.Then.ShouldBeRejected()", "", SliceRejectionSources.Members)]
    [InlineData("_outcome = await _slice.Run()", "_slice.Then.ShouldBeRejected()", "_slice = new(new RegisterAuthor(\"\"));", "Slice<RegisterAuthor> _slice = null!; SliceOutcome<RegisterAuthor> _outcome = null!;")]
    [InlineData("this._outcome = await this._slice.Run()", "this._outcome.ShouldBeRejected()", "this._slice = new(new RegisterAuthor(\"\"));", "Slice<RegisterAuthor> _slice = null!; SliceOutcome<RegisterAuthor> _outcome = null!;")]
    [InlineData("await this._slice.Run()", "this._slice.Then.ShouldBeRejected()", "", SliceRejectionSources.Members)]
    [InlineData("_outcome = await _slice.Run()", "_outcome.ShouldBeRejected()", "", SliceRejectionSources.Members + " async Task Destroy() => await _slice.DisposeAsync();")]
    [InlineData("_outcome = await _slice.Run()", "_outcome.ShouldBeRejected()", "", SliceRejectionSources.Members + " async Task Destroy() { await this._slice.DisposeAsync(); }")]
    public void should_preserve_the_own_command_and_unnamed_rejection(string action, string assertion, string setup, string members)
    {
        var sources = SliceRejectionSources.With(action, assertion, setup, members);
        Analyzed.ErrorsIn(sources).ShouldBeEmpty();
        Generate(sources);
        Result.Source.ShouldContain("specification WhenRegisteringWithAnEmptyName");
        Result.Source.ShouldContain("when RegisterAuthor");
        Result.Source.ShouldContain("then error");
        var specification = Result.Model.Slices.SelectMany(slice => slice.Specifications).Single();
        specification.When!.Name.ShouldEqual("RegisterAuthor");
        specification.Errors.Single().ShouldEqual(string.Empty);
        AssertDocument();
    }

    [Fact]
    public void should_preserve_an_inherited_establish_construction()
    {
        var sources = SliceRejectionSources.WithInheritedSetup();
        Analyzed.ErrorsIn(sources).ShouldBeEmpty();
        Generate(sources);
        Result.Source.ShouldContain("specification WhenRegisteringWithAnEmptyName");
        var specification = Result.Model.Slices.SelectMany(slice => slice.Specifications).Single();
        specification.When!.Name.ShouldEqual("RegisterAuthor");
        specification.Errors.Single().ShouldEqual(string.Empty);
        AssertDocument();
    }
}
