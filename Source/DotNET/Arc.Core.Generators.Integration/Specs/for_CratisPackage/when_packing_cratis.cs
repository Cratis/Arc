// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.IO.Compression;
using System.Xml.Linq;

namespace Cratis.Arc.Core.Generators.Integration.Specs.for_CratisPackage;

/// <summary>
/// Verifies that the Cratis meta-package keeps the proxy generator build package as a dependency.
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
