// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Runtime.CompilerServices;
using Cratis.Arc.Generators.Specs.Testing;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Cratis.Arc.Generators.Specs.for_ProjectReferenceModulesGenerator;

public class when_running_the_generated_registration : given.project_libraries
{
    readonly string _reachedKey = $"Reached.{Guid.NewGuid():N}";
    readonly string _loadedByNameKey = $"LoadedByName.{Guid.NewGuid():N}";
    readonly string _derivedKey = $"Derived.{Guid.NewGuid():N}";
    ProjectLibraryLoadContext _context;
    Assembly _app;
    Exception _error;

    void Establish()
    {
        var reached = CompileWithModuleInitializer("ReachedLibrary", "public class Reached;", _reachedKey);
        var loadedByName = CompileWithModuleInitializer("LoadedByNameLibrary", "internal class Hidden;", _loadedByNameKey);

        // Its only public type derives from a type in an assembly that is not deployed, so naming it fails and the
        // project reference is loaded by name instead.
        var derived = CompileWithModuleInitializer("DerivedWithInitializer", "public class Derived : CompileOnlyLibrary.Base;", _derivedKey, _compileOnlyLibrary);
        var result = ProjectReferenceModulesGeneratorRunner.Run(
            OutputKind.ConsoleApplication,
            "5|ReachedLibrary|LoadedByNameLibrary|DerivedWithInitializer|NotDeployedLibrary|HiddenLibrary",
            LanguageVersion.Latest,
            [reached.Reference, loadedByName.Reference, derived.Reference, _packageLibrary.Reference, _hiddenLibrary.Reference, _compileOnlyLibrary.Reference, ProjectLibrary.Compile("NotDeployedLibrary", "namespace NotDeployedLibrary { public class NotDeployed; }").Reference]);
        result.Errors.ShouldBeEmpty();

        _context = new ProjectLibraryLoadContext();
        _context.Add(reached);
        _context.Add(loadedByName);
        _context.Add(derived);
        _app = _context.Add(new ProjectLibrary("App", ProjectReferenceModulesGeneratorRunner.Emit(result.Compilation)));
    }

    void Because()
    {
        // Loading by name resolves through the contextual reflection context, as it would in the default context of
        // a real executable.
        using var scope = _context.EnterContextualReflection();

        // The executable's own module initializer is what the runtime runs before Main; run it the same way.
        RuntimeHelpers.RunModuleConstructor(_app.ManifestModule.ModuleHandle);
        _error = Catch.Exception(GeneratedMetadataRegistration.Default.EnsureRegistered);
    }

    [Fact] void should_not_fail() => _error.ShouldBeNull();
    [Fact] void should_run_the_module_initializer_of_a_project_reference_it_names_a_type_in() => (AppContext.GetData(_reachedKey) is true).ShouldBeTrue();
    [Fact] void should_run_the_module_initializer_of_a_project_reference_without_accessible_types() => (AppContext.GetData(_loadedByNameKey) is true).ShouldBeTrue();
    [Fact] void should_run_the_module_initializer_of_a_project_reference_whose_type_cannot_be_loaded() => (AppContext.GetData(_derivedKey) is true).ShouldBeTrue();
    [Fact] void should_report_the_project_reference_that_is_not_deployed() => GeneratedMetadataRegistration.Default.Skipped.Select(_ => _.Name).ShouldContain("NotDeployedLibrary");
    [Fact] void should_report_the_project_reference_that_could_not_be_loaded_by_name() => GeneratedMetadataRegistration.Default.Skipped.Select(_ => _.Name).ShouldContain("HiddenLibrary");
}
