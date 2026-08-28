// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Generation;
using Cratis.Screenplay.Generation.DotNet;

namespace Cratis.Arc.Screenplay.for_ArcSpecificationFactAdapter;

public class when_only_an_application_query_exists : Specification
{
    bool _canAnalyze;

    void Because()
    {
        var compilation = Analyzed.Project(
            "Projects",
            [],
            ("Testing/Framework.cs", GeneratedQuerySpecificationSources.Framework),
            ("Projects/Overview/ListProjects/ProjectOverview.cs", GeneratedQuerySpecificationSources.Application));
        var context = new DotNetAnalysisContext([
            SourceProjects.Create("Projects", DotNetProjectRole.Application, compilation)
        ]);
        _canAnalyze = new ArcSpecificationFactAdapter().CanAnalyze(context);
    }

    [Fact] void should_not_analyze_without_a_specification_shaped_type() => _canAnalyze.ShouldBeFalse();
}
