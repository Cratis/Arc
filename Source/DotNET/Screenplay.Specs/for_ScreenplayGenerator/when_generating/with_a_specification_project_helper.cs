// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ArcSpecificationFactAdapter;
using Cratis.Screenplay.Generation.DotNet;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class with_a_specification_project_helper : Specification
{
    const string Application = """
        using Cratis.Chronicle.Events;
        using Cratis.Concepts;
        namespace Library.Authors.Registration;
        public record ApplicationId(string Value) : ConceptAs<string>(Value);
        [EventType]
        public record AuthorRegistered(string Name);
        """;

    const string Specifications = """
        using Cratis.Chronicle.Events;
        using Cratis.Concepts;
        namespace Library.Specifications.Fixtures;
        public record HelperId(string Value) : ConceptAs<string>(Value);
        public enum HelperState { Unknown, Known }
        public record ReferencedSpecId(int Value) : ConceptAs<int>(Value);
        [EventType]
        public record FixtureEvent(ReferencedSpecId Id);
        """;

    DotNetProjectCompilation _application;
    DotNetProjectCompilation _specifications;
    ScreenplayGenerationResult _result;

    void Establish()
    {
        var application = Analyzed.Project("Library", [], ("Library/Authors/Registration/Registration.cs", Application));
        var specifications = Analyzed.Project("Library.Specifications", [], ("Library/Specifications/Fixtures/Fixture.cs", Specifications));
        _application = SourceProjects.Create("Library", DotNetProjectRole.Application, application);
        _specifications = SourceProjects.Create("Library.Specifications", DotNetProjectRole.Specifications, specifications);
    }

    void Because() => _result = new ScreenplayGenerator().Generate([_application, _specifications], new ScreenplayOptions { Domain = "Library", Module = "Library" });

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(_application.Compilation).Concat(Analyzed.ErrorsIn(_specifications.Compilation)).ShouldBeEmpty();
    [Fact] void should_recover_unused_application_declarations_and_referenced_specification_types_only() => _result.Model.Concepts.Select(concept => concept.Name).ShouldContainOnly(["ApplicationId", "ReferencedSpecId"]);
    [Fact] void should_preserve_use_driven_analysis_of_specification_project_artifacts() => _result.Source.ShouldContain("event FixtureEvent");
    [Fact] void should_report_nothing() => _result.Diagnostics.ShouldBeEmpty();

    [Fact]
    public void should_also_respect_the_role_of_a_single_project()
    {
        var result = new ScreenplayGenerator().Generate(_specifications, new ScreenplayOptions { Domain = "Library", Module = "Library" });
        result.Model.Concepts.Select(concept => concept.Name).ShouldContainOnly(["ReferencedSpecId"]);
        result.Source.ShouldContain("event FixtureEvent");
        result.Diagnostics.ShouldBeEmpty();
    }
}
