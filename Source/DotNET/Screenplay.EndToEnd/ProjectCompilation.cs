// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Cratis.Arc.Screenplay.Analysis;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

namespace Cratis.Arc.Screenplay.EndToEnd;

/// <summary>
/// Reads a real project, or a real solution, into the compilations the generator is handed.
/// </summary>
/// <remarks>
/// The generator itself never loads a workspace - it takes compilations and nothing else - which is what lets every
/// specification build them from source strings. That seam is exactly what this check exists to get behind: a
/// compilation built from strings has no intermediate folder, no source generator output on disk and no referenced
/// package that declares an event, so a defect that only shows in one of those is invisible to a specification by
/// construction. Two shipped that way.
/// <para>
/// A solution is read as every project it holds that could hold part of the application, which is what an application
/// written as several projects really is. Whether a project could is asked of what it can see rather than of what it
/// is called: an artifact is declared with an attribute the framework ships, so a project resolving neither the Arc
/// nor the Chronicle one cannot declare a single thing the document is made of. A Roslyn analyzer beside the
/// application is exactly that project, and reading its <c>netstandard2.0</c> compilation in as though it were part
/// of the application says something about the solution that is not true.
/// </para>
/// <para>
/// The specifications of an application are turned away separately and by name, because nothing about what a
/// specification project can see tells them apart - it references the same framework the application does, which is
/// the whole point of it. That leaves the name the solution lays them out by, which is a weaker signal and is asked
/// second, of the projects that got past the first.
/// </para>
/// </remarks>
public static class ProjectCompilation
{
    static readonly string[] _artifacts = [WellKnownTypeNames.CommandAttribute, WellKnownTypeNames.EventTypeAttribute];
    static readonly string[] _specifications = ["Specs", "Tests", "Specs.AppHost"];
    static readonly string[] _solutions = [".sln", ".slnx", ".slnf"];

    /// <summary>
    /// Loads a project or a solution and everything it references.
    /// </summary>
    /// <param name="path">The full path of the project or solution file.</param>
    /// <param name="failures">Everything the workspace reported while loading, in the order it reported them.</param>
    /// <returns>The compilations, empty when nothing yielded one.</returns>
    /// <remarks>
    /// Registering the SDK has to happen before any MSBuild type is touched, which is why the members touching one
    /// are marked as not inlinable - the JIT would otherwise resolve those types while registration is still running.
    /// </remarks>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async Task<IReadOnlyList<Compilation>> Of(string path, ICollection<string> failures)
    {
        if (!MSBuildLocator.IsRegistered)
        {
            MSBuildLocator.RegisterDefaults();
        }

        return IsSolution(path) ? await LoadSolution(path, failures) : await LoadProject(path, failures);
    }

    /// <summary>
    /// Determines whether a path names a solution rather than a project.
    /// </summary>
    /// <param name="path">The path to check.</param>
    /// <returns>True when it names a solution.</returns>
    static bool IsSolution(string path) =>
        Array.Exists(_solutions, extension => string.Equals(Path.GetExtension(path), extension, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Loads a single project.
    /// </summary>
    /// <param name="path">The full path of the project file.</param>
    /// <param name="failures">Everything the workspace reported while loading.</param>
    /// <returns>The compilation, empty when the project yielded none.</returns>
    [MethodImpl(MethodImplOptions.NoInlining)]
    static async Task<IReadOnlyList<Compilation>> LoadProject(string path, ICollection<string> failures)
    {
        var reported = new Lock();
        using var workspace = MSBuildWorkspace.Create();
        using var subscription = workspace.RegisterWorkspaceFailedHandler(args => Report(args, reported, failures));

        var project = await workspace.OpenProjectAsync(path);

        return await project.GetCompilationAsync() is { } compilation ? [compilation] : [];
    }

    /// <summary>
    /// Loads every project of a solution that holds part of the application.
    /// </summary>
    /// <param name="path">The full path of the solution file.</param>
    /// <param name="failures">Everything the workspace reported while loading.</param>
    /// <returns>The compilations, ordered by the name of the project each one came from.</returns>
    [MethodImpl(MethodImplOptions.NoInlining)]
    static async Task<IReadOnlyList<Compilation>> LoadSolution(string path, ICollection<string> failures)
    {
        var reported = new Lock();
        using var workspace = MSBuildWorkspace.Create();
        using var subscription = workspace.RegisterWorkspaceFailedHandler(args => Report(args, reported, failures));

        var solution = await workspace.OpenSolutionAsync(path);
        var compilations = new List<Compilation>();

        foreach (var project in solution.Projects
            .Where(_ => !IsSpecifications(_.Name))
            .OrderBy(_ => _.Name, StringComparer.Ordinal))
        {
            if (await project.GetCompilationAsync() is { } compilation && CanDeclareAnArtifact(compilation))
            {
                compilations.Add(compilation);
            }
        }

        return compilations;
    }

    /// <summary>
    /// Determines whether a compilation could declare any of what the document is made of.
    /// </summary>
    /// <param name="compilation">The compilation to check.</param>
    /// <returns>True when it could.</returns>
    /// <remarks>
    /// Every artifact is declared with an attribute the framework ships, so resolving one of those attributes is the
    /// least a project has to be able to do to hold part of the application. What this misses is a project that can
    /// see the framework and declares nothing - a host wiring the application up - which is read and contributes an
    /// empty model, and a project holding types the application carries without referencing either framework, whose
    /// types are reached through the artifacts referring to them rather than through the project declaring them.
    /// </remarks>
    static bool CanDeclareAnArtifact(Compilation compilation) =>
        Array.Exists(_artifacts, _ => compilation.GetTypeByMetadataName(_) is not null);

    /// <summary>
    /// Determines whether a project holds specifications rather than part of the application.
    /// </summary>
    /// <param name="name">The name of the project.</param>
    /// <returns>True when it holds specifications.</returns>
    static bool IsSpecifications(string name) =>
        Array.Exists(_specifications, suffix =>
            string.Equals(name, suffix, StringComparison.Ordinal) ||
            name.EndsWith($".{suffix}", StringComparison.Ordinal));

    /// <summary>
    /// Records what the workspace reported while loading.
    /// </summary>
    /// <param name="args">What was reported.</param>
    /// <param name="reported">The lock guarding the collection.</param>
    /// <param name="failures">The failures collected so far.</param>
    static void Report(WorkspaceDiagnosticEventArgs args, Lock reported, ICollection<string> failures)
    {
        lock (reported)
        {
            failures.Add(args.Diagnostic.Message);
        }
    }
}
