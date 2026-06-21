// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Cratis.Arc.Core.Generators.Integration.Specs.Testing;

/// <summary>
/// Packs the Cratis meta package and Cratis.Arc.MongoDB (with their in-repo dependencies) into a local feed and
/// builds a consumer that references both, reproducing the analyzer double-load that caused CS0101.
/// </summary>
internal static class CratisMetaConsumerBuildScenario
{
    /// <summary>
    /// The in-repo package closure a consumer of Cratis + Cratis.Arc.MongoDB pulls. Externals (Cratis.Chronicle,
    /// Cratis.Fundamentals, MongoDB.Driver, ...) are restored from nuget.org.
    /// </summary>
    static readonly string[] _projectsToPack =
    [
        Path.Combine("Source", "DotNET", "Arc.Core", "Arc.Core.csproj"),
        Path.Combine("Source", "DotNET", "Arc", "Arc.csproj"),
        Path.Combine("Source", "DotNET", "Chronicle", "Chronicle.csproj"),
        Path.Combine("Source", "DotNET", "Swagger", "Swagger.csproj"),
        Path.Combine("Source", "DotNET", "Tools", "ProxyGenerator.Build", "ProxyGenerator.Build.csproj"),
        Path.Combine("Source", "DotNET", "MongoDB", "MongoDB.csproj"),
        Path.Combine("Source", "DotNET", "Cratis", "Cratis.csproj"),
    ];

    /// <summary>
    /// Packs the closure into a local feed, restores the consumer from it, and builds the consumer.
    /// </summary>
    /// <returns>A <see cref="CratisMetaConsumerBuildResult"/> describing whether the consumer built and its output.</returns>
    /// <exception cref="MetaConsumerIntegrationFailure">Thrown when packing or restoring the consumer fails (the build itself does not throw — its outcome is the result under test).</exception>
    public static CratisMetaConsumerBuildResult Execute()
    {
        var repositoryRoot = GetRepositoryRoot();
        var workingDirectory = Path.Combine(Path.GetTempPath(), "Cratis.Arc.MetaConsumer.Integration", Guid.NewGuid().ToString("N"));
        var packageDirectory = Path.Combine(workingDirectory, "packages");
        var restoredPackagesDirectory = Path.Combine(workingDirectory, "restored-packages");
        var consumerSourceDirectory = Path.Combine(repositoryRoot, "Source", "DotNET", "Arc.Core.Generators.Integration", "MetaConsumerApp");
        var consumerWorkingDirectory = Path.Combine(workingDirectory, "MetaConsumerApp");

        Directory.CreateDirectory(packageDirectory);
        Directory.CreateDirectory(restoredPackagesDirectory);
        CopyDirectory(consumerSourceDirectory, consumerWorkingDirectory);

        foreach (var project in _projectsToPack)
        {
            RunDotNet(repositoryRoot, $"pack \"{Path.Combine(repositoryRoot, project)}\" -c Release --output \"{packageDirectory}\" -p:IncludeSymbols=false -p:IncludeSource=false", throwOnFailure: true);
        }

        var consumerProjectPath = Path.Combine(consumerWorkingDirectory, "MetaConsumerApp.csproj");
        RunDotNet(consumerWorkingDirectory, $"restore \"{consumerProjectPath}\" --source \"{packageDirectory}\" --source https://api.nuget.org/v3/index.json --packages \"{restoredPackagesDirectory}\"", throwOnFailure: true);

        var (exitCode, output) = RunDotNet(consumerWorkingDirectory, $"build \"{consumerProjectPath}\" --no-restore", throwOnFailure: false);

        return new(exitCode == 0, output, workingDirectory);
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

        throw new MetaConsumerIntegrationFailure("Could not locate the repository root from the integration spec output directory.");
    }

    static void CopyDirectory(string sourceDirectory, string destinationDirectory)
    {
        Directory.CreateDirectory(destinationDirectory);

        foreach (var file in Directory.GetFiles(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            // Skip any previously built output so the consumer builds from a clean state.
            var relativePath = Path.GetRelativePath(sourceDirectory, file);
            if (relativePath.StartsWith($"bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal) ||
                relativePath.StartsWith($"obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal) ||
                relativePath.StartsWith($"Generated{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            {
                continue;
            }

            var destinationPath = Path.Combine(destinationDirectory, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
            File.Copy(file, destinationPath, overwrite: true);
        }
    }

    static (int ExitCode, string Output) RunDotNet(string workingDirectory, string arguments, bool throwOnFailure)
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
        var output = $"{standardOutput}{Environment.NewLine}{standardError}";

        if (throwOnFailure && process.ExitCode != 0)
        {
            throw new MetaConsumerIntegrationFailure($"dotnet {arguments} failed with exit code {process.ExitCode}.{Environment.NewLine}{output}");
        }

        return (process.ExitCode, output);
    }

    /// <summary>
    /// The exception that is thrown when the Cratis meta consumer integration scenario cannot complete its prerequisites.
    /// </summary>
    /// <param name="message">The failure message.</param>
    sealed class MetaConsumerIntegrationFailure(string message) : Exception(message);
}
