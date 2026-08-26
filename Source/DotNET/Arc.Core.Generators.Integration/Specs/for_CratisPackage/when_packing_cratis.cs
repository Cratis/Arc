// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.IO.Compression;
using System.Xml.Linq;

namespace Cratis.Arc.Core.Generators.Integration.Specs.for_CratisPackage;

/// <summary>
/// Verifies that the Cratis meta-package bundles all ARC and ARCCHR analyzers plus the proxy generator, and
/// keeps the proxy generator build package as a dependency. NuGet's analyzers assets do not flow transitively
/// through package dependencies, so this aggregate package must explicitly bundle them.
/// </summary>
public class when_packing_cratis
{
    /// <summary>
    /// Verifies that packing Cratis includes the proxy generator build package as a dependency.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when packing does not produce a Cratis package or nuspec entry.</exception>
    [Fact]
    public void should_include_proxy_generator_build_as_a_package_dependency()
    {
        var repositoryRoot = GetRepositoryRoot();
        var workingDirectory = Path.Combine(Path.GetTempPath(), "Cratis.Package.Integration", Guid.NewGuid().ToString("N"));
        var packageDirectory = Path.Combine(workingDirectory, "packages");

        Directory.CreateDirectory(packageDirectory);
        RunDotNet(
            repositoryRoot,
            $"pack \"{Path.Combine(repositoryRoot, "Source", "DotNET", "Cratis", "Cratis.csproj")}\" -c Release --output \"{packageDirectory}\" -p:IncludeSymbols=false -p:IncludeSource=false");

        var packagePath = Directory.GetFiles(packageDirectory, "Cratis.*.nupkg", SearchOption.TopDirectoryOnly)
            .OrderDescending()
            .FirstOrDefault()
            ?? throw new InvalidOperationException("Expected a packed Cratis nupkg to be created.");

        using var package = ZipFile.OpenRead(packagePath);
        var nuspecEntry = package.Entries.Single(_ => _.FullName.EndsWith(".nuspec", StringComparison.Ordinal));
        using var nuspecStream = nuspecEntry.Open();
        var nuspec = XDocument.Load(nuspecStream);
        var dependencyIds = nuspec
            .Descendants()
            .Where(_ => _.Name.LocalName == "dependency")
            .Select(_ => _.Attribute("id")?.Value)
            .Where(_ => _ is not null)
            .ToArray();

        dependencyIds.ShouldContain("Cratis.Arc.ProxyGenerator.Build");
    }

    /// <summary>
    /// Verifies that packing Cratis bundles all Arc and Chronicle analyzer and code fix assemblies plus the
    /// proxy generator under analyzers/dotnet/cs. This ensures consumers of the Cratis aggregate package
    /// receive the full analyzer/generator set without needing to reference Cratis.CodeAnalysis.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when packing does not produce a Cratis package.</exception>
    [Fact]
    public void should_bundle_all_analyzers_and_generator()
    {
        var repositoryRoot = GetRepositoryRoot();
        var workingDirectory = Path.Combine(Path.GetTempPath(), "Cratis.Package.Analyzers.Integration", Guid.NewGuid().ToString("N"));
        var packageDirectory = Path.Combine(workingDirectory, "packages");

        Directory.CreateDirectory(packageDirectory);
        RunDotNet(
            repositoryRoot,
            $"pack \"{Path.Combine(repositoryRoot, "Source", "DotNET", "Cratis", "Cratis.csproj")}\" -c Release --output \"{packageDirectory}\" -p:IncludeSymbols=false -p:IncludeSource=false");

        var packagePath = Directory.GetFiles(packageDirectory, "Cratis.*.nupkg", SearchOption.TopDirectoryOnly)
            .Where(_ => Path.GetFileName(_).StartsWith("Cratis.", StringComparison.Ordinal)
                     && !Path.GetFileName(_).StartsWith("Cratis.Arc", StringComparison.Ordinal)
                     && !Path.GetFileName(_).StartsWith("Cratis.CodeAnalysis", StringComparison.Ordinal))
            .OrderDescending()
            .FirstOrDefault()
            ?? throw new InvalidOperationException("Expected a packed Cratis nupkg to be created.");

        using var package = ZipFile.OpenRead(packagePath);
        var analyzerEntries = package.Entries
            .Select(_ => _.FullName)
            .Where(_ => _.StartsWith("analyzers/dotnet/cs/", StringComparison.Ordinal))
            .ToArray();

        // Arc analyzers and generator
        analyzerEntries.ShouldContain("analyzers/dotnet/cs/Cratis.Arc.Core.CodeAnalysis.dll");
        analyzerEntries.ShouldContain("analyzers/dotnet/cs/Cratis.Arc.Core.CodeAnalysis.CodeFixes.dll");
        analyzerEntries.ShouldContain("analyzers/dotnet/cs/Cratis.Arc.Core.Generators.dll");

        // Chronicle analyzers
        analyzerEntries.ShouldContain("analyzers/dotnet/cs/Cratis.Arc.Chronicle.CodeAnalysis.dll");
        analyzerEntries.ShouldContain("analyzers/dotnet/cs/Cratis.Arc.Chronicle.CodeAnalysis.CodeFixes.dll");
    }

    /// <summary>
    /// Verifies that no Microsoft.CodeAnalysis.Workspaces or System.Composition assembly leaks into the Cratis
    /// package's analyzer folder. Workspaces references in a compiler-loaded analyzer assembly cause RS1038 and
    /// may crash the compiler host.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when packing does not produce a Cratis package.</exception>
    [Fact]
    public void should_not_leak_workspaces_binaries()
    {
        var repositoryRoot = GetRepositoryRoot();
        var workingDirectory = Path.Combine(Path.GetTempPath(), "Cratis.Package.NoWorkspaces.Integration", Guid.NewGuid().ToString("N"));
        var packageDirectory = Path.Combine(workingDirectory, "packages");

        Directory.CreateDirectory(packageDirectory);
        RunDotNet(
            repositoryRoot,
            $"pack \"{Path.Combine(repositoryRoot, "Source", "DotNET", "Cratis", "Cratis.csproj")}\" -c Release --output \"{packageDirectory}\" -p:IncludeSymbols=false -p:IncludeSource=false");

        var packagePath = Directory.GetFiles(packageDirectory, "Cratis.*.nupkg", SearchOption.TopDirectoryOnly)
            .Where(_ => Path.GetFileName(_).StartsWith("Cratis.", StringComparison.Ordinal)
                     && !Path.GetFileName(_).StartsWith("Cratis.Arc", StringComparison.Ordinal)
                     && !Path.GetFileName(_).StartsWith("Cratis.CodeAnalysis", StringComparison.Ordinal))
            .OrderDescending()
            .FirstOrDefault()
            ?? throw new InvalidOperationException("Expected a packed Cratis nupkg to be created.");

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
