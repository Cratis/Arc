// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Templates;
using Cratis.Arc.Slices;

namespace Cratis.Arc.ProxyGenerator;

public static partial class TypeExtensions
{
    /// <summary>
    /// Resolve the slice location of a command or query type from the same namespace mapping as its output folder.
    /// </summary>
    /// <param name="type">The type that owns the command or query.</param>
    /// <param name="segmentsToSkip">Number of segments to skip from the namespace when no namespace root matches.</param>
    /// <returns>The resolved <see cref="SliceLocationDescriptor"/>.</returns>
    public static SliceLocationDescriptor ResolveSliceLocation(this Type type, int segmentsToSkip)
    {
        var segments = SliceNamespace.Resolve(type.Namespace ?? string.Empty, _namespaceRoots.Select(root => root.Namespace), segmentsToSkip);
        var (module, feature, slice) = SliceNamespace.ToParts(segments);
        return new(module, feature, slice);
    }
}
