// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Core.Generators.Integration.Specs.Testing;

/// <summary>
/// Represents one clean consumer restore and build result.
/// </summary>
/// <param name="Definition">The consumer definition.</param>
/// <param name="BuildWasClean">Whether the build completed with zero warnings and zero errors.</param>
/// <param name="PackagesHaveLocalProvenance">Whether every restored integration package matches its local-feed nupkg hash.</param>
/// <param name="ArcAnalyzerCount">The resolved ARC analyzer count reported by MSBuild.</param>
/// <param name="ArcCodeFixCount">The resolved ARC code-fix count reported by MSBuild.</param>
/// <param name="ArcGeneratorCount">The resolved Arc generator count reported by MSBuild.</param>
/// <param name="ChronicleAnalyzerCount">The resolved ARCCHR analyzer count reported by MSBuild.</param>
/// <param name="ChronicleCodeFixCount">The resolved ARCCHR code-fix count reported by MSBuild.</param>
/// <param name="GeneratedMarkerCount">The generated marker source count.</param>
/// <param name="GeneratedQueryMetadataCount">The generated query metadata source count.</param>
/// <param name="GeneratedMetadataContainsReadModel">Whether generated metadata contains <c>WeatherReadModel</c>.</param>
/// <param name="GeneratedMetadataContainsQuery">Whether generated metadata contains <c>GetByName</c>.</param>
/// <param name="ForbiddenOutputFiles">Workspaces or Composition binaries found in consumer output.</param>
public sealed record ConsumerBuildResult(
    ConsumerDefinition Definition,
    bool BuildWasClean,
    bool PackagesHaveLocalProvenance,
    int ArcAnalyzerCount,
    int ArcCodeFixCount,
    int ArcGeneratorCount,
    int ChronicleAnalyzerCount,
    int ChronicleCodeFixCount,
    int GeneratedMarkerCount,
    int GeneratedQueryMetadataCount,
    bool GeneratedMetadataContainsReadModel,
    bool GeneratedMetadataContainsQuery,
    IReadOnlyCollection<string> ForbiddenOutputFiles);
