// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Specs.SourceFileResolverFixture;

namespace Cratis.Arc.ProxyGenerator.for_Generator.when_generating_incrementally;

public class and_a_previously_produced_proxy_is_obsolete : given.a_previous_generation
{
    string _obsoleteProxy;

    void Establish() => _obsoleteProxy = GeneratedFile;
    async Task Because() => Result = await RunGenerator(true, $"--exclude-type={typeof(DeterministicProxyFirst).FullName}", $"--exclude-type={typeof(DeterministicProxySecond).FullName}");

    [Fact] void should_succeed() => Result.ExitCode.ShouldEqual(0);
    [Fact] void should_delete_the_previously_produced_proxy() => File.Exists(_obsoleteProxy).ShouldBeFalse();
    [Fact] void should_delete_the_obsolete_index() => File.Exists(Path.Combine(Path.GetDirectoryName(_obsoleteProxy)!, "index.ts")).ShouldBeFalse();
}
