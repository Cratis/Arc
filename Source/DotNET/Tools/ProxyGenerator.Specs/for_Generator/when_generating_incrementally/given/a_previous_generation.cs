// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Arc.ProxyGenerator.Specs.SourceFileResolverFixture;

namespace Cratis.Arc.ProxyGenerator.for_Generator.when_generating_incrementally.given;

public class a_previous_generation : Specification
{
    protected string Folder;
    protected string OutputPath;
    protected string StateFile;
    protected string AssemblyFile;
    protected string GeneratedFile;
    protected Dictionary<string, byte[]> OriginalBytes;
    protected Dictionary<string, DateTime> OriginalTimes;
    protected (int ExitCode, string Output, string Error) Result;

    async Task Establish()
    {
        Folder = Path.Combine(Path.GetTempPath(), $"arc-incremental-{Guid.NewGuid():N}");
        OutputPath = Path.Combine(Folder, "generated");
        StateFile = Path.Combine(Folder, "state.json");
        AssemblyFile = typeof(DeterministicProxyFirst).Assembly.Location;
        var first = await RunGenerator();
        if (first.ExitCode != 0 || first.Error.Length > 0)
        {
            throw new InvalidOperationException($"Initial generation failed: {first.Output}{first.Error}");
        }
        if (!File.Exists(StateFile))
        {
            throw new InvalidOperationException($"Initial generation did not save state: {first.Output}");
        }
        GeneratedFile = Directory.GetFiles(OutputPath, "DeterministicProxyFirst.ts", SearchOption.AllDirectories).Single();
        OriginalBytes = Directory.GetFiles(OutputPath, "*.ts", SearchOption.AllDirectories).ToDictionary(path => path, File.ReadAllBytes);
        OriginalTimes = OriginalBytes.Keys.ToDictionary(path => path, File.GetLastWriteTimeUtc);
    }

    protected async Task<(int ExitCode, string Output, string Error)> RunGenerator(bool useState = true, params string[] extraArguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        foreach (var argument in new[] { typeof(Generator).Assembly.Location, AssemblyFile, OutputPath, "0", "--library-mode", "--skip-output-deletion", "--use-source-file-as-output-file" })
        {
            startInfo.ArgumentList.Add(argument);
        }
        if (useState) startInfo.ArgumentList.Add($"--state-file={StateFile}");
        foreach (var argument in extraArguments) startInfo.ArgumentList.Add(argument);
        foreach (var type in typeof(DeterministicProxyFirst).Assembly.GetTypes()
            .Where(type => type != typeof(DeterministicProxyFirst) && type != typeof(DeterministicProxySecond)))
        {
            startInfo.ArgumentList.Add($"--exclude-type={type.FullName}");
        }

        using var process = Process.Start(startInfo)!;
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        return (process.ExitCode, await output, await error);
    }

    void Destroy() => Directory.Delete(Folder, true);
}
