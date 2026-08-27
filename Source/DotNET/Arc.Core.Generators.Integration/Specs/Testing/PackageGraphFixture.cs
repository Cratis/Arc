// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using System.Xml.Linq;

namespace Cratis.Arc.Core.Generators.Integration.Specs.Testing;

/// <summary>
/// Packs the exact-head build outputs once and exercises clean consumers against the resulting local feed.
/// </summary>
public sealed class PackageGraphFixture : IDisposable
{
    const string ArcAnalyzer = "Cratis.Arc.Core.CodeAnalysis.dll";
    const string ArcCodeFix = "Cratis.Arc.Core.CodeAnalysis.CodeFixes.dll";
    const string ArcGenerator = "Cratis.Arc.Core.Generators.dll";
    const string ChronicleAnalyzer = "Cratis.Arc.Chronicle.CodeAnalysis.dll";
    const string ChronicleCodeFix = "Cratis.Arc.Chronicle.CodeAnalysis.CodeFixes.dll";

    static readonly PackageDefinition[] _packageDefinitions =
    [
        new("Cratis.Arc.Core.CodeAnalysis", "Arc.Core.CodeAnalysis.Package/Arc.Core.CodeAnalysis.Package.csproj"),
        new("Cratis.Arc.Chronicle.CodeAnalysis", "Chronicle.CodeAnalysis.Package/Chronicle.CodeAnalysis.Package.csproj"),
        new("Cratis.Arc.Core", "Arc.Core/Arc.Core.csproj"),
        new("Cratis.Arc", "Arc/Arc.csproj"),
        new("Cratis.Arc.Chronicle", "Chronicle/Chronicle.csproj"),
        new("Cratis.Arc.Swagger", "Swagger/Swagger.csproj"),
        new("Cratis.Arc.ProxyGenerator.Build", "Tools/ProxyGenerator.Build/ProxyGenerator.Build.csproj"),
        new("Cratis", "Cratis/Cratis.csproj"),
        new("Cratis.CodeAnalysis", "Cratis.CodeAnalysis/Cratis.CodeAnalysis.csproj")
    ];

    static readonly ConsumerDefinition[] _consumerDefinitions =
    [
        new("arc-code-analysis", ["Cratis.Arc.Core.CodeAnalysis"], 1, 1, 0, 0, 0),
        new("chronicle-code-analysis", ["Cratis.Arc.Chronicle.CodeAnalysis"], 0, 0, 0, 1, 1),
        new("arc-core", ["Cratis.Arc.Core"], 1, 1, 1, 0, 0),
        new("arc", ["Cratis.Arc"], 1, 1, 1, 0, 0),
        new("arc-chronicle", ["Cratis.Arc.Chronicle"], 1, 1, 1, 1, 1),
        new("cratis", ["Cratis"], 1, 1, 1, 1, 1),
        new("code-analysis", ["Cratis.CodeAnalysis"], 1, 1, 0, 1, 1),
        new("arc-core-with-code-analysis", ["Cratis.Arc.Core", "Cratis.CodeAnalysis"], 1, 1, 1, 1, 1),
        new("arc-with-code-analysis", ["Cratis.Arc", "Cratis.CodeAnalysis"], 1, 1, 1, 1, 1),
        new("arc-chronicle-with-code-analysis", ["Cratis.Arc.Chronicle", "Cratis.CodeAnalysis"], 1, 1, 1, 1, 1),
        new("cratis-with-code-analysis", ["Cratis", "Cratis.CodeAnalysis"], 1, 1, 1, 1, 1)
    ];

    readonly string _workingDirectory;
    readonly string _feedDirectory;
    readonly string _packagesDirectory;
    readonly string _globalPackagesDirectory;

    /// <summary>
    /// Initializes a new instance of the <see cref="PackageGraphFixture"/> class.
    /// </summary>
    public PackageGraphFixture()
    {
        RepositoryRoot = FindRepositoryRoot();
        PackageVersion = $"999.0.0-integration-{Guid.NewGuid():N}";
        var physicalTemporaryDirectory = ResolvePhysicalPath(Path.GetTempPath());
        _workingDirectory = Path.Combine(physicalTemporaryDirectory, "Cratis.Arc.PackageGraph.Integration", Guid.NewGuid().ToString("N"));
        _feedDirectory = Path.Combine(_workingDirectory, "feed");
        _packagesDirectory = Path.Combine(_workingDirectory, "packages");
        _globalPackagesDirectory = Environment.GetEnvironmentVariable("NUGET_PACKAGES") ??
                                   Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages");
        Directory.CreateDirectory(_workingDirectory);
        Directory.CreateDirectory(_feedDirectory);
        Directory.CreateDirectory(_packagesDirectory);

        Packages = PackPackages();
        Consumers = _consumerDefinitions.Select(BuildConsumer).ToArray();
        ArcCodeFixResult = DiscoverArcCodeFix();
        ChronicleCodeFixResult = DiscoverChronicleCodeFix();
    }

    /// <summary>
    /// Gets the repository root containing the exact-head build outputs.
    /// </summary>
    public string RepositoryRoot { get; }

    /// <summary>
    /// Gets the unique integration-only package version used by every pack and consumer reference.
    /// </summary>
    public string PackageVersion { get; }

    /// <summary>
    /// Gets the packed package archives by package identifier.
    /// </summary>
    public IReadOnlyDictionary<string, PackageArchive> Packages { get; }

    /// <summary>
    /// Gets the clean consumer build results.
    /// </summary>
    public IReadOnlyCollection<ConsumerBuildResult> Consumers { get; }

    /// <summary>
    /// Gets the SDK-host ARC code-fix discovery result.
    /// </summary>
    public CodeFixHostResult ArcCodeFixResult { get; }

    /// <summary>
    /// Gets the SDK-host ARCCHR code-fix discovery result.
    /// </summary>
    public CodeFixHostResult ChronicleCodeFixResult { get; }

    /// <inheritdoc/>
    public void Dispose() => TryDeleteWorkingDirectory();

    Dictionary<string, PackageArchive> PackPackages()
    {
        foreach (var package in _packageDefinitions)
        {
            var projectPath = Path.Combine(RepositoryRoot, "Source", "DotNET", package.ProjectPath);
            RunDotNet(
                RepositoryRoot,
                [
                    "pack",
                    projectPath,
                    "-c",
                    "Release",
                    "--no-build",
                    "--no-restore",
                    "--output",
                    _feedDirectory,
                    $"-p:Version={PackageVersion}",
                    "-p:EnableSourceControlManagerQueries=false",
                    "-p:EnableSourceLink=false",
                    "-p:EmbedUntrackedSources=false",
                    "-p:IncludeSymbols=false",
                    "-p:IncludeSource=false"
                ],
                isolatedPackages: true);
        }

        return _packageDefinitions.ToDictionary(
            package => package.Id,
            package => ReadPackage(package.Id),
            StringComparer.Ordinal);
    }

    PackageArchive ReadPackage(string packageId)
    {
        var packagePath = Path.Combine(_feedDirectory, $"{packageId}.{PackageVersion}.nupkg");
        if (!File.Exists(packagePath))
        {
            throw new PackageGraphFailure($"Expected package '{packagePath}' to exist.");
        }

        using var package = ZipFile.OpenRead(packagePath);
        var entries = package.Entries.Select(_ => _.FullName).ToArray();
        var nuspecEntry = package.Entries.Single(_ => _.FullName.EndsWith(".nuspec", StringComparison.Ordinal));
        using var nuspecStream = nuspecEntry.Open();
        var nuspec = XDocument.Load(nuspecStream);
        var dependencies = nuspec
            .Descendants()
            .Where(_ => _.Name.LocalName == "dependency")
            .Select(dependency => new PackageDependency(
                dependency.Ancestors().FirstOrDefault(_ => _.Name.LocalName == "group")?.Attribute("targetFramework")?.Value ?? string.Empty,
                dependency.Attribute("id")?.Value ?? string.Empty,
                dependency.Attribute("version")?.Value ?? string.Empty,
                dependency.Attribute("include")?.Value ?? string.Empty,
                dependency.Attribute("exclude")?.Value ?? string.Empty))
            .ToArray();

        return new(packagePath, entries, dependencies);
    }

    ConsumerBuildResult BuildConsumer(ConsumerDefinition definition)
    {
        var consumerDirectory = Path.Combine(_workingDirectory, "consumers", definition.Name);
        Directory.CreateDirectory(consumerDirectory);
        var projectPath = Path.Combine(consumerDirectory, "Consumer.csproj");
        File.WriteAllText(projectPath, CreateConsumerProject(definition.PackageIds));
        var consumerSource = definition.ArcGeneratorCount > 0
            ? CreateRuntimeConsumerSource()
            : "public sealed class Consumer;\n";
        File.WriteAllText(Path.Combine(consumerDirectory, "Consumer.cs"), consumerSource);

        RestoreConsumer(consumerDirectory, projectPath);
        var buildResult = RunDotNet(consumerDirectory, ["build", projectPath, "--no-restore"], isolatedPackages: true);

        var analyzerEvidencePath = Path.Combine(consumerDirectory, "obj", "resolved-analyzers.txt");
        if (!File.Exists(analyzerEvidencePath))
        {
            throw new PackageGraphFailure($"Expected MSBuild analyzer evidence at '{analyzerEvidencePath}'.");
        }

        var resolvedAnalyzerFiles = File.ReadAllLines(analyzerEvidencePath)
            .Where(_ => !string.IsNullOrWhiteSpace(_))
            .ToArray();
        var generatedDirectory = Path.Combine(consumerDirectory, "obj", "Generated");
        var generatedFiles = Directory.Exists(generatedDirectory)
            ? Directory.GetFiles(generatedDirectory, "*.cs", SearchOption.AllDirectories)
            : [];
        var generatedMetadataFiles = generatedFiles
            .Where(_ => Path.GetFileName(_) == "GeneratedQueryMetadata.g.cs")
            .ToArray();
        var generatedMetadata = string.Join(Environment.NewLine, generatedMetadataFiles.Select(File.ReadAllText));
        var outputDirectory = Path.Combine(consumerDirectory, "bin");
        var forbiddenOutputFiles = Directory.GetFiles(outputDirectory, "*.dll", SearchOption.AllDirectories)
            .Where(_ => Path.GetFileName(_).Contains("Workspaces", StringComparison.OrdinalIgnoreCase)
                     || Path.GetFileName(_).Contains("Composition", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        var buildWasClean = buildResult.StandardOutput.Contains("0 Warning(s)", StringComparison.Ordinal) &&
                            buildResult.StandardOutput.Contains("0 Error(s)", StringComparison.Ordinal);

        return new(
            definition,
            buildWasClean,
            RestoredPackagesHaveLocalProvenance(
                Path.Combine(consumerDirectory, "obj", "project.assets.json"),
                definition.PackageIds),
            CountFile(resolvedAnalyzerFiles, ArcAnalyzer),
            CountFile(resolvedAnalyzerFiles, ArcCodeFix),
            CountFile(resolvedAnalyzerFiles, ArcGenerator),
            CountFile(resolvedAnalyzerFiles, ChronicleAnalyzer),
            CountFile(resolvedAnalyzerFiles, ChronicleCodeFix),
            generatedFiles.Count(_ => Path.GetFileName(_) == "CratisArcGeneratedMarker.g.cs"),
            generatedMetadataFiles.Length,
            generatedMetadata.Contains("WeatherReadModel", StringComparison.Ordinal),
            generatedMetadata.Contains("GetByName", StringComparison.Ordinal),
            forbiddenOutputFiles);
    }

    void RestoreConsumer(string consumerDirectory, string projectPath) =>
        RunDotNet(
            consumerDirectory,
            [
                "restore",
                projectPath,
                "--source",
                _feedDirectory,
                "--source",
                "https://api.nuget.org/v3/index.json",
                "--packages",
                _packagesDirectory,
                $"-p:RestoreFallbackFolders={_globalPackagesDirectory}"
            ],
            isolatedPackages: true);

    bool RestoredPackagesHaveLocalProvenance(string assetsPath, IReadOnlyCollection<string> requestedPackageIds)
    {
        using var assets = JsonDocument.Parse(File.ReadAllText(assetsPath));
        var restoredIntegrationPackages = assets.RootElement
            .GetProperty("libraries")
            .EnumerateObject()
            .Select(_ => _.Name)
            .Select(SplitPackageIdentity)
            .Where(_ => _packageDefinitions.Any(package => string.Equals(package.Id, _.Id, StringComparison.OrdinalIgnoreCase)))
            .ToArray();
        var containsEveryRequestedPackage = requestedPackageIds.All(requested =>
            restoredIntegrationPackages.Any(restored => string.Equals(restored.Id, requested, StringComparison.OrdinalIgnoreCase)));

        return containsEveryRequestedPackage && restoredIntegrationPackages.Length > 0 && restoredIntegrationPackages.All(package =>
        {
            if (!string.Equals(package.Version, PackageVersion, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var packedPackage = Path.Combine(_feedDirectory, $"{package.Id}.{PackageVersion}.nupkg");
            var restoredPackage = Path.Combine(
                _packagesDirectory,
                package.Id.ToLowerInvariant(),
                PackageVersion.ToLowerInvariant(),
                $"{package.Id.ToLowerInvariant()}.{PackageVersion.ToLowerInvariant()}.nupkg");
            return File.Exists(restoredPackage) && FilesHaveSameHash(packedPackage, restoredPackage);
        });
    }

    CodeFixHostResult DiscoverArcCodeFix()
    {
        const string original = """
            using Cratis.Arc.Authorization;

            public enum Role { Admin }

            [Roles("Admin")]
            public sealed class Secured;
            """;
        return DiscoverCodeFix(
            "arc-code-fix",
            "Cratis.Arc.Core",
            "ARC0011",
            original,
            "[Roles(nameof(Role.Admin))]");
    }

    CodeFixHostResult DiscoverChronicleCodeFix()
    {
        const string original = """
            using System;
            using System.ComponentModel.DataAnnotations;
            using Cratis.Arc.Commands.ModelBound;

            [Command]
            public record Do([property: Key] Guid Id)
            {
                public void Handle() { }
            }
            """;
        return DiscoverCodeFix(
            "chronicle-code-fix",
            "Cratis.Arc.Chronicle",
            "ARCCHR0008",
            original,
            "[property: Cratis.Chronicle.Keys.Key]");
    }

    CodeFixHostResult DiscoverCodeFix(string name, string packageId, string diagnosticId, string originalSource, string expectedSource)
    {
        var consumerDirectory = Path.Combine(_workingDirectory, "code-fixes", name);
        Directory.CreateDirectory(consumerDirectory);
        var projectPath = Path.Combine(consumerDirectory, "Consumer.csproj");
        var sourcePath = Path.Combine(consumerDirectory, "Consumer.cs");
        File.WriteAllText(projectPath, CreateConsumerProject([packageId], emitGeneratedFiles: false));
        File.WriteAllText(sourcePath, originalSource);

        RestoreConsumer(consumerDirectory, projectPath);
        var formatResult = RunDotNet(
            consumerDirectory,
            ["format", "analyzers", projectPath, "--no-restore", "--diagnostics", diagnosticId, "--verbosity", "diagnostic"],
            isolatedPackages: true);
        RunDotNet(consumerDirectory, ["build", projectPath, "--no-restore"], isolatedPackages: true);

        var rewrittenSource = File.ReadAllText(sourcePath);
        return new(
            diagnosticId,
            rewrittenSource.Contains(expectedSource, StringComparison.Ordinal),
            formatResult.StandardOutput.Contains("Formatted 1", StringComparison.Ordinal));
    }

    string CreateConsumerProject(IEnumerable<string> packageIds, bool emitGeneratedFiles = true)
    {
        var generatedProperties = emitGeneratedFiles
            ? "<EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles><CompilerGeneratedFilesOutputPath>$(BaseIntermediateOutputPath)Generated</CompilerGeneratedFilesOutputPath>"
            : string.Empty;
        var references = string.Join(
            Environment.NewLine,
            packageIds.Select(_ => $"        <PackageReference Include=\"{_}\" Version=\"{PackageVersion}\" />"));
        return $"""
            <Project Sdk="Microsoft.NET.Sdk">
                <PropertyGroup>
                    <TargetFramework>net10.0</TargetFramework>
                    <ImplicitUsings>enable</ImplicitUsings>
                    <Nullable>enable</Nullable>
                    {generatedProperties}
                </PropertyGroup>
                <ItemGroup>
            {references}
                </ItemGroup>
                <Target Name="CaptureResolvedAnalyzers" BeforeTargets="CoreCompile">
                    <WriteLinesToFile File="$(BaseIntermediateOutputPath)resolved-analyzers.txt"
                                      Lines="@(Analyzer->'%(FullPath)')"
                                      Overwrite="true" />
                </Target>
            </Project>
            """;
    }

    string CreateRuntimeConsumerSource() =>
        """
        using Cratis.Arc.Queries.ModelBound;

        [ReadModel]
        public record WeatherReadModel(string City)
        {
            public static WeatherReadModel GetByName(string city) => new(city);
        }
        """;

    void TryDeleteWorkingDirectory()
    {
        if (!Directory.Exists(_workingDirectory))
        {
            return;
        }

        try
        {
            foreach (var file in Directory.GetFiles(_workingDirectory, "*", SearchOption.AllDirectories))
            {
                File.SetAttributes(file, FileAttributes.Normal);
            }

            Directory.Delete(_workingDirectory, recursive: true);
        }
        catch (IOException)
        {
            // A failed integration run may leave an SDK process holding a temporary file. The operating system's
            // temporary-directory cleanup is the safe fallback; cleanup must not hide the original spec failure.
        }
        catch (UnauthorizedAccessException)
        {
            // See the IOException case above.
        }
    }

    ProcessResult RunDotNet(string workingDirectory, IReadOnlyCollection<string> arguments, bool isolatedPackages = false)
    {
        using var process = new Process
        {
            StartInfo = new()
            {
                FileName = "dotnet",
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            }
        };
        foreach (var argument in arguments)
        {
            process.StartInfo.ArgumentList.Add(argument);
        }
        if (isolatedPackages)
        {
            process.StartInfo.Environment["NUGET_PACKAGES"] = _packagesDirectory;
        }

        process.Start();
        var standardOutputTask = process.StandardOutput.ReadToEndAsync();
        var standardErrorTask = process.StandardError.ReadToEndAsync();
        process.WaitForExit();
        var standardOutput = standardOutputTask.GetAwaiter().GetResult();
        var standardError = standardErrorTask.GetAwaiter().GetResult();
        if (process.ExitCode != 0)
        {
            throw new PackageGraphFailure($"dotnet {string.Join(' ', arguments)} failed with exit code {process.ExitCode}.{Environment.NewLine}{standardOutput}{Environment.NewLine}{standardError}");
        }

        return new(standardOutput, standardError);
    }

    PackageIdentity SplitPackageIdentity(string identity)
    {
        var separator = identity.LastIndexOf('/');
        return separator < 0
            ? new(identity, string.Empty)
            : new(identity[..separator], identity[(separator + 1)..]);
    }

    bool FilesHaveSameHash(string first, string second)
    {
        using var firstStream = File.OpenRead(first);
        using var secondStream = File.OpenRead(second);
        return SHA256.HashData(firstStream).SequenceEqual(SHA256.HashData(secondStream));
    }

    int CountFile(IEnumerable<string> files, string fileName) =>
        files.Count(_ => string.Equals(Path.GetFileName(_), fileName, StringComparison.Ordinal));

    string ResolvePhysicalPath(string path)
    {
        var root = Path.GetPathRoot(path) ?? string.Empty;
        var current = root;
        foreach (var segment in path[root.Length..].Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries))
        {
            var candidate = Path.Combine(current, segment);
            current = new DirectoryInfo(candidate).ResolveLinkTarget(returnFinalTarget: true)?.FullName ?? candidate;
        }

        return current;
    }

    string FindRepositoryRoot()
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

        throw new PackageGraphFailure("Could not locate the repository root from the integration spec output directory.");
    }

    sealed record PackageDefinition(string Id, string ProjectPath);
    sealed record PackageIdentity(string Id, string Version);
    sealed record ProcessResult(string StandardOutput, string StandardError);
}
