// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_unsupported_slice_rejections : a_generated_document
{
    [Theory]
    [InlineData("_outcome = await _slice.Run()", "_outcome.ShouldBeRejected(\"invalid\")", "", SliceRejectionSources.Members)]
    [InlineData("_outcome = await _slice.Run()", "_outcome.ShouldBeDenied()", "", SliceRejectionSources.Members)]
    [InlineData("_outcome = await _slice.Run()", "_outcome.ShouldHaveAppendedEvent<AuthorRegistered>(default).GetAwaiter().GetResult()", "", SliceRejectionSources.Members)]
    [InlineData("_outcome = await _slice.Run()", "_outcome.ShouldBeRejected()", "_slice.GivenCaller(new ClaimsPrincipal());", SliceRejectionSources.Members)]
    [InlineData("if (true) _outcome = await _slice.Run()", "_outcome.ShouldBeRejected()", "", SliceRejectionSources.Members)]
    [InlineData("_outcome = await _slice.Run(); await _slice.Run()", "_outcome.ShouldBeRejected()", "", SliceRejectionSources.Members)]
    [InlineData("_outcome = await _slice.Run()", "_other.Then.ShouldBeRejected()", "", SliceRejectionSources.Members + " readonly Slice<RegisterAuthor> _other = new(new RegisterAuthor(\"other\"));")]
    [InlineData("_outcome = await _slice.Run()", "_outcome.ShouldBeRejected()", "_slice = new(new RegisterAuthor(\"changed\"));", "Slice<RegisterAuthor> _slice = new(new RegisterAuthor(\"\")); SliceOutcome<RegisterAuthor> _outcome = null!;")]
    [InlineData("_outcome = await _slice.Run(); _outcome = _other.Then", "_outcome.ShouldBeRejected()", "", SliceRejectionSources.Members + " readonly Slice<RegisterAuthor> _other = new(new RegisterAuthor(\"other\"));")]
    [InlineData("_outcome = await _slice.Run()", "_outcome.ShouldBeRejected()", "", "Slice<RegisterAuthor> _slice = null!; SliceOutcome<RegisterAuthor> _outcome = null!; void Build() => _slice = new(new RegisterAuthor(\"\"));")]
    [InlineData("_outcome = await _slice.Run()", "_outcome.ShouldBeRejected()", "", "Slice<RegisterAuthor> _slice = null!; SliceOutcome<RegisterAuthor> _outcome = null!; System.Action Build() => () => _slice = new(new RegisterAuthor(\"\"));")]
    [InlineData("_outcome = await _slice.Run()", "_outcome.ShouldBeRejected()", "", "readonly Slice<RegisterAuthor> _slice = new(new RegisterAuthor(\"\")) { Scenario = { Services = { new Microsoft.Extensions.DependencyInjection.ServiceDescriptor(typeof(object), new object()) } } }; SliceOutcome<RegisterAuthor> _outcome = null!;")]
    [InlineData("_outcome = await _slice.Run()", "_outcome.ShouldBeRejected()", "_slice = new(new RegisterAuthor(\"\")) { Scenario = { Services = { new Microsoft.Extensions.DependencyInjection.ServiceDescriptor(typeof(object), new object()) } } };", "Slice<RegisterAuthor> _slice = null!; SliceOutcome<RegisterAuthor> _outcome = null!;")]
    [InlineData("_outcome = await _slice.Run()", "_outcome.ShouldBeRejected()", "", "readonly Slice<RegisterAuthor> _slice = new(new RegisterAuthor(\"\")) { Scenario = { Context = { [\"caller\"] = \"hidden\" } } }; SliceOutcome<RegisterAuthor> _outcome = null!;")]
    [InlineData("_outcome = await _slice.Run()", "_outcome.ShouldBeRejected()", "_slice = new(new RegisterAuthor(\"\")) { Scenario = { Context = { [\"caller\"] = \"hidden\" } } };", "Slice<RegisterAuthor> _slice = null!; SliceOutcome<RegisterAuthor> _outcome = null!;")]
    [InlineData("_outcome = await _slice.Run()", "_outcome.ShouldBeRejected()", "", "readonly Slice<RegisterAuthor> _slice = new(new RegisterAuthor(\"\")) { }; SliceOutcome<RegisterAuthor> _outcome = null!;")]
    [InlineData("_outcome = await _slice.Run()", "_outcome.ShouldBeRejected()", "_slice = new(new RegisterAuthor(\"\")) { };", "Slice<RegisterAuthor> _slice = null!; SliceOutcome<RegisterAuthor> _outcome = null!;")]
    [InlineData("_outcome = await _slice.Run()", "_outcome.ShouldBeRejected()", "", SliceRejectionSources.Members + " object InspectSlice() => _slice;")]
    [InlineData("_outcome = await _slice.Run()", "_outcome.ShouldBeRejected()", "", SliceRejectionSources.Members + " object InspectOutcome() => _outcome;")]
    [InlineData("_outcome = await _slice.Run()", "_outcome.ShouldBeRejected()", "", SliceRejectionSources.Members + " async Task Destroy() { _ = _slice.Then; await _slice.DisposeAsync(); }")]
    [InlineData("_outcome = await _slice.Run()", "_outcome.ShouldBeRejected()", "", SliceRejectionSources.Members + " async Task Destroy() { await _slice.DisposeAsync(); _ = _outcome; }")]
    public void should_withhold_the_whole_scenario(string action, string assertion, string setup, string members)
    {
        var sources = SliceRejectionSources.With(action, assertion, setup, members);
        Analyzed.ErrorsIn(sources).ShouldBeEmpty();
        Generate(sources);
        Result.Model.Slices.SelectMany(slice => slice.Specifications).ShouldBeEmpty();
        Result.Source.ShouldNotContain("specification WhenRegisteringWithAnEmptyName");
        Result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableSpecification);
        RoundTrip.Errors.ShouldBeEmpty();
        RoundTrip.IsStable.ShouldBeTrue();
    }

    [Fact]
    public void should_withhold_inherited_extra_setup()
    {
        var sources = SliceRejectionSources.WithInheritedSetup("_slice.GivenCaller(new ClaimsPrincipal());");
        Analyzed.ErrorsIn(sources).ShouldBeEmpty();
        Generate(sources);
        Result.Model.Slices.SelectMany(slice => slice.Specifications).ShouldBeEmpty();
        Result.Source.ShouldNotContain("specification WhenRegisteringWithAnEmptyName");
        Result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableSpecification);
        RoundTrip.Errors.ShouldBeEmpty();
        RoundTrip.IsStable.ShouldBeTrue();
    }

    [Fact]
    public void should_not_recognize_a_same_named_foreign_slice()
    {
        var sources = SliceRejectionSources.With().Select(source =>
            (source.Path, source.Text.Replace("Cratis.Arc.Chronicle.Testing.Slices", "Foreign.Testing.Slices", StringComparison.Ordinal))).ToArray();
        Analyzed.ErrorsIn(sources).ShouldBeEmpty();
        Generate(sources);
        Result.Model.Slices.SelectMany(slice => slice.Specifications).ShouldBeEmpty();
        Result.Source.ShouldNotContain("specification WhenRegisteringWithAnEmptyName");
    }
}
