// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.for_GeneratedMetadataRegistration.when_ensuring_registered;

public class and_loading_an_assembly_fails_unexpectedly : given.a_generated_metadata_registration
{
    InvalidOperationException _failure;
    Exception _error;
    Exception _laterError;

    void Establish()
    {
        _failure = new("Unexpected");
        _getDependencyContextProjectNames().Returns(["Broken.Project"]);
        _loadAssembly(Arg.Any<AssemblyName>()).Returns(_ => throw _failure);
    }

    void Because()
    {
        _error = Catch.Exception(_registration.EnsureRegistered);
        _laterError = Catch.Exception(_registration.EnsureRegistered);
    }

    [Fact] void should_propagate_the_failure() => _error.ShouldEqual(_failure);
    [Fact] void should_propagate_the_failure_again_on_later_calls() => _laterError.ShouldEqual(_failure);
    [Fact] void should_not_report_it_as_skipped() => _registration.Skipped.ShouldBeEmpty();
}
