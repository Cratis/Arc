// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_CheckingGeneratedOutput;

public class when_comparing_artifacts : Specification
{
    string _folder;
    string _path;
    CheckingGeneratedOutput _output;

    void Establish()
    {
        _folder = Path.Combine(Path.GetTempPath(), $"arc-check-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_folder);
        _path = Path.Combine(_folder, "proxy.ts");
        _output = new(_folder);
    }

    [Theory]
    [InlineData("body", "changed")]
    [InlineData("source", "changed")]
    [InlineData("hash", "changed")]
    [InlineData("time", null)]
    [InlineData("equal", null)]
    [InlineData("missing", "missing")]
    [InlineData("unowned", "changed")]
    async Task should_compare_bytes_tolerating_only_the_metadata_time(string change, string? expected)
    {
        var metadata = new GeneratedFileMetadata("Example", DateTime.UtcNow, "hash");
        var content = metadata.ToCommentLine() + "\nexport const value = 1;\n";
        var actual = change switch
        {
            "body" => content + "// edited",
            "source" => content.Replace("Example", "Another", StringComparison.Ordinal),
            "hash" => content.Replace("Hash: hash", "Hash: edited", StringComparison.Ordinal),
            "time" => content.Replace(metadata.GeneratedTime.ToString("O"), metadata.GeneratedTime.AddDays(-1).ToString("O"), StringComparison.Ordinal),
            "unowned" => "// handwritten\nexport const value = 1;\n",
            _ => content
        };
        if (change != "missing") await File.WriteAllTextAsync(_path, actual, TestContext.Current.CancellationToken);
        await _output.WriteFile(_path, content);
        _output.Report.Ordered().Select(_ => _.Kind).ShouldEqual(expected is null ? [] : new[] { expected });
        if (change != "missing") (await File.ReadAllTextAsync(_path, TestContext.Current.CancellationToken)).ShouldEqual(actual);
    }

    [Fact]
    async Task should_reject_conflicting_expected_contents()
    {
        await _output.WriteFile(_path, "first");
        await Assert.ThrowsAsync<InvalidOperationException>(() => _output.WriteFile(_path, "second"));
    }

    [Fact]
    async Task should_reject_paths_outside_the_output_root() =>
        await Assert.ThrowsAsync<InvalidOperationException>(() => _output.WriteFile(Path.Combine(_folder, "..", "outside.ts"), "content"));

    [Fact]
    void should_order_and_deduplicate_findings()
    {
        var output = new CheckingGeneratedOutput(_folder);
        output.Report.Add("obsolete", "z.ts");
        output.Report.Add("changed", "a.ts");
        output.Report.Add("changed", "a.ts");
        output.Report.Ordered().Select(_ => _.RelativePath).ShouldEqual(["a.ts", "z.ts"]);
    }

    void Destroy() => Directory.Delete(_folder, true);
}
