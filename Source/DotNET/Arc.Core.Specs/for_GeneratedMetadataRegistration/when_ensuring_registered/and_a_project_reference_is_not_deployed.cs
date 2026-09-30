// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.for_GeneratedMetadataRegistration.when_ensuring_registered;

public class and_a_project_reference_is_not_deployed : given.a_generated_metadata_registration
{
    FileNotFoundException _missing;
    Exception _error;

    void Establish()
    {
        _missing = new("Not deployed", "Contract.Project");
        _loadAssembly(Arg.Is<AssemblyName>(_ => _.Name == "Contract.Project")).Returns(_ => throw _missing);
        _registration.Register(_entryAssembly, Modules(("Contract.Project", () => throw new FileNotFoundException("Not deployed", "Contract.Project"))), []);
    }

    void Because() => _error = Catch.Exception(_registration.EnsureRegistered);

    [Fact] void should_not_fail() => _error.ShouldBeNull();
    [Fact] void should_report_the_project_reference_as_skipped() => _registration.Skipped.Single().ShouldEqual(new SkippedProjectAssembly("Contract.Project", _missing));
}
