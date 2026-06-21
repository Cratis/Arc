// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.IO.Compression;

namespace Cratis.Arc.Core.Generators.Integration.Specs.for_CratisCodeAnalysisPackage;

/// <summary>
/// Verifies that the Cratis.CodeAnalysis umbrella package bundles every Cratis Arc lint analyzer, so a single
/// reference brings the full ARC*/ARCCHR* set.
/// </summary>
public class when_packing_cratis_codeanalysis
{
    /// <summary>
    /// Verifies that packing Cratis.CodeAnalysis bundles the Arc and Arc.Chronicle analyzer assemblies under
    /// analyzers/dotnet/cs.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when packing does not produce a Cratis.CodeAnalysis package.</exception>
    [Fact]
    public void should_bundle_the_arc_and_chronicle_lint_analyzers()
    {
        var repositoryRoot = GetRepositoryRoot();
        var workingDirectory = Path.Combine(Path.GetTempPath(), "Cratis.CodeAnalysis.Package.Integration", Guid.NewGuid().ToString("N"));
        var packageDirectory = Path.Combine(workingDirectory, "packages");

        Directory.CreateDirectory(packageDirectory);
        RunDotNet(
            repositoryRoot,
            $"pack \"{Path.Combine(repositoryRoot, "Source", "DotNET", "Cratis.CodeAnalysis", "Cratis.CodeAnalysis.csproj")}\" -c Release --output \"{packageDirectory}\" -p:IncludeSymbols=false -p:IncludeSource=false");

        var packagePath = Directory.GetFiles(packageDirectory, "Cratis.CodeAnalysis.*.nupkg", SearchOption.TopDirectoryOnly)
            .OrderDescending()
            .FirstOrDefault()
            ?? throw new InvalidOperationException("Expected a packed Cratis.CodeAnalysis nupkg to be created.");

        using var package = ZipFile.OpenRead(packagePath);
        var analyzerEntries = package.Entries
            .Select(_ => _.FullName)
            .Where(_ => _.StartsWith("analyzers/dotnet/cs/", StringComparison.Ordinal))
            .ToArray();

        analyzerEntries.ShouldContain("analyzers/dotnet/cs/Cratis.Arc.Core.CodeAnalysis.dll");
        analyzerEntries.ShouldContain("analyzers/dotnet/cs/Cratis.Arc.Chronicle.CodeAnalysis.dll");
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
