// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Core.Generators.Integration.Specs.Testing;

namespace Cratis.Arc.Core.Generators.Integration.Specs.for_AnalyzerPackageGraph;

/// <summary>
/// Verifies that the packed Arc.Core build targets report the project references of an executable consumer to the
/// generator, whether the consumer references Arc.Core directly or through Cratis.Arc.
/// </summary>
/// <param name="fixture">The shared package-graph fixture.</param>
[Collection(PackageGraphCollection.Name)]
public class when_building_executable_package_consumers(PackageGraphFixture fixture) : Specification
{
    const string LibraryRegistration = "[\"Library\"] = static () => typeof(global::ProjectLibrary.LibraryMarker).Module,";

    readonly PackageGraphFixture _fixture = fixture;

    /// <summary>
    /// Verifies that every executable consumer builds with zero warnings and zero errors.
    /// </summary>
    [Fact]
    public void should_build_every_executable_consumer_cleanly()
    {
        foreach (var consumer in _fixture.ExecutableConsumers)
        {
            consumer.BuildWasClean.ShouldBeTrue();
        }
    }

    /// <summary>
    /// Verifies that the executable gets one generated registration naming a type in its project reference.
    /// </summary>
    [Fact]
    public void should_generate_a_registration_naming_a_type_in_the_project_reference()
    {
        foreach (var consumer in _fixture.ExecutableConsumers.Where(_ => _.Definition.ExpectsProjectReferenceRegistration))
        {
            consumer.GeneratedProjectReferenceRegistrations.Count.ShouldEqual(1);
            consumer.GeneratedProjectReferenceRegistrations.Single().Contains(LibraryRegistration, StringComparison.Ordinal).ShouldBeTrue();
        }
    }

    /// <summary>
    /// Verifies that without transitive project references nothing is generated, so Arc keeps discovering project
    /// assemblies through the runtime dependency context.
    /// </summary>
    [Fact]
    public void should_not_generate_a_registration_without_transitive_project_references()
    {
        foreach (var consumer in _fixture.ExecutableConsumers.Where(_ => !_.Definition.ExpectsProjectReferenceRegistration))
        {
            consumer.GeneratedProjectReferenceRegistrations.ShouldBeEmpty();
        }
    }
}
