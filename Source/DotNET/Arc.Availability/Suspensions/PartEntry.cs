// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability.Suspensions;

/// <summary>
/// A module, feature, slice or part an operator can suspend.
/// </summary>
/// <param name="Kind">One of <see cref="ModuleKind"/>, <see cref="FeatureKind"/>, <see cref="SliceKind"/> or <see cref="PartKind"/>.</param>
/// <param name="Path">The path to suspend, see <see cref="PartPaths"/>.</param>
/// <param name="Name">The name to show.</param>
public record PartEntry(string Kind, string Path, string Name)
{
    /// <summary>
    /// The kind of a module.
    /// </summary>
    public const string ModuleKind = "Module";

    /// <summary>
    /// The kind of a feature.
    /// </summary>
    public const string FeatureKind = "Feature";

    /// <summary>
    /// The kind of a slice.
    /// </summary>
    public const string SliceKind = "Slice";

    /// <summary>
    /// The kind of a part: a command or a query.
    /// </summary>
    public const string PartKind = "Part";
}
