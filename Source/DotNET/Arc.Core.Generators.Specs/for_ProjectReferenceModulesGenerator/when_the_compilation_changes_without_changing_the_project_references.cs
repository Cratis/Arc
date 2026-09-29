// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Generators.Specs.Testing;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Cratis.Arc.Generators.Specs.for_ProjectReferenceModulesGenerator;

public class when_the_compilation_changes_without_changing_the_project_references : given.project_libraries
{
    GeneratorRunResult _result;

    void Because()
    {
        var parseOptions = new CSharpParseOptions(LanguageVersion.Latest);
        var compilation = ProjectReferenceModulesGeneratorRunner.CreateCompilation(OutputKind.ConsoleApplication, parseOptions, ProjectReferenceModulesGeneratorRunner.ProgramSource, [_visibleLibrary.Reference]);
        var driver = ProjectReferenceModulesGeneratorRunner.CreateDriver("1|VisibleLibrary", parseOptions).RunGenerators(compilation);
        var edited = compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText("class Edited;", parseOptions));
        _result = driver.RunGenerators(edited).GetRunResult().Results.Single();
    }

    [Fact] void should_produce_an_unchanged_model() => _result.TrackedSteps[ProjectReferenceModulesGenerator.ModelTrackingName].Single().Outputs.Single().Reason.ShouldEqual(IncrementalStepRunReason.Unchanged);
    [Fact] void should_skip_generating_the_source_again() => _result.TrackedOutputSteps.SelectMany(_ => _.Value).SelectMany(_ => _.Outputs).All(_ => _.Reason == IncrementalStepRunReason.Cached).ShouldBeTrue();
}
