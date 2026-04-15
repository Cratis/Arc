// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Core.Generators.Integration.Specs.Testing;

namespace Cratis.Arc.Core.Generators.Integration.Specs.for_PackagedQueryMetadataGenerator;

/// <summary>
/// Verifies that Arc.Core packages the query metadata generator and that a consumer app receives generated query metadata from the packed nupkg.
/// </summary>
/// <param name="fixture">The fixture that executes the packaged Arc.Core integration scenario once for the test class.</param>
public class when_restoring_arc_core_from_a_packed_nupkg(PackagedArcCoreBuildFixture fixture) : IClassFixture<PackagedArcCoreBuildFixture>
{
    readonly PackagedArcCoreBuildResult _buildResult = fixture.Result;

    /// <summary>
    /// Verifies that the packed Arc.Core nupkg contains the query metadata generator as an analyzer asset.
    /// </summary>
    [Fact]
    public void should_pack_the_generator_dll_as_an_analyzer_asset() =>
        _buildResult.PackageEntries.ShouldContain("analyzers/dotnet/cs/Cratis.Arc.Core.Generators.dll");

    /// <summary>
    /// Verifies that building the sample app from the packed Arc.Core nupkg emits the generated query metadata source file.
    /// </summary>
    [Fact]
    public void should_emit_the_generated_query_metadata_file() =>
        File.Exists(_buildResult.GeneratedFilePath).ShouldBeTrue();

    /// <summary>
    /// Verifies that the generated query metadata registers the sample app query.
    /// </summary>
    [Fact]
    public void should_register_the_sample_query_in_generated_metadata() =>
        _buildResult.GeneratedFileContent.ShouldContain("SampleApp.WeatherReadModel.GetByName");

    /// <summary>
    /// Verifies that the generated query metadata registers the sample app read model type.
    /// </summary>
    [Fact]
    public void should_register_the_sample_read_model_type_in_generated_metadata() =>
        _buildResult.GeneratedFileContent.ShouldContain("typeof(global::SampleApp.WeatherReadModel)");
}