// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.for_GeneratedMetadataRegistration.when_ensuring_registered;

public class and_a_project_reference_type_cannot_be_loaded : given.a_generated_metadata_registration
{
    bool _workingModuleReached;
    Exception _error;

    void Establish() => _registration.Register(
        _entryAssembly,
        Modules(
            ("Broken.Project", () => throw new TypeLoadException("Base type lives in a compile-time only assembly")),
            (_initializedAssemblyName, GetWorkingModule)),
        []);

    void Because() => _error = Catch.Exception(_registration.EnsureRegistered);

    Module GetWorkingModule()
    {
        _workingModuleReached = true;
        return _initializedModule;
    }

    [Fact] void should_not_fail() => _error.ShouldBeNull();
    [Fact] void should_load_the_project_reference_by_name_instead() => _loadAssembly.Received(1)(Arg.Is<AssemblyName>(_ => _.Name == "Broken.Project"));
    [Fact] void should_still_reach_the_other_project_references() => _workingModuleReached.ShouldBeTrue();
    [Fact] void should_not_report_anything_as_skipped() => _registration.Skipped.ShouldBeEmpty();
}
