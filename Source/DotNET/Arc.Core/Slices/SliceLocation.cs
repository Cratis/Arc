// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Slices;

/// <summary>
/// Represents where a command or query lives: the module, feature and slice resolved from its namespace.
/// </summary>
/// <remarks>
/// The location is resolved from the same namespace mapping the proxy generator uses for output folders: below the
/// longest matching namespace root (<c>NamespaceRoot</c>), otherwise after skipping the configured number of leading
/// segments (<c>CratisProxiesSegmentsToSkip</c>). The first remaining segment is the module, the last is the slice
/// and those in between form the feature. When there are fewer than three segments, the deepest parts are filled
/// and the rest are empty, never null.
/// </remarks>
/// <param name="Module">The <see cref="ModuleName"/> the slice belongs to.</param>
/// <param name="Feature">The <see cref="FeatureName"/> the slice belongs to.</param>
/// <param name="Slice">The <see cref="SliceName"/> of the slice.</param>
public record SliceLocation(ModuleName Module, FeatureName Feature, SliceName Slice)
{
    /// <summary>
    /// Gets a slice location where every part is empty.
    /// </summary>
    public static readonly SliceLocation NotSet = new(ModuleName.NotSet, FeatureName.NotSet, SliceName.NotSet);

    /// <summary>
    /// Resolves the slice location of a type, such as a command or a read model, from its namespace.
    /// </summary>
    /// <param name="type">The type to resolve for.</param>
    /// <returns>The resolved <see cref="SliceLocation"/>.</returns>
    /// <remarks>
    /// The namespace roots and segments to skip are read from <see cref="SliceNamespaceRootAttribute"/> and
    /// <see cref="SliceNamespaceSegmentsToSkipAttribute"/> on the type's assembly, falling back to the entry
    /// assembly when the type's assembly declares neither. The Arc build emits them from the same
    /// MSBuild settings the proxy generator reads.
    /// </remarks>
    public static SliceLocation For(Type type) => SliceLocationResolver.For(type);

    /// <inheritdoc/>
    public override string ToString() => string.Join('/', new[] { Module.Value, Feature.Value, Slice.Value }.Where(part => part.Length > 0));
}
