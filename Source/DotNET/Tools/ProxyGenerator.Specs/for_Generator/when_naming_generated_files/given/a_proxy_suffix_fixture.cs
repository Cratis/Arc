// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Arc.ProxyGenerator.Specs.ProxyFileSuffixFixture;

namespace Cratis.Arc.ProxyGenerator.for_Generator.when_naming_generated_files.given;

/// <summary>
/// Runs the real generator command line over the proxy file suffix fixture and reads back what it wrote.
/// </summary>
public class a_proxy_suffix_fixture : Specification, IDisposable
{
    /// <summary>
    /// The hand-written module the fixture maps <see cref="decimal"/> to; it must never be suffixed.
    /// </summary>
    protected const string HandWrittenModule = "./handwritten/Money";

    protected string _outputPath = null!;
    string _temporaryPath = null!;

    void Establish()
    {
        _temporaryPath = Path.Combine(Path.GetTempPath(), $"arc-proxy-suffix-{Guid.NewGuid():N}");
        _outputPath = Path.Combine(_temporaryPath, "generated");
        Directory.CreateDirectory(_outputPath);
    }

    public void Dispose()
    {
        if (Directory.Exists(_temporaryPath))
        {
            Directory.Delete(_temporaryPath, true);
        }
    }

    protected string[] GeneratedFileNames() =>
        Directory.GetFiles(_outputPath, "*.ts", SearchOption.AllDirectories)
            .Where(file => Path.GetFileName(file) != "index.ts")
            .Select(Path.GetFileName)
            .Order(StringComparer.Ordinal)
            .ToArray()!;

    protected string ContentOf(string fileName) =>
        File.ReadAllText(Directory.GetFiles(_outputPath, fileName, SearchOption.AllDirectories).Single());

    protected async Task<int> RunGenerator(bool useProxyFileSuffix, bool useSourceFileAsOutputFile = false)
    {
        var fixtureTypes = new HashSet<Type> { typeof(ProxySuffixOrder), typeof(ProxySuffixLine) };
        var startInfo = new ProcessStartInfo
        {
            FileName = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet",
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false
        };
        startInfo.ArgumentList.Add(typeof(Generator).Assembly.Location);
        startInfo.ArgumentList.Add(typeof(ProxySuffixOrder).Assembly.Location);
        startInfo.ArgumentList.Add(_outputPath);
        startInfo.ArgumentList.Add("0");
        startInfo.ArgumentList.Add("--library-mode");
        startInfo.ArgumentList.Add("--skip-output-deletion");
        startInfo.ArgumentList.Add($"--type-to-ts=System.Decimal=Money={HandWrittenModule}");
        if (useProxyFileSuffix)
        {
            startInfo.ArgumentList.Add("--use-proxy-file-suffix");
        }

        if (useSourceFileAsOutputFile)
        {
            startInfo.ArgumentList.Add("--use-source-file-as-output-file");
        }

        foreach (var typeName in typeof(ProxySuffixOrder).Assembly.GetTypes()
                     .Where(type => !fixtureTypes.Contains(type) && type.FullName is not null)
                     .Select(type => type.FullName!)
                     .Order(StringComparer.Ordinal))
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
}
