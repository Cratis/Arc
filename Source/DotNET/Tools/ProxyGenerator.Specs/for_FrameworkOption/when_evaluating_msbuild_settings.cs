// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Xml.Linq;
using Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;

namespace Cratis.Arc.ProxyGenerator.for_FrameworkOption;

public class when_evaluating_msbuild_settings : Specification
{
    readonly string _directory = Path.Combine(Path.GetTempPath(), $"cratis-framework-{Guid.NewGuid():N}");
    string _default = string.Empty;
    string _project = string.Empty;
    string _override = string.Empty;
    string _command = string.Empty;

    async Task Because()
    {
        Directory.CreateDirectory(_directory);
        _default = await Evaluate(null, null);
        _project = await Evaluate("none", null);
        _override = await Evaluate("react", "none");
        var targets = XDocument.Load(Path.Combine(JavaScriptResources.RepoRoot, "Source/DotNET/Tools/ProxyGenerator.Build/build/Cratis.Arc.ProxyGenerator.Build.targets"));
        _command = targets.Descendants().Single(element => element.Name.LocalName == "Exec").Attribute("Command")!.Value;
    }

    void Destroy() => Directory.Delete(_directory, true);

    async Task<string> Evaluate(string? projectValue, string? overrideValue)
    {
        var props = Path.Combine(JavaScriptResources.RepoRoot, "Source/DotNET/Tools/ProxyGenerator.Build/build/Cratis.Arc.ProxyGenerator.Build.props");
        var project = Path.Combine(_directory, "framework.proj");
        var document = new XDocument(new XElement("Project",
            projectValue is null ? null : new XElement("PropertyGroup", new XElement("CratisProxiesFramework", projectValue)),
            new XElement("Import", new XAttribute("Project", props))));
        await File.WriteAllTextAsync(project, document.ToString());
        var start = new ProcessStartInfo("dotnet")
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        foreach (var argument in new[] { "msbuild", project, "-nologo", "-getProperty:CratisProxiesFramework" }) start.ArgumentList.Add(argument);
        if (overrideValue is not null) start.ArgumentList.Add($"-p:CratisProxiesFramework={overrideValue}");
        using var process = Process.Start(start)!;
        var output = process.StandardOutput.ReadToEndAsync();
        var errors = process.StandardError.ReadToEndAsync();
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await process.WaitForExitAsync(deadline.Token);
        if (process.ExitCode != 0) throw new InvalidOperationException(await errors);
        return (await output).Trim();
    }

    [Fact] void should_default_to_react() => _default.ShouldEqual("react");
    [Fact] void should_keep_the_project_selection() => _project.ShouldEqual("none");
    [Fact] void should_honor_the_command_line_override() => _override.ShouldEqual("none");
    [Fact] void should_forward_the_selection_to_the_executable() => _command.ShouldContain("--framework=\"$(CratisProxiesFramework)\"");
}
