// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Cratis.Arc.Chronicle.CodeAnalysis.Specs.for_ReadProtectionMsBuild.given;

public class a_read_protection_build : for_AttributePrecedencePolicy.given.a_declaration
{
    protected string _buildMode = string.Empty;
    protected bool _expectFailure;
    protected bool _importChroniclePackageProps;
    protected string _output = string.Empty;
    protected int _exitCode;

    protected async Task GenerateAndResolve()
    {
        var root = RepositoryRoot();
        var workspace = Path.Combine(Path.GetTempPath(), "cratis-read-protection-msbuild", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workspace);
        try
        {
            await GenerateIn(root, workspace);
        }
        finally
        {
            Directory.Delete(workspace, recursive: true);
        }
    }

    async Task GenerateIn(string root, string workspace)
    {
        var project = Path.Combine(workspace, "Consumer.csproj");
        var assemblyInfo = Path.Combine(workspace, "Consumer.AssemblyInfo.cs");
        var target = Path.Combine(root, "Source", "DotNET", "Arc.Core", "build", "Cratis.Arc.Core.targets");
        var chroniclePackageProps = Path.Combine(root, "Source", "DotNET", "Chronicle", "build", "Cratis.Arc.Chronicle.props");
        var packageImport = _importChroniclePackageProps ? $"<Import Project=\"{chroniclePackageProps}\" />" : string.Empty;
        var modeProperty = _buildMode.Length == 0 && _importChroniclePackageProps ? string.Empty : $"<CratisReadProtection>{_buildMode}</CratisReadProtection>";
        await File.WriteAllTextAsync(project, $"<Project Sdk=\"Microsoft.NET.Sdk\">{packageImport}<PropertyGroup><TargetFramework>net10.0</TargetFramework><GenerateAssemblyInfo>true</GenerateAssemblyInfo><GeneratedAssemblyInfoFile>{assemblyInfo}</GeneratedAssemblyInfoFile>{modeProperty}</PropertyGroup><Import Project=\"{target}\" /></Project>");
        var start = new ProcessStartInfo("dotnet") { WorkingDirectory = workspace, UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
        start.ArgumentList.Add("msbuild");
        start.ArgumentList.Add(project);
        start.ArgumentList.Add("-target:GenerateAssemblyInfo");
        using var process = Process.Start(start)!;
        var output = process.StandardOutput.ReadToEndAsync();
        var errors = process.StandardError.ReadToEndAsync();
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        try
        {
            await process.WaitForExitAsync(deadline.Token);
        }
        finally
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }

        _exitCode = process.ExitCode;
        _output = await output + await errors;
        if (_expectFailure)
        {
            return;
        }
        Assert.True(_exitCode == 0, _output);
        _source = await File.ReadAllTextAsync(assemblyInfo) + " public class Command; public class Model;";
        Resolve();
    }

    static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Source", "DotNET", "Arc.Core", "build", "Cratis.Arc.Core.targets")))
        {
            directory = directory.Parent;
        }

        return directory!.FullName;
    }
}
