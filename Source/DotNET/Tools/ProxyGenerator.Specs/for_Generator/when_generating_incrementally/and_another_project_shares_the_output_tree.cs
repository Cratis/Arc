// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Specs.SourceFileResolverFixture;

namespace Cratis.Arc.ProxyGenerator.for_Generator.when_generating_incrementally;

public class and_another_project_shares_the_output_tree : given.a_previous_generation
{
    string _otherProxy;
    byte[] _otherBytes;
    DateTime _otherWriteTime;

    async Task Establish()
    {
        var ownState = StateFile;
        var own = await RunGenerator(true, $"--exclude-type={typeof(DeterministicProxySecond).FullName}");
        own.ExitCode.ShouldEqual(0);
        StateFile = Path.Combine(Folder, "other-project.json");
        var other = await RunGenerator(true, $"--exclude-type={typeof(DeterministicProxyFirst).FullName}", $"--namespace-root={typeof(DeterministicProxySecond).Namespace}=other-project");
        other.ExitCode.ShouldEqual(0);
        StateFile = ownState;
        _otherProxy = Directory.GetFiles(Path.Combine(OutputPath, "other-project"), "DeterministicProxyFirst.ts", SearchOption.AllDirectories).Single();
        _otherBytes = await File.ReadAllBytesAsync(_otherProxy);
        _otherWriteTime = File.GetLastWriteTimeUtc(_otherProxy);
    }

    async Task Because() => Result = await RunGenerator(true, $"--exclude-type={typeof(DeterministicProxySecond).FullName}", "--emit-interfaces");

    [Fact] void should_succeed() => Result.ExitCode.ShouldEqual(0);
    [Fact] void should_regenerate() => Result.Output.ShouldContain("Project assemblies in");
    [Fact] void should_preserve_the_other_projects_proxy() => File.ReadAllBytes(_otherProxy).SequenceEqual(_otherBytes).ShouldBeTrue();
    [Fact] void should_not_rewrite_the_other_projects_proxy() => File.GetLastWriteTimeUtc(_otherProxy).ShouldEqual(_otherWriteTime);
    [Fact] void should_keep_its_own_proxy() => File.Exists(GeneratedFile).ShouldBeTrue();
}
