// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Cratis.Arc.ProxyGenerator.for_Generator.given;

public class a_validator_runtime_application : Specification
{
    protected string ApplicationPath = null!;
    protected string HarnessPath = null!;
    protected string OutputPath = null!;
    protected string WorkingPath = null!;
    protected string StatePath = null!;
    protected string Output = string.Empty;
    protected int ExitCode;

    void Establish()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (!File.Exists(Path.Combine(root.FullName, "Arc.slnx")) && !File.Exists(Path.Combine(root.FullName, "Arc.sln")))
        {
            root = root.Parent ?? throw new DirectoryNotFoundException("Could not find Arc repository");
        }

        WorkingPath = Path.Combine(root.FullName, ".ai-work", "arc-3584", "fixtures", Guid.NewGuid().ToString("N"));
        var applicationFolder = Path.Combine(WorkingPath, "application");
        Directory.CreateDirectory(applicationFolder);
        var configuration = new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;
        var applicationProjectFolder = Path.Combine(root.FullName, "Source", "DotNET", "Tools", "ProxyGenerator.Specs", "TestArtifacts", "ValidatorRuntimeApplication");
        var fixtureFolder = Path.Combine(applicationProjectFolder, "bin", configuration, "net10.0");
        HarnessPath = Path.Combine(applicationProjectFolder, "Harness", "bin", configuration, "net10.0", "ValidatorRuntimeHarness.dll");
        foreach (var file in Directory.GetFiles(fixtureFolder))
        {
            File.Copy(file, Path.Combine(applicationFolder, Path.GetFileName(file)));
        }

        ApplicationPath = Path.Combine(applicationFolder, "ValidatorRuntimeApplication.dll");
        StatePath = Path.Combine(WorkingPath, "state.json");
        OutputPath = Path.Combine(WorkingPath, "output");
        Directory.CreateDirectory(OutputPath);
        File.WriteAllText(Path.Combine(OutputPath, "index.ts"), "unchanged index");
    }

    protected Task GenerateInFreshProcess() => RunInFreshProcess(typeof(Generator).Assembly.Location, ApplicationPath, OutputPath, "0", "--skip-output-deletion", $"--state-file={StatePath}");

    protected async Task RunInFreshProcess(string assemblyPath, params string[] arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet",
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false
        };
        startInfo.ArgumentList.Add(assemblyPath);
        foreach (var argument in arguments) startInfo.ArgumentList.Add(argument);
        using var process = Process.Start(startInfo)!;
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        try
        {
            await process.WaitForExitAsync(timeout.Token);
            Output = await output + await error;
            ExitCode = process.ExitCode;
        }
        finally
        {
            if (!process.HasExited) process.Kill(entireProcessTree: true);
        }
    }

    void Destroy() => Directory.Delete(WorkingPath, true);
}
