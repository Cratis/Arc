// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.IO.Compression;

namespace Cratis.Arc.Core.Generators.Integration.Specs.Testing;

/// <summary>
/// Executes the packaged Arc.Core integration scenario against the sample consumer app.
/// </summary>
internal static class PackagedArcCoreBuildScenario
{
    /// <summary>
    /// Packs Arc.Core into a local feed, restores the sample app from that packed nupkg, and builds the sample app.
    /// </summary>
    /// <returns>A <see cref="PackagedArcCoreBuildResult"/> describing the packed nupkg and generated source output.</returns>
    /// <exception cref="PackageIntegrationFailure">Thrown when packing Arc.Core or building the sample app does not produce the expected artifacts.</exception>
    public static PackagedArcCoreBuildResult Execute()
    {
        var repositoryRoot = GetRepositoryRoot();
        var workingDirectory = CreateWorkingDirectory();
        var packageDirectory = Path.Combine(workingDirectory, "packages");
        var restoredPackagesDirectory = Path.Combine(workingDirectory, "restored-packages");
        var integrationSourceDirectory = Path.Combine(repositoryRoot, "Source", "DotNET", "Arc.Core.Generators.Integration");
        var integrationWorkingDirectory = Path.Combine(workingDirectory, "Arc.Core.Generators.Integration");
        var sampleWorkingDirectory = Path.Combine(integrationWorkingDirectory, "SampleApp");

        Directory.CreateDirectory(packageDirectory);
        Directory.CreateDirectory(restoredPackagesDirectory);
        CopyDirectory(integrationSourceDirectory, integrationWorkingDirectory);

        var arcCoreProjectPath = Path.Combine(repositoryRoot, "Source", "DotNET", "Arc.Core", "Arc.Core.csproj");
        RunDotNet(repositoryRoot, $"pack \"{arcCoreProjectPath}\" -c Release --output \"{packageDirectory}\" -p:IncludeSymbols=false -p:IncludeSource=false");

        var packagePath = Directory.GetFiles(packageDirectory, "Cratis.Arc.Core.*.nupkg", SearchOption.TopDirectoryOnly)
            .OrderDescending()
            .FirstOrDefault()
            ?? throw new PackageIntegrationFailure("Expected a packed Cratis.Arc.Core nupkg to be created.");

        var sampleProjectPath = Path.Combine(sampleWorkingDirectory, "SampleApp.csproj");
        RunDotNet(sampleWorkingDirectory, $"restore \"{sampleProjectPath}\" --source \"{packageDirectory}\" --source https://api.nuget.org/v3/index.json --packages \"{restoredPackagesDirectory}\"");
        RunDotNet(sampleWorkingDirectory, $"build \"{sampleProjectPath}\" --no-restore");

        var generatedFilePath = Directory.GetFiles(sampleWorkingDirectory, "GeneratedQueryMetadata.g.cs", SearchOption.AllDirectories)
            .SingleOrDefault()
            ?? throw new PackageIntegrationFailure("Expected the sample app build to emit GeneratedQueryMetadata.g.cs.");

        using var package = ZipFile.OpenRead(packagePath);
        var packageEntries = package.Entries.Select(_ => _.FullName).ToArray();

        return new(
            packagePath,
            packageEntries,
            generatedFilePath,
            File.ReadAllText(generatedFilePath),
            workingDirectory);
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

        throw new PackageIntegrationFailure("Could not locate the repository root from the integration spec output directory.");
    }

    static string CreateWorkingDirectory()
    {
        var workingDirectory = Path.Combine(Path.GetTempPath(), "Cratis.Arc.Core.Generators.Integration", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workingDirectory);
        return workingDirectory;
    }

    static void CopyDirectory(string sourceDirectory, string destinationDirectory)
    {
        Directory.CreateDirectory(destinationDirectory);

        foreach (var directory in Directory.GetDirectories(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceDirectory, directory);
            Directory.CreateDirectory(Path.Combine(destinationDirectory, relativePath));
        }

        foreach (var file in Directory.GetFiles(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceDirectory, file);
            var destinationPath = Path.Combine(destinationDirectory, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
            File.Copy(file, destinationPath, overwrite: true);
        }
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
            throw new PackageIntegrationFailure($"dotnet {arguments} failed with exit code {process.ExitCode}.{Environment.NewLine}{standardOutput}{Environment.NewLine}{standardError}");
        }
    }

    /// <summary>
    /// The exception that is thrown when the packaged Arc.Core integration scenario fails.
    /// </summary>
    /// <param name="message">The failure message.</param>
    sealed class PackageIntegrationFailure(string message) : Exception(message);
}