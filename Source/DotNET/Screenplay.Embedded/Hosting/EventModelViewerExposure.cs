// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Reflection;

namespace Cratis.Arc.Screenplay.Embedded.Hosting;

/// <summary>
/// Decides whether the automatic mapping exposes the explorer.
/// </summary>
/// <remarks>
/// The decision is deliberately about the build rather than the environment: an environment name is
/// configuration a deployment can get wrong, while a Release-built assembly is what is published. An
/// application that wants the explorer in a Release build says so, and is then the one that said it.
/// </remarks>
static class EventModelViewerExposure
{
    /// <summary>
    /// Gets whether the explorer is exposed for the given options and entry assembly.
    /// </summary>
    /// <param name="options">The options the host configured.</param>
    /// <param name="entryAssembly">The entry assembly of the process, or null when the process has none.</param>
    /// <returns>True when the explorer is to be mapped, false otherwise.</returns>
    internal static bool ShouldExpose(EventModelViewerOptions options, Assembly? entryAssembly) =>
        options.Enabled ?? (entryAssembly is not null && IsDebugBuild(entryAssembly));

    /// <summary>
    /// Gets whether an assembly was built with optimizations disabled, as a Debug build is.
    /// </summary>
    /// <param name="assembly">The assembly to read.</param>
    /// <returns>True when the assembly says its optimizations are disabled, false otherwise.</returns>
    /// <remarks>
    /// An assembly without the attribute at all - which a Release build emitted by some toolchains has - counts
    /// as optimized, so the explorer stays closed rather than opening on a missing declaration.
    /// </remarks>
    internal static bool IsDebugBuild(Assembly assembly) =>
        assembly.GetCustomAttribute<DebuggableAttribute>() is { IsJITOptimizerDisabled: true };
}
