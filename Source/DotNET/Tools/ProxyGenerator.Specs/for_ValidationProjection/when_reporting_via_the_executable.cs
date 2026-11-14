// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Arc.ProxyGenerator.for_ValidationProjection;

public class when_reporting_via_the_executable : Specification
{
    string _folder;
    string _assembly;
    string[] _options;

    void Establish()
    {
        _folder = Path.Combine(Path.GetTempPath(), $"cratis-report-{Guid.NewGuid():N}");
        _assembly = typeof(ProjectionCommand).Assembly.Location;
        _options = ["--library-mode", .. typeof(ProjectionCommand).Assembly.GetTypes()
            .Where(_ => _ != typeof(ProjectionCommand) && _ != typeof(ProjectionReadModel))
            .Select(_ => $"--exclude-type={_.FullName}")];
    }

    void Destroy()
    {
        if (Directory.Exists(_folder)) Directory.Delete(_folder, recursive: true);
    }

    [Theory]
    [InlineData("text")]
    [InlineData("json")]
    async Task should_preserve_generated_bytes_when_reporting(string format)
    {
        (await ToolProcess.Run(_assembly, _folder, _options)).ExitCode.ShouldEqual(0);
        var bytes = ProxyBytes();
        var result = await ToolProcess.Run(_assembly, _folder, [.. _options, $"--validation-report={format}"]);
        result.ExitCode.ShouldEqual(0);
        ProxyBytes().Keys.Order().ShouldEqual(bytes.Keys.Order());
        ProxyBytes().All(pair => bytes[pair.Key].SequenceEqual(pair.Value)).ShouldBeTrue();
        if (format == "json") AssertSafeJson(result.Output);
        else result.Output.ShouldContain("Validation projection");
    }

    [Fact]
    async Task should_emit_deterministic_flat_json_with_safe_members_only()
    {
        var options = _options.Append("--validation-report=json").ToArray();
        var first = await ToolProcess.Run(_assembly, _folder, options);
        var second = await ToolProcess.Run(_assembly, _folder, options);
        first.ExitCode.ShouldEqual(0);
        second.ExitCode.ShouldEqual(0);
        first.Output.ShouldEqual(second.Output);
        AssertSafeJson(first.Output);
        using var json = JsonDocument.Parse(first.Output);
        var records = json.RootElement.GetProperty("records").EnumerateArray().ToArray();
        records.Any(_ => _.GetProperty("endpoint").GetProperty("kind").GetString() == "command").ShouldBeTrue();
        records.Any(_ => _.GetProperty("endpoint").GetProperty("kind").GetString() == "query").ShouldBeTrue();
        records.Any(_ => _.GetProperty("reason").GetString() == "no-projectable-rules" && _.GetProperty("member").ValueKind == JsonValueKind.Null).ShouldBeTrue();
        records.All(record => !record.TryGetProperty("projection", out _)).ShouldBeTrue();
    }

    [Fact]
    async Task should_write_an_atomic_json_file_outside_the_proxy_tree()
    {
        var report = Path.Combine(_folder, "report.json");
        var output = Path.Combine(_folder, "proxies");
        var result = await ToolProcess.Run(_assembly, output, [.. _options, $"--validation-report-file={report}"]);
        result.ExitCode.ShouldEqual(0);
        AssertSafeJson(File.ReadAllText(report));
        Directory.GetFiles(_folder).Select(Path.GetFileName).ShouldContainOnly("report.json");
    }

    [Fact]
    async Task should_not_replace_a_report_when_generation_fails()
    {
        Directory.CreateDirectory(_folder);
        var report = Path.Combine(_folder, "report.json");
        File.WriteAllText(report, "preserve me");
        var result = await ToolProcess.Run("missing-input.dll", Path.Combine(_folder, "proxies"), $"--validation-report-file={report}");
        result.ExitCode.ShouldEqual(1);
        File.ReadAllText(report).ShouldEqual("preserve me");
    }

    Dictionary<string, byte[]> ProxyBytes() => Directory.GetFiles(_folder, "*.ts", SearchOption.AllDirectories)
        .ToDictionary(path => Path.GetRelativePath(_folder, path), File.ReadAllBytes);

    static void AssertSafeJson(string output)
    {
        using var json = JsonDocument.Parse(output);
        json.RootElement.GetProperty("schemaVersion").GetInt32().ShouldEqual(1);
        output.ShouldNotContain("PRIVATE_");
        output.ShouldNotContain("927461");
        output.ShouldNotContain(nameof(ProjectionCommandValidator));
    }
}
