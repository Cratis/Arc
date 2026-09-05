// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Core.Generators.Integration.Specs.Testing;

/// <summary>
/// Represents one dependency declared in a packed nuspec dependency group.
/// </summary>
/// <param name="TargetFramework">The dependency group's target framework.</param>
/// <param name="Id">The dependency package identifier.</param>
/// <param name="Version">The declared dependency version.</param>
/// <param name="Include">The declared included asset classes.</param>
/// <param name="Exclude">The declared excluded asset classes.</param>
public sealed record PackageDependency(
    string TargetFramework,
    string Id,
    string Version,
    string Include,
    string Exclude)
{
    /// <summary>
    /// Gets whether the dependency includes the specified asset class.
    /// </summary>
    /// <param name="asset">The asset class.</param>
    /// <returns><see langword="true"/> when the asset class is included; otherwise, <see langword="false"/>.</returns>
    public bool Includes(string asset) => ContainsAsset(Include, asset);

    /// <summary>
    /// Gets whether the dependency excludes the specified asset class.
    /// </summary>
    /// <param name="asset">The asset class.</param>
    /// <returns><see langword="true"/> when the asset class is excluded; otherwise, <see langword="false"/>.</returns>
    public bool Excludes(string asset) => ContainsAsset(Exclude, asset);

    static bool ContainsAsset(string assets, string asset) =>
        assets.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Contains(asset, StringComparer.OrdinalIgnoreCase);
}
