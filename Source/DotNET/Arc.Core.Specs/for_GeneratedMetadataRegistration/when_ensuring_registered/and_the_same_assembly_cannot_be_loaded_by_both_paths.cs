// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.for_GeneratedMetadataRegistration.when_ensuring_registered;

public class and_the_same_assembly_cannot_be_loaded_by_both_paths : given.a_generated_metadata_registration
{
    FileNotFoundException _missing;

    void Establish()
    {
        _missing = new("Not deployed", "Missing.Project");
        _loadAssembly(Arg.Any<AssemblyName>()).Returns(_ => throw _missing);

        // The registering executable is not the entry assembly, so the dependency context is consulted as well as the
        // generated registration, and each names the assembly, in a different case.
        _getDependencyContextProjectNames().Returns(["missing.project"]);
        _registration.Register(_otherExecutable, Modules(), ["Missing.Project"]);
    }

    void Because()
    {
        _registration.EnsureRegistered();
        _registration.EnsureRegistered();
    }

    [Fact] void should_try_to_load_it_by_both_paths() => _loadAssembly.Received(2)(Arg.Any<AssemblyName>());
    [Fact] void should_report_it_as_skipped_once() => _registration.Skipped.Single().ShouldEqual(new SkippedProjectAssembly("Missing.Project", _missing));
}
