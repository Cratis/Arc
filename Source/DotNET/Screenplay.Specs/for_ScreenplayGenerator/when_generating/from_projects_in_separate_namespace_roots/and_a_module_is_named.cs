// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Emission;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating.from_projects_in_separate_namespace_roots;

/// <summary>
/// Taking the modules from the namespaces is what happens when nobody says otherwise, not what happens regardless.
/// A caller that names a module has said the whole document is that module, and gathering the applications beneath
/// it is then the answer rather than the accident.
/// </summary>
public class and_a_module_is_named : Specification
{
    string _source;

    void Because() =>
        _source = new ScreenplayGenerator(
                new ApplicationModelAnalyzer(DeclaredUserInterfaceFiles.None),
                new ScreenplayEmitter())
            .Generate(
                [SeparateRootsSource.LibraryProject(), SeparateRootsSource.QuickstartProject()],
                new ScreenplayOptions { Domain = "Samples", Module = "Everything" })
            .Source;

    [Fact] void should_declare_the_module_that_was_named() => _source.Contains("module Everything", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_not_take_a_module_from_the_namespaces() => _source.Contains("module Library", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_gather_the_applications_beneath_it() => _source.Contains("feature Library", StringComparison.Ordinal).ShouldBeTrue();
}
