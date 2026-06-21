// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Core.Generators.Integration.Specs.Testing;

/// <summary>
/// Represents the result of building a consumer that references both the Cratis meta package and Cratis.Arc.MongoDB
/// from locally packed nupkgs.
/// </summary>
/// <param name="buildSucceeded">Whether the consumer build completed successfully.</param>
/// <param name="buildOutput">The combined standard output and error of the consumer build.</param>
/// <param name="workingDirectory">The temporary working directory used by the integration scenario.</param>
public sealed class CratisMetaConsumerBuildResult(
    bool buildSucceeded,
    string buildOutput,
    string workingDirectory)
{
    /// <summary>
    /// Gets a value indicating whether the consumer build completed successfully.
    /// </summary>
    public bool BuildSucceeded { get; } = buildSucceeded;

    /// <summary>
    /// Gets the combined standard output and error of the consumer build.
    /// </summary>
    public string BuildOutput { get; } = buildOutput;

    /// <summary>
    /// Gets the temporary working directory used by the integration scenario.
    /// </summary>
    public string WorkingDirectory { get; } = workingDirectory;
}
