// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Core.Generators.Integration.Specs.Testing;

namespace Cratis.Arc.Core.Generators.Integration.Specs.for_AnalyzerPackageGraph;

/// <summary>
/// Verifies physical analyzer ownership and complete dependency metadata in the freshly packed nupkgs.
/// </summary>
/// <param name="fixture">The shared package-graph fixture.</param>
[Collection(PackageGraphCollection.Name)]
public class when_inspecting_packed_packages(PackageGraphFixture fixture) : Specification
{
    static readonly string[] _runtimeFrameworks = ["net8.0", "net9.0", "net10.0"];
    static readonly string[] _codeAnalysisFrameworks = [".NETStandard2.0"];

    readonly PackageGraphFixture _fixture = fixture;

    /// <summary>
    /// Verifies standalone ARC analyzer and code-fix ownership.
    /// </summary>
    [Fact]
    public void should_make_the_standalone_arc_package_the_only_owner_of_arc_analyzer_and_code_fix()
    {
        var expectedEntries = new[]
        {
            "analyzers/dotnet/cs/Cratis.Arc.Core.CodeAnalysis.dll",
            "analyzers/dotnet/cs/Cratis.Arc.Core.CodeAnalysis.CodeFixes.dll"
        };
        var entries = AnalyzerEntries("Cratis.Arc.Core.CodeAnalysis");

        entries.Length.ShouldEqual(expectedEntries.Length);
        foreach (var expectedEntry in expectedEntries)
        {
            entries.ShouldContain(expectedEntry);
        }
    }

    /// <summary>
    /// Verifies standalone ARCCHR analyzer and code-fix ownership.
    /// </summary>
    [Fact]
    public void should_make_the_standalone_chronicle_package_the_only_owner_of_chronicle_analyzer_and_code_fix()
    {
        var expectedEntries = new[]
        {
            "analyzers/dotnet/cs/Cratis.Arc.Chronicle.CodeAnalysis.dll",
            "analyzers/dotnet/cs/Cratis.Arc.Chronicle.CodeAnalysis.CodeFixes.dll"
        };
        var entries = AnalyzerEntries("Cratis.Arc.Chronicle.CodeAnalysis");

        entries.Length.ShouldEqual(expectedEntries.Length);
        foreach (var expectedEntry in expectedEntries)
        {
            entries.ShouldContain(expectedEntry);
        }
    }

    /// <summary>
    /// Verifies physical generator ownership in Arc.Core.
    /// </summary>
    [Fact]
    public void should_make_arc_core_the_only_runtime_package_with_a_physical_generator()
    {
        var entries = AnalyzerEntries("Cratis.Arc.Core");

        entries.Length.ShouldEqual(1);
        entries.ShouldContain("analyzers/dotnet/cs/Cratis.Arc.Core.Generators.dll");
    }

    /// <summary>
    /// Verifies that aggregate packages do not physically forward analyzer assemblies.
    /// </summary>
    [Fact]
    public void should_not_physically_forward_analyzers_through_aggregate_packages()
    {
        foreach (var packageId in new[] { "Cratis.Arc", "Cratis.Arc.Chronicle", "Cratis", "Cratis.CodeAnalysis" })
        {
            AnalyzerEntries(packageId).ShouldBeEmpty();
        }
    }

    /// <summary>
    /// Verifies that analyzer dependencies use the unique integration version and include analyzer assets.
    /// </summary>
    [Fact]
    public void should_flow_analyzer_dependencies_at_the_unique_integration_version()
    {
        AssertAnalyzerDependency("Cratis.Arc.Core", "Cratis.Arc.Core.CodeAnalysis", _runtimeFrameworks);
        AssertAnalyzerDependency("Cratis.Arc.Chronicle", "Cratis.Arc.Chronicle.CodeAnalysis", _runtimeFrameworks);
        AssertAnalyzerDependency("Cratis.CodeAnalysis", "Cratis.Arc.Core.CodeAnalysis", _codeAnalysisFrameworks);
        AssertAnalyzerDependency("Cratis.CodeAnalysis", "Cratis.Arc.Chronicle.CodeAnalysis", _codeAnalysisFrameworks);
    }

    /// <summary>
    /// Verifies that ordinary runtime edges do not forward analyzer assets directly.
    /// </summary>
    [Fact]
    public void should_exclude_analyzer_flow_from_ordinary_runtime_edges()
    {
        AssertRuntimeDependency("Cratis.Arc", "Cratis.Arc.Core");
        AssertRuntimeDependency("Cratis.Arc.Chronicle", "Cratis.Arc.Core");
        AssertRuntimeDependency("Cratis", "Cratis.Arc");
        AssertRuntimeDependency("Cratis", "Cratis.Arc.Chronicle");
    }

    /// <summary>
    /// Verifies that Cratis.CodeAnalysis has only the two standalone analyzer package dependencies.
    /// </summary>
    [Fact]
    public void should_make_cratis_code_analysis_a_dependency_only_metapackage()
    {
        var package = _fixture.Packages["Cratis.CodeAnalysis"];

        package.Entries.Any(_ => _.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)).ShouldBeFalse();
        package.Dependencies.Count.ShouldEqual(2);
        package.Dependencies.Any(_ => _.Id == "Cratis.Arc.Core.CodeAnalysis").ShouldBeTrue();
        package.Dependencies.Any(_ => _.Id == "Cratis.Arc.Chronicle.CodeAnalysis").ShouldBeTrue();
        package.Dependencies.Any(_ => _.Id == "System.Text.Json").ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that the Cratis package retains the proxy-generator build dependency.
    /// </summary>
    [Fact]
    public void should_retain_the_proxy_generator_build_dependency()
    {
        foreach (var dependency in AssertDependencyGroups("Cratis", "Cratis.Arc.ProxyGenerator.Build", _runtimeFrameworks))
        {
            dependency.Version.ShouldEqual(_fixture.PackageVersion);
            dependency.Includes("BuildTransitive").ShouldBeTrue();
        }
    }

    void AssertAnalyzerDependency(string packageId, string dependencyId, IReadOnlyCollection<string> expectedFrameworks)
    {
        foreach (var dependency in AssertDependencyGroups(packageId, dependencyId, expectedFrameworks))
        {
            dependency.Version.ShouldEqual(_fixture.PackageVersion);
            dependency.Includes("Analyzers").ShouldBeTrue();
        }
    }

    void AssertRuntimeDependency(string packageId, string dependencyId)
    {
        foreach (var dependency in AssertDependencyGroups(packageId, dependencyId, _runtimeFrameworks))
        {
            dependency.Version.ShouldEqual(_fixture.PackageVersion);
            dependency.Excludes("Analyzers").ShouldBeTrue();
        }
    }

    PackageDependency[] AssertDependencyGroups(
        string packageId,
        string dependencyId,
        IReadOnlyCollection<string> expectedFrameworks)
    {
        var dependencies = Dependencies(packageId, dependencyId);

        dependencies.Length.ShouldEqual(expectedFrameworks.Count);
        foreach (var framework in expectedFrameworks)
        {
            dependencies.Count(_ => string.Equals(_.TargetFramework, framework, StringComparison.Ordinal)).ShouldEqual(1);
        }

        return dependencies;
    }

    PackageDependency[] Dependencies(string packageId, string dependencyId) =>
        _fixture.Packages[packageId].Dependencies
            .Where(_ => string.Equals(_.Id, dependencyId, StringComparison.Ordinal))
            .ToArray();

    string[] AnalyzerEntries(string packageId) =>
        _fixture.Packages[packageId].Entries
            .Where(_ => _.StartsWith("analyzers/dotnet/cs/", StringComparison.Ordinal))
            .ToArray();
}
