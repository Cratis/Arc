// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Core.Generators.Integration.Specs.Testing;

namespace Cratis.Arc.Core.Generators.Integration.Specs.for_AnalyzerPackageGraph;

/// <summary>
/// Verifies that the SDK Workspaces host discovers and applies code fixes from the packed analyzer packages.
/// </summary>
/// <param name="fixture">The shared package-graph fixture.</param>
[Collection(PackageGraphCollection.Name)]
public class when_discovering_code_fixes_through_the_sdk_host(PackageGraphFixture fixture) : Specification
{
    readonly PackageGraphFixture _fixture = fixture;

    /// <summary>
    /// Verifies ARC code-fix discovery and application.
    /// </summary>
    [Fact]
    public void should_discover_and_apply_the_arc_code_fix()
    {
        _fixture.ArcCodeFixResult.DiagnosticId.ShouldEqual("ARC0011");
        _fixture.ArcCodeFixResult.HostReportedFormattedFile.ShouldBeTrue();
        _fixture.ArcCodeFixResult.SourceWasRewritten.ShouldBeTrue();
    }

    /// <summary>
    /// Verifies ARCCHR0008 code-fix discovery and application.
    /// </summary>
    [Fact]
    public void should_discover_and_apply_the_arcchr0008_code_fix()
    {
        _fixture.ChronicleCodeFixResult.DiagnosticId.ShouldEqual("ARCCHR0008");
        _fixture.ChronicleCodeFixResult.HostReportedFormattedFile.ShouldBeTrue();
        _fixture.ChronicleCodeFixResult.SourceWasRewritten.ShouldBeTrue();
    }
}
