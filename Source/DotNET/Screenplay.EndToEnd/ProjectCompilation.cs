// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

namespace Cratis.Arc.Screenplay.EndToEnd;

/// <summary>
/// Reads a real project file into the compilation the generator is handed.
/// </summary>
/// <remarks>
/// The generator itself never loads a workspace - it takes a <see cref="Compilation"/> and nothing else - which is
/// what lets every specification build one from source strings. That seam is exactly what this check exists to get
/// behind: a compilation built from strings has no intermediate folder, no source generator output on disk and no
/// referenced package that declares an event, so a defect that only shows in one of those is invisible to a
/// specification by construction. Two shipped that way.
/// </remarks>
public static class ProjectCompilation
{
    /// <summary>
    /// Loads a project and everything it references.
    /// </summary>
    /// <param name="path">The full path of the project file.</param>
    /// <param name="failures">Everything the workspace reported while loading, in the order it reported them.</param>
    /// <returns>The <see cref="Compilation"/>, or <see langword="null"/> when the project yielded none.</returns>
    /// <remarks>
    /// Registering the SDK has to happen before any MSBuild type is touched, which is why the member touching one is
    /// marked as not inlinable - the JIT would otherwise resolve those types while registration is still running.
    /// </remarks>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async Task<Compilation?> Of(string path, ICollection<string> failures)
    {
        if (!MSBuildLocator.IsRegistered)
        {
            MSBuildLocator.RegisterDefaults();
        }

        return await Load(path, failures);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static async Task<Compilation?> Load(string path, ICollection<string> failures)
    {
        var reported = new Lock();
        using var workspace = MSBuildWorkspace.Create();
        using var subscription = workspace.RegisterWorkspaceFailedHandler(args =>
        {
            lock (reported)
            {
                failures.Add(args.Diagnostic.Message);
            }
        });

        var project = await workspace.OpenProjectAsync(path);

        return await project.GetCompilationAsync();
    }
}
