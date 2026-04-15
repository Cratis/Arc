// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Core.Generators.Integration.Specs.Testing;

/// <summary>
/// Represents the result of packing Arc.Core and building the sample consumer app from the packed nupkg.
/// </summary>
/// <param name="packagePath">The path to the packed Arc.Core nupkg.</param>
/// <param name="packageEntries">The entries inside the packed Arc.Core nupkg.</param>
/// <param name="generatedFilePath">The path to the generated query metadata file emitted by the sample app build.</param>
/// <param name="generatedFileContent">The content of the generated query metadata file.</param>
/// <param name="workingDirectory">The temporary working directory used by the integration scenario.</param>
public sealed class PackagedArcCoreBuildResult(
    string packagePath,
    IReadOnlyCollection<string> packageEntries,
    string generatedFilePath,
    string generatedFileContent,
    string workingDirectory)
{
    /// <summary>
    /// Gets the path to the packed Arc.Core nupkg.
    /// </summary>
    public string PackagePath { get; } = packagePath;

    /// <summary>
    /// Gets the entries inside the packed Arc.Core nupkg.
    /// </summary>
    public IReadOnlyCollection<string> PackageEntries { get; } = packageEntries;

    /// <summary>
    /// Gets the path to the generated query metadata file emitted by the sample app build.
    /// </summary>
    public string GeneratedFilePath { get; } = generatedFilePath;

    /// <summary>
    /// Gets the content of the generated query metadata file.
    /// </summary>
    public string GeneratedFileContent { get; } = generatedFileContent;

    /// <summary>
    /// Gets the temporary working directory used by the integration scenario.
    /// </summary>
    public string WorkingDirectory { get; } = workingDirectory;
}