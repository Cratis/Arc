// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Text;
using Cratis.Arc.ProxyGenerator.Specs.SourceFileResolverFixture;

namespace Cratis.Arc.ProxyGenerator.for_Generator;

public class when_generating_combined_output_repeatedly : Specification, IDisposable
{
    static readonly DateTime _existingGenerationTime = new(2024, 12, 8, 10, 0, 0, DateTimeKind.Utc);

    string _temporaryPath = null!;
    string _outputPath = null!;
    string _generatedFilePath = null!;
    byte[] _firstGeneration = null!;
    byte[] _secondGeneration = null!;
    byte[] _changedGeneration = null!;
    GeneratedFileMetadata _firstMetadata = null!;
    GeneratedFileMetadata _secondMetadata = null!;
    GeneratedFileMetadata _changedMetadata = null!;
    string _changedContent = null!;
    int _firstExitCode = -1;
    int _secondExitCode = -1;
    int _changedExitCode = -1;

    void Establish()
    {
        _temporaryPath = Path.Combine(Path.GetTempPath(), $"arc-deterministic-proxy-{Guid.NewGuid():N}");
        _outputPath = Path.Combine(_temporaryPath, "generated");
        Directory.CreateDirectory(_outputPath);
    }

    async Task Because()
    {
        _firstExitCode = await RunGenerator();
        if (_firstExitCode != 0)
        {
            return;
        }

        _generatedFilePath = Directory.GetFiles(_outputPath, "DeterministicProxyFirst.ts", SearchOption.AllDirectories).Single();
        _firstGeneration = await File.ReadAllBytesAsync(_generatedFilePath);
        _firstMetadata = ParseMetadata(_firstGeneration);

        _secondExitCode = await RunGenerator();
        if (_secondExitCode != 0)
        {
            return;
        }

        _secondGeneration = await File.ReadAllBytesAsync(_generatedFilePath);
        _secondMetadata = ParseMetadata(_secondGeneration);

        var existingContent = Encoding.UTF8.GetString(_secondGeneration);
        var metadataLineEnd = existingContent.IndexOf('\n');
        var semanticContent = existingContent[(metadataLineEnd + 1)..];
        var existingMetadata = new GeneratedFileMetadata(_secondMetadata.SourceTypeName, _existingGenerationTime, _secondMetadata.ContentHash);
        await File.WriteAllTextAsync(_generatedFilePath, $"{existingMetadata.ToCommentLine()}{Environment.NewLine}{semanticContent}");

        _changedExitCode = await RunGenerator(mapStringToNumber: true);
        if (_changedExitCode != 0)
        {
            return;
        }

        _changedGeneration = await File.ReadAllBytesAsync(_generatedFilePath);
        _changedMetadata = ParseMetadata(_changedGeneration);
        _changedContent = Encoding.UTF8.GetString(_changedGeneration);
    }

    [Fact] void should_complete_the_first_generation() => _firstExitCode.ShouldEqual(0);
    [Fact] void should_complete_the_second_generation() => _secondExitCode.ShouldEqual(0);
    [Fact] void should_complete_the_changed_generation() => _changedExitCode.ShouldEqual(0);
    [Fact] void should_generate_the_first_type_in_the_combined_file() => Encoding.UTF8.GetString(_firstGeneration).ShouldContain($"export class {nameof(DeterministicProxyFirst)}");
    [Fact] void should_generate_the_second_type_in_the_combined_file() => Encoding.UTF8.GetString(_firstGeneration).ShouldContain($"export class {nameof(DeterministicProxySecond)}");
    [Fact] void should_produce_byte_identical_unchanged_output() => _secondGeneration.SequenceEqual(_firstGeneration).ShouldBeTrue();
    [Fact] void should_preserve_the_generation_timestamp_for_unchanged_output() => _secondMetadata.GeneratedTime.ShouldEqual(_firstMetadata.GeneratedTime);
    [Fact] void should_preserve_the_content_hash_for_unchanged_output() => _secondMetadata.ContentHash.ShouldEqual(_firstMetadata.ContentHash);
    [Fact] void should_update_the_semantic_content() => _changedContent.ShouldContain("name!: number;");
    [Fact] void should_update_the_content_hash() => _changedMetadata.ContentHash.ShouldNotEqual(_secondMetadata.ContentHash);
    [Fact] void should_update_the_generation_timestamp() => (_changedMetadata.GeneratedTime.ToUniversalTime() > _existingGenerationTime).ShouldBeTrue();

    public void Dispose()
    {
        if (Directory.Exists(_temporaryPath))
        {
            Directory.Delete(_temporaryPath, true);
        }
    }

    async Task<int> RunGenerator(bool mapStringToNumber = false)
    {
        var generatorAssembly = typeof(Generator).Assembly.Location;
        var fixtureAssembly = typeof(DeterministicProxyFirst).Assembly.Location;
        var typesToGenerate = new HashSet<Type>
        {
            typeof(DeterministicProxyFirst),
            typeof(DeterministicProxySecond)
        };
        var excludedTypeNames = typeof(DeterministicProxyFirst).Assembly.GetTypes()
            .Where(type => !typesToGenerate.Contains(type) && type.FullName is not null)
            .Select(type => type.FullName)
            .Order(StringComparer.Ordinal);

        var startInfo = new ProcessStartInfo
        {
            FileName = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet",
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false
        };
        startInfo.ArgumentList.Add(generatorAssembly);
        startInfo.ArgumentList.Add(fixtureAssembly);
        startInfo.ArgumentList.Add(_outputPath);
        startInfo.ArgumentList.Add("0");
        startInfo.ArgumentList.Add("--library-mode");
        startInfo.ArgumentList.Add("--skip-output-deletion");
        startInfo.ArgumentList.Add("--skip-index-generation");
        startInfo.ArgumentList.Add("--use-source-file-as-output-file");
        if (mapStringToNumber)
        {
            startInfo.ArgumentList.Add("--type-to-ts=System.String=number");
        }
        foreach (var typeName in excludedTypeNames)
        {
            startInfo.ArgumentList.Add($"--exclude-type={typeName}");
        }

        using var process = Process.Start(startInfo)!;
        var standardOutput = process.StandardOutput.ReadToEndAsync();
        var standardError = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        await Task.WhenAll(standardOutput, standardError);
        return process.ExitCode;
    }

    static GeneratedFileMetadata ParseMetadata(byte[] content)
    {
        var firstLine = Encoding.UTF8.GetString(content).Split('\n')[0].TrimEnd('\r');
        GeneratedFileMetadata.TryParse(firstLine, out var metadata).ShouldBeTrue();
        return metadata!;
    }
}
