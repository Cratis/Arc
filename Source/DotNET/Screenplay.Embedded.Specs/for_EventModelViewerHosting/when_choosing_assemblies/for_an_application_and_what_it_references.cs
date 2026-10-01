// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Screenplay.Embedded.for_EventModelEndpoints.given;
using Cratis.Arc.Screenplay.Embedded.for_EventModelViewerHosting.given;
using Cratis.Arc.Screenplay.Embedded.Hosting;

namespace Cratis.Arc.Screenplay.Embedded.for_EventModelViewerHosting.when_choosing_assemblies;

public class for_an_application_and_what_it_references : Specification
{
    static readonly Assembly _entryAssembly = typeof(for_an_application_and_what_it_references).Assembly;
    static readonly Assembly _referencedApplication = typeof(Company.Library.Program).Assembly;

    Assembly _loadedWithoutBeingReferenced;
    IReadOnlyList<Assembly> _assemblies;
    IReadOnlyList<string> _names;

    void Establish() => _loadedWithoutBeingReferenced = an_application_assembly.BuiltForDebugging();

    void Because()
    {
        _assemblies = EventModelViewerAssemblies.For(
            _entryAssembly,
            [.. AppDomain.CurrentDomain.GetAssemblies(), _loadedWithoutBeingReferenced],
            [an_embedded_application.Assembly]);

        _names = [.. _assemblies.Select(assembly => assembly.GetName().Name ?? string.Empty)];
    }

    [Fact] void should_serve_the_application_itself() => _assemblies.Contains(_entryAssembly).ShouldBeTrue();
    [Fact] void should_serve_an_assembly_the_application_references() => _assemblies.Contains(_referencedApplication).ShouldBeTrue();
    [Fact] void should_serve_an_assembly_the_host_named_itself() => _assemblies.Contains(an_embedded_application.Assembly).ShouldBeTrue();
    [Fact] void should_serve_each_assembly_once() => _names.Distinct(StringComparer.Ordinal).Count().ShouldEqual(_names.Count);
    [Fact] void should_not_serve_an_assembly_that_is_merely_loaded() => _assemblies.Contains(_loadedWithoutBeingReferenced).ShouldBeFalse();

    [Fact] void should_not_serve_framework_assemblies() => _names
        .Any(name => name.StartsWith("System", StringComparison.Ordinal) || name.StartsWith("Microsoft", StringComparison.Ordinal))
        .ShouldBeFalse();

    [Fact] void should_not_serve_cratis_assemblies_other_than_the_application_itself() => _names
        .Any(name => !string.Equals(name, _entryAssembly.GetName().Name, StringComparison.Ordinal) &&
            name.StartsWith("Cratis.", StringComparison.Ordinal))
        .ShouldBeFalse();
}
