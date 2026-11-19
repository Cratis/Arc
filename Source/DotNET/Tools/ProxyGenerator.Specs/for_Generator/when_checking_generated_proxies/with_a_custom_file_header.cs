// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Specs.SourceFileResolverFixture;

namespace Cratis.Arc.ProxyGenerator.for_Generator.when_checking_generated_proxies;

public class with_a_custom_file_header : Specification
{
    string _folder;
    string _output;
    Dictionary<string, byte[]> _bytes;
    Dictionary<string, DateTime> _times;
    string[] _directories;
    (int ExitCode, string Output, string Error) _result;

    void Establish()
    {
        _folder = Path.Combine(Path.GetTempPath(), $"arc-check-header-{Guid.NewGuid():N}");
        _output = Path.Combine(_folder, "generated");
        Directory.CreateDirectory(_folder);
    }

    async Task Because()
    {
        var header = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("// Example header\n"));
        var assembly = typeof(DeterministicProxyFirst).Assembly;
        var options = new[] { "--library-mode", $"--file-header-base64={header}" }
            .Concat(assembly.GetTypes().Where(type => type != typeof(DeterministicProxyFirst) && type != typeof(DeterministicProxySecond))
                .Select(type => $"--exclude-type={type.FullName}")).ToArray();
        var generated = await ToolProcess.Run(assembly.Location, _output, options);
        if (generated.ExitCode != 0) throw new InvalidOperationException(generated.Output + generated.Error);
        _bytes = Directory.GetFiles(_folder, "*", SearchOption.AllDirectories).ToDictionary(path => path, File.ReadAllBytes);
        _times = Directory.GetFileSystemEntries(_folder, "*", SearchOption.AllDirectories).Append(_folder).ToDictionary(path => path, File.GetLastWriteTimeUtc);
        _directories = Directory.GetDirectories(_folder, "*", SearchOption.AllDirectories);
        _result = await ToolProcess.Run(assembly.Location, _output, [.. options, "--check"]);
    }

    [Fact] void should_be_clean() => _result.ExitCode.ShouldEqual(0);
    [Fact] void should_report_no_drift() => _result.Output.ShouldContain("Proxies are up to date.");
    [Fact] void should_generate_indexes_with_the_custom_header() => Directory.GetFiles(_output, "index.ts", SearchOption.AllDirectories).All(path => File.ReadAllText(path).Contains("// Example header", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_keep_all_files() => Directory.GetFiles(_folder, "*", SearchOption.AllDirectories).Order().ShouldEqual(_bytes.Keys.Order());
    [Fact] void should_keep_all_bytes() => _bytes.All(pair => File.ReadAllBytes(pair.Key).SequenceEqual(pair.Value)).ShouldBeTrue();
    [Fact] void should_keep_all_timestamps() => _times.All(pair => File.GetLastWriteTimeUtc(pair.Key) == pair.Value).ShouldBeTrue();
    [Fact] void should_keep_all_directories() => Directory.GetDirectories(_folder, "*", SearchOption.AllDirectories).Order().ShouldEqual(_directories.Order());

    void Destroy() => Directory.Delete(_folder, recursive: true);
}
