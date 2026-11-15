// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Cratis.Arc.ProxyGenerator.for_Generator;

public class when_generating_from_an_assembly_referencing_aspnetcore : Specification
{
    string _assemblyPath = null!;
    string _outputPath = null!;
    string _generatedCommand = string.Empty;
    string _standardError = string.Empty;
    int _exitCode;

    void Establish()
    {
        var bin = new DirectoryInfo(Path.GetDirectoryName(typeof(when_generating_from_an_assembly_referencing_aspnetcore).Assembly.Location)!);
        var configuration = bin.Parent!;
        var project = configuration.Parent!.Parent!;
        const string assembly = "Cratis.Arc.ProxyGenerator.Specs.AspNetCoreDiscovery.dll";
        _assemblyPath = Path.Combine(project.FullName, "TestArtifacts", "AspNetCoreDiscovery", "bin", configuration.Name, bin.Name, assembly);
        if (!File.Exists(_assemblyPath) || File.Exists(Path.Combine(bin.FullName, assembly)))
        {
            throw new InvalidOperationException("ASP.NET Core fixture isolation guard failed");
        }

        _outputPath = Path.Combine(Path.GetTempPath(), $"arc-aspnetcore-validator-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_outputPath);
    }

    async Task Because()
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet",
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false
        };
        foreach (var argument in new[] { typeof(Generator).Assembly.Location, _assemblyPath, _outputPath, "0", "--skip-index-generation" })
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo)!;
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        await output;
        _standardError = await error;
        _exitCode = process.ExitCode;
        if (_exitCode == 0)
        {
            _generatedCommand = await File.ReadAllTextAsync(Directory.GetFiles(_outputPath, "AspNetCoreDiscoveryCommand.ts", SearchOption.AllDirectories).Single());
        }
    }

    void Destroy() => Directory.Delete(_outputPath, true);

    [Fact] void should_exit_successfully() => _exitCode.ShouldEqual(0);
    [Fact] void should_not_report_validator_discovery_errors() => _standardError.ShouldBeEmpty();
    [Fact] void should_generate_the_command_proxy() => _generatedCommand.ShouldContain("export class AspNetCoreDiscoveryCommand");
    [Fact] void should_extract_the_validator_rule() => _generatedCommand.ShouldContain("Value is required");
}
