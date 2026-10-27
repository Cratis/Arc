// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Slices;

/// <summary>
/// Declares how many leading namespace segments to skip for an assembly when no namespace root matches a type.
/// </summary>
/// <remarks>
/// The Arc build emits it from <c>CratisProxiesSegmentsToSkip</c>, the same property the proxy generator reads.
/// </remarks>
/// <param name="count">The number of leading namespace segments to skip.</param>
[AttributeUsage(AttributeTargets.Assembly)]
public sealed class SliceNamespaceSegmentsToSkipAttribute(int count) : Attribute
{
    /// <summary>
    /// Gets the number of leading namespace segments to skip.
    /// </summary>
    public int Count { get; } = count;
}
