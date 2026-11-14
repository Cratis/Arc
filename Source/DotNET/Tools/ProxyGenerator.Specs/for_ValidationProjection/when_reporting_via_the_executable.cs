// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Arc.ProxyGenerator.for_ValidationProjection;

public class when_reporting_via_the_executable : Specification
{
    string _folder;
    string[] _options;
    string _assembly;

    void Establish()
    {
        _folder = Path.Combine(Path.GetTempPath(), $"arc-report-{Guid.NewGuid():N}");
        _assembly = typeof(ProjectionCommand).Assembly.Location;
        _options = ["--library-mode", .. typeof(ProjectionCommand).Assembly.GetTypes()
            .Where(_ => _ != typeof(ProjectionCommand) && _ != typeof(ProjectionReadModel))
            .Select(_ => $"--exclude-type={_.FullName}")];
    }

    [Theory]
    [InlineData(false, 0)]
    [InlineData(true, 2)]
    async Task should_emit_one_safe_deterministic_json_document_without_mutation(bool edit, int expectedExit)
    {
        var generated = await ToolProcess.Run(_assembly, _folder, _options);
        generated.ExitCode.ShouldEqual(0);
        if (edit) File.AppendAllText(Directory.GetFiles(_folder, "*.ts", SearchOption.AllDirectories).First(_ => GeneratedFileMetadata.IsGeneratedFile(_, out var metadata) && metadata.SourceTypeName.EndsWith(nameof(ProjectionCommand))), "// edited\n");
        var bytes = Directory.GetFiles(_folder, "*", SearchOption.AllDirectories).ToDictionary(_ => _, File.ReadAllBytes);
        var times = Directory.GetFileSystemEntries(_folder, "*", SearchOption.AllDirectories).Append(_folder).ToDictionary(_ => _, File.GetLastWriteTimeUtc);
        var options = _options.Concat(["--check", "--check-report=json", "--report-validation=json"]).ToArray();
        var first = await ToolProcess.Run(_assembly, _folder, options);
        var second = await ToolProcess.Run(_assembly, _folder, options);
        first.ExitCode.ShouldEqual(expectedExit);
        second.Output.ShouldEqual(first.Output);
        using var json = JsonDocument.Parse(first.Output);
        json.RootElement.GetProperty("schemaVersion").GetInt32().ShouldEqual(1);
        json.RootElement.GetProperty("validation").EnumerateArray().Any(_ => _.GetProperty("endpoint").GetProperty("kind").GetString() == "query").ShouldBeTrue();
        first.Output.ShouldContain("no-projectable-rules");
        foreach (var forbidden in new[] { "PRIVATE_", "927461", "errorMessage", "arguments", "documentation", "validatorType", "exception", "generatedTime" }) first.Output.Contains(forbidden, StringComparison.OrdinalIgnoreCase).ShouldBeFalse();
        bytes.Keys.Order().ShouldEqual(Directory.GetFiles(_folder, "*", SearchOption.AllDirectories).Order());
        bytes.All(_ => File.ReadAllBytes(_.Key).SequenceEqual(_.Value)).ShouldBeTrue();
        times.All(_ => File.GetLastWriteTimeUtc(_.Key) == _.Value).ShouldBeTrue();
    }

    [Fact]
    async Task should_report_validation_alone_as_json_without_changing_exit_semantics()
    {
        var result = await ToolProcess.Run(_assembly, _folder, [.. _options, "--report-validation=json"]);
        result.ExitCode.ShouldEqual(0);
        using var json = JsonDocument.Parse(result.Output);
        json.RootElement.TryGetProperty("drift", out _).ShouldBeFalse();
    }

    [Fact]
    async Task should_include_text_projection_after_text_drift_summary()
    {
        var generated = await ToolProcess.Run(_assembly, _folder, _options);
        generated.ExitCode.ShouldEqual(0);
        var result = await ToolProcess.Run(_assembly, _folder, [.. _options, "--check", "--report-validation"]);
        result.ExitCode.ShouldEqual(0);
        result.Output.ShouldContain("Proxies are up to date.");
        result.Output.ShouldContain("Server validation remains authoritative.");
    }

    void Destroy()
    {
        if (Directory.Exists(_folder)) Directory.Delete(_folder, true);
    }
}
