// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Emission;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating.from_projects_in_separate_namespace_roots;

/// <summary>
/// None of several projects is the application, which is why nothing names the domain after one of them. The same
/// holds for the module: a single one would carry a name that belongs to nobody - the solution file, or the neutral
/// default - and would gather every application in the solution beneath it as a feature. The namespaces already say
/// what the modules are.
/// </summary>
public class and_nothing_names_a_module : Specification
{
    string _source;

    void Because() =>
        _source = new ScreenplayGenerator(
                new ApplicationModelAnalyzer(DeclaredUserInterfaceFiles.None),
                new ScreenplayEmitter())
            .Generate(
                [SeparateRootsSource.LibraryProject(), SeparateRootsSource.QuickstartProject()],
                new ScreenplayOptions { Domain = "Samples" })
            .Source;

    [Fact] void should_declare_the_first_namespace_root_as_a_module() => _source.Contains("module Library", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_declare_the_second_namespace_root_as_a_module() => _source.Contains("module Quickstart", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_not_gather_them_under_the_name_the_solution_goes_by() => _source.Contains("module Samples", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_not_turn_either_application_into_a_feature() => _source.Contains("feature Library", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_still_name_the_domain_the_caller_asked_for() => _source.Contains("domain Samples", StringComparison.Ordinal).ShouldBeTrue();
}
