// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Specs.SourceFileResolverFixture;

namespace Cratis.Arc.ProxyGenerator.for_Generator.when_checking_generated_proxies;

public class with_index_suffix_and_layout_options : given.a_generated_output
{
    [Theory]
    [InlineData("")]
    [InlineData("--skip-index-generation")]
    [InlineData("--use-proxy-file-suffix")]
    [InlineData("--use-source-file-as-output-file")]
    [InlineData("--namespace-root=Cratis.Arc.ProxyGenerator.Specs.SourceFileResolverFixture=custom")]
    async Task should_be_clean_for_matching_options_and_drift_for_changed_layout(string option)
    {
        var options = new[] { "--library-mode", option }.Where(_ => _.Length > 0)
            .Concat(typeof(DeterministicProxyFirst).Assembly.GetTypes().Where(_ => _ != typeof(DeterministicProxyFirst) && _ != typeof(DeterministicProxySecond)).Select(_ => $"--exclude-type={_.FullName}")).ToArray();
        var path = Path.Combine(Folder, "options");
        var generated = await ToolProcess.Run(AssemblyFile, path, options);
        generated.ExitCode.ShouldEqual(0);
        var bytes = Directory.GetFiles(path, "*", SearchOption.AllDirectories).ToDictionary(_ => _, File.ReadAllBytes);
        var times = Directory.GetFileSystemEntries(path, "*", SearchOption.AllDirectories).Append(path).ToDictionary(_ => _, File.GetLastWriteTimeUtc);
        var checkedOutput = await ToolProcess.Run(AssemblyFile, path, [.. options, "--check"]);
        checkedOutput.ExitCode.ShouldEqual(0);
        bytes.All(_ => File.ReadAllBytes(_.Key).SequenceEqual(_.Value)).ShouldBeTrue();
        times.All(_ => File.GetLastWriteTimeUtc(_.Key) == _.Value).ShouldBeTrue();
        var changed = await ToolProcess.Run(AssemblyFile, path, [.. options.Where(_ => !_.StartsWith("--namespace-root=")), "--check", "--namespace-root=Cratis.Arc.ProxyGenerator.Specs.SourceFileResolverFixture=other"]);
        changed.ExitCode.ShouldEqual(2);
    }
}
