// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Cratis.Arc.ProxyGenerator.for_Generator.given;

public class a_runtime_dependency_application : Specification
{
    protected string OutputPath = null!;
    protected string StandardOutput = string.Empty;
    protected string StandardError = string.Empty;
    protected int ExitCode;
    string _applicationPath = null!;

    void Establish()
    {
        var bin = new DirectoryInfo(Path.GetDirectoryName(typeof(a_runtime_dependency_application).Assembly.Location)!);
        var configuration = bin.Parent!;
        var project = configuration.Parent!.Parent!;
        var fixtureBin = Path.Combine(project.FullName, "TestArtifacts", "RuntimeDependencyApplication", "bin", configuration.Name, bin.Name);
        const string application = "Cratis.Arc.ProxyGenerator.Specs.RuntimeDependencyApplication.dll";
        const string library = "Cratis.Arc.ProxyGenerator.Specs.RuntimeDependencyLibrary.dll";
        foreach (var file in new[] { application, library })
        {
            if (!File.Exists(Path.Combine(fixtureBin, file)) || File.Exists(Path.Combine(bin.FullName, file)))
            {
                throw new InvalidOperationException($"Runtime dependency fixture isolation guard failed for {file}");
            }
        }

        _applicationPath = Path.Combine(fixtureBin, application);
        OutputPath = Path.Combine(Path.GetTempPath(), $"arc-runtime-dependency-{Guid.NewGuid():N}");
        Directory.CreateDirectory(OutputPath);
    }

    protected async Task Generate(string excludedCommand)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet",
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false
        };
        foreach (var argument in new[]
        {
            typeof(Generator).Assembly.Location, _applicationPath, OutputPath, "0", "--skip-index-generation",
            $"--exclude-type=Cratis.Arc.ProxyGenerator.Specs.RuntimeDependencyApplication.{excludedCommand}"
        })
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo)!;
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        StandardOutput = await output;
        StandardError = await error;
        ExitCode = process.ExitCode;
    }

    protected string CommandText(string name)
    {
        var file = Directory.GetFiles(OutputPath, $"{name}.ts", SearchOption.AllDirectories).SingleOrDefault();
        return file is null ? string.Empty : File.ReadAllText(file);
    }

    void Destroy() => Directory.Delete(OutputPath, true);
}
