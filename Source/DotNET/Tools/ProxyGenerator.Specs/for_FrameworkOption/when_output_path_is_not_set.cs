// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Xml.Linq;
using Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;

namespace Cratis.Arc.ProxyGenerator.for_FrameworkOption;

public class when_output_path_is_not_set : Specification
{
    readonly string _directory = Path.Combine(Path.GetTempPath(), $"cratis-output-path-{Guid.NewGuid():N}");
    const string Message = "CratisProxiesOutputPath is not set";
    string _default = string.Empty;
    string _configured = string.Empty;
    string _suppressed = string.Empty;
    string _notSuppressed = string.Empty;
    string _warningsAsErrors = string.Empty;

    async Task Because()
    {
        Directory.CreateDirectory(_directory);
        _default = await Run();
        _configured = await Run(Path.Combine(_directory, "proxies"), "-p:CratisProxiesSkip=true");
        _suppressed = await Run(null, "-p:CratisProxiesSuppressOutputPathMessage=true");
        _notSuppressed = await Run(null, "-p:CratisProxiesSuppressOutputPathMessage=false");
        _warningsAsErrors = await Run(null, "-warnaserror", "-p:TreatWarningsAsErrors=true");
    }

    void Destroy() => Directory.Delete(_directory, true);

    async Task<string> Run(string? outputPath = null, params string[] arguments)
    {
        var targets = Path.Combine(JavaScriptResources.RepoRoot, "Source/DotNET/Tools/ProxyGenerator.Build/build/Cratis.Arc.ProxyGenerator.Build.targets");
        var project = Path.Combine(_directory, "output-path.proj");
        var properties = new XElement(
            "PropertyGroup",
            new XElement("OutputPath", "bin/"),
            new XElement("AssemblyName", "Missing"),
            new XElement("IntermediateOutputPath", "obj/"));
        if (outputPath is not null)
        {
            properties.Add(new XElement("CratisProxiesOutputPath", outputPath));
        }

        var document = new XDocument(new XElement(
            "Project",
            properties,
            new XElement("Import", new XAttribute("Project", targets)),
            new XElement("Target", new XAttribute("Name", "AfterBuild"))));
        await File.WriteAllTextAsync(project, document.ToString());
        var start = new ProcessStartInfo("dotnet")
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        foreach (var argument in new[] { "msbuild", project, "-nologo", "-t:AfterBuild" }.Concat(arguments))
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

    [Fact] void should_explain_that_generation_is_off() => _default.ShouldContain(Message);
    [Fact] void should_succeed_without_an_output_path() => _default.EndsWith("exit=0", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_not_run_the_generator() => _default.ShouldNotContain("ProxiesOutputPath :");
    [Fact] void should_not_emit_the_message_with_an_output_path() => _configured.ShouldNotContain(Message);
    [Fact] void should_succeed_with_an_output_path_and_generation_skipped() => _configured.EndsWith("exit=0", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_honor_message_suppression() => _suppressed.ShouldNotContain(Message);
    [Fact] void should_succeed_with_message_suppression() => _suppressed.EndsWith("exit=0", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_emit_the_message_with_suppression_disabled() => _notSuppressed.ShouldContain(Message);
    [Fact] void should_succeed_with_suppression_disabled() => _notSuppressed.EndsWith("exit=0", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_still_emit_the_message_with_warnings_as_errors() => _warningsAsErrors.ShouldContain(Message);
    [Fact] void should_not_promote_the_message_to_an_error() => _warningsAsErrors.EndsWith("exit=0", StringComparison.Ordinal).ShouldBeTrue();
}
