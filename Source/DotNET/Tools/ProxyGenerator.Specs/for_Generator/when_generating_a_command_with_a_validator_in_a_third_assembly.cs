// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Arc.ProxyGenerator.Specs.ReferencedConceptCommand;

namespace Cratis.Arc.ProxyGenerator.for_Generator;

public class when_generating_a_command_with_a_validator_in_a_third_assembly : Specification
{
    string _outputPath = null!;
    string _generatedCommand = null!;
    int _exitCode;

    void Establish()
    {
        _outputPath = Path.Combine(Path.GetTempPath(), $"arc-third-assembly-validator-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_outputPath);
    }

    async Task Because()
    {
        var assembly = typeof(CommandUsingExternalName).Assembly;
        var excluded = assembly.GetTypes()
            .Where(_ => _ != typeof(CommandUsingExternalName) && _.FullName is not null)
            .Select(_ => _.FullName!)
            .Order(StringComparer.Ordinal);
        var startInfo = new ProcessStartInfo
        {
            FileName = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet",
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false
        };
        startInfo.ArgumentList.Add(typeof(Generator).Assembly.Location);
        startInfo.ArgumentList.Add(assembly.Location);
        startInfo.ArgumentList.Add(_outputPath);
        startInfo.ArgumentList.Add("0");
        startInfo.ArgumentList.Add("--skip-index-generation");
        foreach (var name in excluded)
        {
            startInfo.ArgumentList.Add($"--exclude-type={name}");
        }

        using var process = Process.Start(startInfo)!;
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        await Task.WhenAll(output, error);
        _exitCode = process.ExitCode;
        if (_exitCode == 0)
        {
            _generatedCommand = await File.ReadAllTextAsync(Directory.GetFiles(_outputPath, $"{nameof(CommandUsingExternalName)}.ts", SearchOption.AllDirectories).Single());
        }
    }

    void Destroy() => Directory.Delete(_outputPath, true);

    [Fact] void should_generate_the_command() => _exitCode.ShouldEqual(0);
    [Fact] void should_emit_the_validator_rule_from_the_third_assembly() => _generatedCommand.ShouldContain("maxLength(37)");
    [Fact] void should_emit_the_validator_message_from_the_third_assembly() => _generatedCommand.ShouldContain("External name is too long");
}
