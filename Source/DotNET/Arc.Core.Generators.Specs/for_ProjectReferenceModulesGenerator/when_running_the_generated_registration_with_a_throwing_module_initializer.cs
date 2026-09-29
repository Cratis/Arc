// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Runtime.CompilerServices;
using Cratis.Arc.Generators.Specs.Testing;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Cratis.Arc.Generators.Specs.for_ProjectReferenceModulesGenerator;

public class when_running_the_generated_registration_with_a_throwing_module_initializer : given.project_libraries
{
    /// <summary>
    /// Stands in for <c>Cratis.Arc.ProjectReferenceModuleInitializers</c>, which registers with the process-wide
    /// instance: the generated registration is captured and handed to an instance of its own, so the failure it
    /// provokes does not poison the one other specifications register with.
    /// </summary>
    const string CaptureSource = """
        public static class Capture
        {
            public static System.Reflection.Assembly RegisteringAssembly;
            public static System.Collections.Generic.IReadOnlyDictionary<string, System.Func<System.Reflection.Module>> Modules;
            public static System.Collections.Generic.IEnumerable<string> AssembliesWithoutReachableTypes;

            public static void Register(
                System.Reflection.Assembly registeringAssembly,
                System.Collections.Generic.IReadOnlyDictionary<string, System.Func<System.Reflection.Module>> modules,
                System.Collections.Generic.IEnumerable<string> assembliesWithoutReachableTypes)
            {
                RegisteringAssembly = registeringAssembly;
                Modules = modules;
                AssembliesWithoutReachableTypes = assembliesWithoutReachableTypes;
            }
        }
        """;

    Func<AssemblyName, Assembly> _loadAssembly;
    GeneratedMetadataRegistration _registration;
    Exception _error;
    Exception _laterError;

    void Establish()
    {
        var throwing = CompileWithThrowingModuleInitializer("ThrowingLibrary", "public class Throwing;");
        var generated = ProjectReferenceModulesGeneratorRunner.Run(OutputKind.ConsoleApplication, "1|ThrowingLibrary", throwing);
        generated.Errors.ShouldBeEmpty();

        const string RegisterCall = "global::Cratis.Arc.ProjectReferenceModuleInitializers.Register(";
        generated.Source!.Contains(RegisterCall, StringComparison.Ordinal).ShouldBeTrue();

        var parseOptions = new CSharpParseOptions(LanguageVersion.Latest);
        var compilation = ProjectReferenceModulesGeneratorRunner
            .CreateCompilation(OutputKind.ConsoleApplication, parseOptions, ProjectReferenceModulesGeneratorRunner.ProgramSource + CaptureSource, [throwing.Reference])
            .AddSyntaxTrees(CSharpSyntaxTree.ParseText(generated.Source.Replace(RegisterCall, "global::Capture.Register(", StringComparison.Ordinal), parseOptions));

        var context = new ProjectLibraryLoadContext();
        context.Add(throwing);
        var app = context.Add(new ProjectLibrary("App", ProjectReferenceModulesGeneratorRunner.Emit(compilation)));

        // The executable's own module initializer is what the runtime runs before Main; run it the same way.
        RuntimeHelpers.RunModuleConstructor(app.ManifestModule.ModuleHandle);
        var capture = app.GetType("Capture")!;
        var registeringAssembly = (Assembly)capture.GetField("RegisteringAssembly")!.GetValue(null)!;
        var modules = (IReadOnlyDictionary<string, Func<Module>>)capture.GetField("Modules")!.GetValue(null)!;
        var assembliesWithoutReachableTypes = (IEnumerable<string>)capture.GetField("AssembliesWithoutReachableTypes")!.GetValue(null)!;

        _loadAssembly = Substitute.For<Func<AssemblyName, Assembly>>();
        _registration = new(_loadAssembly, () => [], () => registeringAssembly);
        _registration.Register(registeringAssembly, modules, assembliesWithoutReachableTypes);
    }

    void Because()
    {
        _error = Catch.Exception(_registration.EnsureRegistered);
        _laterError = Catch.Exception(_registration.EnsureRegistered);
    }

    [Fact] void should_fail_with_a_type_initialization_exception() => _error.ShouldBeOfExactType<TypeInitializationException>();
    [Fact] void should_carry_the_failure_of_the_module_initializer() => _error.InnerException.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_fail_again_with_the_same_exception_on_later_calls() => _laterError.ShouldEqual(_error);
    [Fact] void should_not_load_the_project_reference_by_name() => _loadAssembly.DidNotReceiveWithAnyArgs()(default!);
    [Fact] void should_not_report_the_project_reference_as_skipped() => _registration.Skipped.ShouldBeEmpty();
}
