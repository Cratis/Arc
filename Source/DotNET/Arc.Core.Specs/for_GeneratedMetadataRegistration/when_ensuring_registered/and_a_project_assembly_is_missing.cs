// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.for_GeneratedMetadataRegistration.when_ensuring_registered;

public class and_a_project_assembly_is_missing : given.a_generated_metadata_registration
{
    FileNotFoundException _missing;
    Exception _error;

    void Establish()
    {
        _missing = new("Not deployed", "Missing.Project");
        _getDependencyContextProjectNames().Returns(["Missing.Project", "Present.Project"]);
        _loadAssembly(Arg.Is<AssemblyName>(_ => _.Name == "Missing.Project")).Returns(_ => throw _missing);
    }

    void Because() => _error = Catch.Exception(_registration.EnsureRegistered);

    [Fact] void should_not_fail() => _error.ShouldBeNull();
    [Fact] void should_report_the_missing_assembly() => _registration.Skipped.Single().ShouldEqual(new SkippedProjectAssembly("Missing.Project", _missing));
    [Fact] void should_still_load_the_other_project_libraries() => _loadAssembly.Received(1)(Arg.Is<AssemblyName>(_ => _.Name == "Present.Project"));
}
