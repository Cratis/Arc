// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Slices;

/// <summary>
/// Declares a namespace root for an assembly: the slice location of a type below it is resolved from the
/// namespace segments that follow the root.
/// </summary>
/// <remarks>
/// The Arc build emits one for every <c>NamespaceRoot</c> item, the same items the proxy generator reads.
/// </remarks>
/// <param name="namespace">The namespace root, for example <c>MyApp.Features</c>.</param>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class SliceNamespaceRootAttribute(string @namespace) : Attribute
{
    /// <summary>
    /// Gets the namespace root.
    /// </summary>
    public string Namespace { get; } = @namespace;
}
