// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_recovering_rendered_specifications.given;

public class a_rendered_query_scenario : for_ScreenplayGenerator.given.a_generated_document
{
    protected static readonly ScreenplayOptions Options = new() { Module = "Projects", SegmentsToSkip = 1 };

    protected void GenerateScenario(string scenario, string? companion = null) =>
        Generate(Options, RenderedQuerySources.Sources(scenario, companion));
}
