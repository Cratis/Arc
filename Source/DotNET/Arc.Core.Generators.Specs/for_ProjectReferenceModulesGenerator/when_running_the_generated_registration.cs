// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Runtime.CompilerServices;
using Cratis.Arc.Generators.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Generators.Specs.for_ProjectReferenceModulesGenerator;

public class when_running_the_generated_registration : given.project_libraries
{
    Assembly _app;
    Exception _error;

    void Establish()
    {
        var result = ProjectReferenceModulesGeneratorRunner.Run(OutputKind.ConsoleApplication, "2|VisibleLibrary|HiddenLibrary", _visibleLibrary, _hiddenLibrary);
        var context = new ProjectLibraryLoadContext();
        context.Add(_visibleLibrary);
        _app = context.Add(new ProjectLibrary("App", ProjectReferenceModulesGeneratorRunner.Emit(result.Compilation)));
    }

    void Because()
    {
        // The executable's own module initializer is what the runtime runs before Main; run it the same way.
        RuntimeHelpers.RunModuleConstructor(_app.ManifestModule.ModuleHandle);
        _error = Catch.Exception(GeneratedMetadataRegistration.Default.EnsureRegistered);
    }

    [Fact] void should_not_fail() => _error.ShouldBeNull();
    [Fact] void should_run_the_module_initializer_of_the_project_reference() => (AppContext.GetData(ModuleInitializerRanKey) is true).ShouldBeTrue();
    [Fact] void should_report_the_project_reference_that_could_not_be_loaded_by_name() => GeneratedMetadataRegistration.Default.Skipped.Select(_ => _.Name).ShouldContain("HiddenLibrary");
}
