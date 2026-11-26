// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Arc.Availability.ManifestFile;
using Cratis.Arc.ProxyGenerator.Specs.AvailabilityManifestFixture;

namespace Cratis.Arc.ProxyGenerator.for_Generator;

public class when_generating_a_command_in_a_gated_slice : Specification
{
    string _outputPath = null!;
    string _generatedCommand = null!;
    ManifestFileDocument _manifest = null!;
    int _exitCode;

    void Establish()
    {
        _outputPath = Path.Combine(Path.GetTempPath(), $"arc-availability-manifest-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_outputPath);
    }

    async Task Because()
    {
        var assembly = typeof(DockVessel).Assembly;
        var excluded = assembly.GetTypes()
            .Where(_ => _ != typeof(DockVessel) && _.FullName is not null)
            .Select(_ => _.FullName!)
            .Order(StringComparer.Ordinal);
        var manifestPath = Path.Combine(_outputPath, "manifest", ManifestFileDocument.FileName);
        var startInfo = new ProcessStartInfo
        {
            FileName = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet",
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false
        };
        startInfo.ArgumentList.Add(typeof(Generator).Assembly.Location);
        startInfo.ArgumentList.Add(assembly.Location);
        startInfo.ArgumentList.Add(Path.Combine(_outputPath, "proxies"));
        startInfo.ArgumentList.Add("0");
        startInfo.ArgumentList.Add("--skip-index-generation");
        startInfo.ArgumentList.Add($"--availability-manifest={manifestPath}");
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
            _generatedCommand = await File.ReadAllTextAsync(Directory.GetFiles(Path.Combine(_outputPath, "proxies"), $"{nameof(DockVessel)}.ts", SearchOption.AllDirectories).Single());
            _manifest = ManifestFileDocument.Parse(await File.ReadAllTextAsync(manifestPath));
        }
    }

    ManifestPart Part => _manifest.Parts.Single(_ => _.Name == typeof(DockVessel).FullName);

    void Destroy() => Directory.Delete(_outputPath, true);

    [Fact] void should_generate_the_command() => _exitCode.ShouldEqual(0);
    [Fact] void should_name_the_part_by_its_full_name_on_the_proxy() => _generatedCommand.ShouldContain($"new PartIdentity('{typeof(DockVessel).FullName}'");
    [Fact] void should_import_the_part_identity() => _generatedCommand.ShouldContain("PartIdentity");
    [Fact] void should_not_put_the_gate_on_the_proxy() => _generatedCommand.ShouldNotContain("Preview");
    [Fact] void should_list_the_command_as_a_part() => Part.Name.ShouldEqual(typeof(DockVessel).FullName);
    [Fact] void should_say_it_is_a_command() => Part.Kind.ShouldEqual("command");
    [Fact] void should_give_the_gates_declared_with_attributes() => Part.Gates.Count.ShouldEqual(2);
    [Fact] void should_give_the_audiences_of_the_first_gate() => ((ManifestAny)Part.Gates[0]).Items.Cast<ManifestFor>().Select(_ => _.Audience).ShouldContainOnly("Plus", "Preview");
    [Fact] void should_give_the_block_of_the_second_gate() => Part.Gates[1].ShouldEqual(new ManifestWith("Paid"));
    [Fact] void should_list_the_part_as_suspendable() => _manifest.Suspendable.Select(_ => _.Kind).ShouldContain("part");
}
