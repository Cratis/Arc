// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.IO.Compression;

namespace Cratis.Arc.Core.Generators.Integration.Specs.for_ArcPackage;

/// <summary>
/// Verifies that the Cratis.Arc aggregate package bundles the ARC analyzers and proxy generator, so consumers
/// of the aggregate package receive the full analyzer/generator set without needing to reference Cratis.Arc.Core
/// directly. NuGet's analyzers assets do not flow transitively through package dependencies.
/// </summary>
public class when_packing_arc
{
    /// <summary>
    /// Verifies that packing Cratis.Arc bundles the Arc analyzer, code fixes, and proxy generator assemblies
    /// under analyzers/dotnet/cs.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when packing does not produce a Cratis.Arc package.</exception>
    [Fact]
    public void should_bundle_the_arc_analyzers_and_generator()
    {
        var repositoryRoot = GetRepositoryRoot();
        var workingDirectory = Path.Combine(Path.GetTempPath(), "Cratis.Arc.Package.Integration", Guid.NewGuid().ToString("N"));
        var packageDirectory = Path.Combine(workingDirectory, "packages");

        Directory.CreateDirectory(packageDirectory);
        RunDotNet(
            repositoryRoot,
            $"pack \"{Path.Combine(repositoryRoot, "Source", "DotNET", "Arc", "Arc.csproj")}\" -c Release --output \"{packageDirectory}\" -p:IncludeSymbols=false -p:IncludeSource=false");

        var packagePath = Directory.GetFiles(packageDirectory, "Cratis.Arc.*.nupkg", SearchOption.TopDirectoryOnly)
            .Where(_ => !_.Contains("Arc.Core", StringComparison.Ordinal) && !_.Contains("Arc.Chronicle", StringComparison.Ordinal))
            .OrderDescending()
            .FirstOrDefault()
            ?? throw new InvalidOperationException("Expected a packed Cratis.Arc nupkg to be created.");

        using var package = ZipFile.OpenRead(packagePath);
        var analyzerEntries = package.Entries
            .Select(_ => _.FullName)
            .Where(_ => _.StartsWith("analyzers/dotnet/cs/", StringComparison.Ordinal))
            .ToArray();

        analyzerEntries.ShouldContain("analyzers/dotnet/cs/Cratis.Arc.Core.CodeAnalysis.dll");
        analyzerEntries.ShouldContain("analyzers/dotnet/cs/Cratis.Arc.Core.CodeAnalysis.CodeFixes.dll");
        analyzerEntries.ShouldContain("analyzers/dotnet/cs/Cratis.Arc.Core.Generators.dll");
    }

    /// <summary>
    /// Verifies that no Microsoft.CodeAnalysis.Workspaces assembly leaks into the Cratis.Arc package's analyzer
    /// folder. Workspaces references in a compiler-loaded analyzer assembly cause RS1038 and may crash the
    /// compiler host.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when packing does not produce a Cratis.Arc package.</exception>
    [Fact]
    public void should_not_leak_workspaces_binaries()
    {
        var repositoryRoot = GetRepositoryRoot();
        var workingDirectory = Path.Combine(Path.GetTempPath(), "Cratis.Arc.Package.NoWorkspaces.Integration", Guid.NewGuid().ToString("N"));
        var packageDirectory = Path.Combine(workingDirectory, "packages");

        Directory.CreateDirectory(packageDirectory);
        RunDotNet(
            repositoryRoot,
            $"pack \"{Path.Combine(repositoryRoot, "Source", "DotNET", "Arc", "Arc.csproj")}\" -c Release --output \"{packageDirectory}\" -p:IncludeSymbols=false -p:IncludeSource=false");

        var packagePath = Directory.GetFiles(packageDirectory, "Cratis.Arc.*.nupkg", SearchOption.TopDirectoryOnly)
            .Where(_ => !_.Contains("Arc.Core", StringComparison.Ordinal) && !_.Contains("Arc.Chronicle", StringComparison.Ordinal))
            .OrderDescending()
            .FirstOrDefault()
            ?? throw new InvalidOperationException("Expected a packed Cratis.Arc nupkg to be created.");

        using var package = ZipFile.OpenRead(packagePath);
        var workspacesEntries = package.Entries
            .Select(_ => _.FullName)
            .Where(_ => _.Contains("Microsoft.CodeAnalysis.Workspaces", StringComparison.OrdinalIgnoreCase)
                     || _.Contains("System.Composition", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        workspacesEntries.ShouldBeEmpty();
    }

    static string GetRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "Arc.slnx")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root from integration spec output directory.");
    }

    static void RunDotNet(string workingDirectory, string arguments)
    {
        using var process = new Process
        {
            StartInfo = new()
            {
                FileName = "dotnet",
                Arguments = arguments,
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            }
        };

        process.Start();
        var standardOutputTask = process.StandardOutput.ReadToEndAsync();
        var standardErrorTask = process.StandardError.ReadToEndAsync();
        process.WaitForExit();
        var standardOutput = standardOutputTask.GetAwaiter().GetResult();
        var standardError = standardErrorTask.GetAwaiter().GetResult();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"dotnet {arguments} failed with exit code {process.ExitCode}.{Environment.NewLine}{standardOutput}{Environment.NewLine}{standardError}");
        }
    }
}
