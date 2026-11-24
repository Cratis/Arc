// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator;

public class when_capping_declared_graph_projections_below_v38 : given.a_generated_document
{
    void Because() => Generate(new ScreenplayOptions { MaximumExecutableModelVersion = SemanticVersion.V37 },
        given.GraphProjectionDeclarationSources.Files(given.GraphProjectionDeclarationSources.Marker()));

    [Fact] void should_keep_a_compilable_document() => AssertDocument();
    [Fact] void should_omit_the_graph() => Bound.Value.Model.Application.Graphs.ShouldBeEmpty();
    [Fact] void should_omit_explicit_projections() => Bound.Value.Model.Application.GraphProjections.ShouldBeEmpty();
    [Fact] void should_report_the_version_loss() => Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.GraphRequiresV38).ShouldBeTrue();
}
