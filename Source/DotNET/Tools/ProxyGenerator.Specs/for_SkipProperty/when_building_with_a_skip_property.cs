// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Xml.Linq;
using Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;

namespace Cratis.Arc.ProxyGenerator.for_SkipProperty;

public class when_building_with_a_skip_property : Specification
{
    readonly string _directory = Path.Combine(Path.GetTempPath(), $"cratis-skip-{Guid.NewGuid():N}");
    string _default = string.Empty;
    string _skip = string.Empty;
    string _alias = string.Empty;
    string _falseValue = string.Empty;

    async Task Because()
    {
        Directory.CreateDirectory(_directory);
        _default = await Run();
        _skip = await Run("-p:CratisProxiesSkip=true");
        _alias = await Run("-p:DisableProxyGenerator=true");
        _falseValue = await Run("-p:CratisProxiesSkip=false");
    }

    void Destroy() => Directory.Delete(_directory, true);

    async Task<string> Run(params string[] arguments)
    {
        var targets = Path.Combine(JavaScriptResources.RepoRoot, "Source/DotNET/Tools/ProxyGenerator.Build/build/Cratis.Arc.ProxyGenerator.Build.targets");
        var project = Path.Combine(_directory, "skip.proj");
        var document = new XDocument(new XElement(
            "Project",
            new XElement(
                "PropertyGroup",
                new XElement("OutputPath", "bin/"),
                new XElement("AssemblyName", "Missing"),
                new XElement("IntermediateOutputPath", "obj/"),
                new XElement("CratisProxiesOutputPath", Path.Combine(_directory, "proxies"))),
            new XElement("Import", new XAttribute("Project", targets)),
            new XElement("Target", new XAttribute("Name", "AfterBuild"))));
        await File.WriteAllTextAsync(project, document.ToString());
        var start = new ProcessStartInfo("dotnet")
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        foreach (var argument in new[] { "msbuild", project, "-nologo", "-t:AfterBuild;CratisProxyGenerator" }.Concat(arguments))
        {
            start.ArgumentList.Add(argument);
        }

        using var process = Process.Start(start)!;
        var output = process.StandardOutput.ReadToEndAsync();
        var errors = process.StandardError.ReadToEndAsync();
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        await process.WaitForExitAsync(deadline.Token);
        return await output + await errors + $"\nexit={process.ExitCode}";
    }

    [Fact] void should_generate_by_default() => _default.ShouldContain("ProxiesOutputPath :");
    [Fact] void should_skip_generation_when_the_skip_property_is_true() => (_skip.EndsWith("exit=0", StringComparison.Ordinal) && !_skip.Contains("ProxiesOutputPath :", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_skip_generation_for_the_alias() => (_alias.EndsWith("exit=0", StringComparison.Ordinal) && !_alias.Contains("ProxiesOutputPath :", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_generate_when_the_skip_property_is_false() => _falseValue.ShouldContain("ProxiesOutputPath :");
}
