// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.TypeDiscovery.Plugin;

namespace Cratis.Arc;

/// <summary>
/// Uses a plugin type so the compiler emits an assembly reference to it, and does so from a method nothing calls
/// so nothing loads the plugin before the specification does.
/// </summary>
/// <remarks>
/// The C# compiler omits a reference to an assembly whose types the compilation never uses, and the assembly
/// closure walk this project is about finds candidates through <c>Assembly.GetReferencedAssemblies</c> - so
/// without a use somewhere, the plugin would be sitting in the output folder unreferenced and unreachable, and
/// the specification would pass by discovering nothing.
/// </remarks>
static class KeepsThePluginInTheReferenceClosure
{
    /// <summary>
    /// Never called.
    /// </summary>
    /// <returns>The plugin's marker type.</returns>
    public static Type TheMarkerTypeNothingAsksFor() => typeof(PluginMarker);
}
