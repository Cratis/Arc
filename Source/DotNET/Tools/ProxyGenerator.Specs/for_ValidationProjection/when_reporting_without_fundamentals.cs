// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Arc.ProxyGenerator.Specs.ActualDependency;

namespace Cratis.Arc.ProxyGenerator.for_ValidationProjection;

public class when_reporting_without_fundamentals : Specification
{
    string _folder;
    string _assembly;
    (int ExitCode, string Output, string Error) _text;
    (int ExitCode, string Output, string Error) _json;

    async Task Establish()
    {
        _folder = Path.Combine(Path.GetTempPath(), $"arc-report-without-concepts-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_folder);
        var fixture = typeof(MismatchedAssemblyMarker).Assembly;
        var name = fixture.GetName().Name;
        _assembly = Path.Combine(_folder, $"{name}.dll");
        await File.WriteAllBytesAsync(_assembly, await File.ReadAllBytesAsync(fixture.Location));
        var dependencies = string.Join(
            "\n",
            "{",
            "  \"runtimeTarget\": { \"name\": \".NETCoreApp,Version=v10.0\" },",
            "  \"targets\": { \".NETCoreApp,Version=v10.0\": {",
            $"    \"{name}/1.0.0\": {{ \"runtime\": {{ \"{name}.dll\": {{}} }} }}",
            "  } },",
            $"  \"libraries\": {{ \"{name}/1.0.0\": {{ \"type\": \"project\", \"serviceable\": false, \"sha512\": \"\" }} }}",
            "}");
        await File.WriteAllTextAsync(Path.ChangeExtension(_assembly, ".deps.json"), dependencies);
    }

    async Task Because()
    {
        var output = Path.Combine(_folder, "generated");
        _text = await ToolProcess.Run(_assembly, output, "--library-mode", "--validation-report=text");
        _json = await ToolProcess.Run(_assembly, output, "--library-mode", "--validation-report=json");
    }

    [Fact] void should_generate_successfully() => _json.ExitCode.ShouldEqual(0);
    [Fact] void should_preserve_the_text_diagnostic() => _text.Output.ShouldContain("Could not load Cratis.Fundamentals, concepts will not be supported");

    [Fact]
    void should_emit_only_one_json_document()
    {
        using var report = JsonDocument.Parse(_json.Output);
        report.RootElement.GetProperty("schemaVersion").GetInt32().ShouldEqual(1);
        report.RootElement.GetProperty("records").GetArrayLength().ShouldEqual(0);
        _json.Output.ShouldNotContain("Could not load");
    }

    void Destroy() => Directory.Delete(_folder, recursive: true);
}
