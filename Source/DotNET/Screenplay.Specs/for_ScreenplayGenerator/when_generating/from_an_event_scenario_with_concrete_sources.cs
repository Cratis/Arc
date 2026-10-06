// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

/// <summary>
/// Concrete occurrence sources survive as for values, whether shared or distinct.
/// </summary>
public class from_an_event_scenario_with_concrete_sources : a_generated_document
{
    [Theory]
    [InlineData("prior", false)]
    [InlineData("current", false)]
    [InlineData("prior", true)]
    public void should_state_each_occurrences_source(string given, bool direct)
    {
        Generate(
            (Analyzed.SlicePath, EventAppendSources.Producer),
            ("Library/Feature/Slice/when_appending/and_it_succeeds.cs", EventAppendSources.With($"new EventSourceId(\"{given}\")", "new EventSourceId(\"current\")", "new EventSourceId(\"current\")", direct)),
            (IntegrationTesting.Path, IntegrationTesting.Source));

        Result.Source.ShouldContain($"given AuthorRegistered\n          for \"{given}\"");
        Result.Source.ShouldContain("when append AuthorRegistered\n          for \"current\"");
        Result.Source.ShouldContain("then AuthorRegistered\n          for \"current\"");
        Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableSpecification).ShouldBeFalse();
        AssertDocument();
    }
}
