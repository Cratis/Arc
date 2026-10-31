// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Runtime.Loader;

namespace Cratis.Arc.ProxyGenerator.Specs.RuntimeDependencyApplication;

/// <summary>
/// Reaches an output-local dependency by name, the way serializers and type registries do.
/// </summary>
/// <remarks>
/// A load through a load context carries no requesting assembly, so nothing but the application's own dependency
/// manifest can place this library: it is not referenced by the generator and is never loaded before this call.
/// </remarks>
public static class ApplicationNameRules
{
    public static int MaximumLength()
    {
        var library = AssemblyLoadContext.Default.LoadFromAssemblyName(new AssemblyName("Cratis.Arc.ProxyGenerator.Specs.RuntimeDependencyLibrary"));
        var marker = library.GetType("Cratis.Arc.ProxyGenerator.Specs.RuntimeDependencyLibrary.RuntimeDependencyMarker", throwOnError: true)!;
        return (int)marker.GetProperty("MaximumNameLength")!.GetValue(null)!;
    }
}
