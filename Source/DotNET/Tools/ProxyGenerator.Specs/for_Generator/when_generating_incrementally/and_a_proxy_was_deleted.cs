// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_Generator.when_generating_incrementally;

public class and_a_proxy_was_deleted : given.a_previous_generation
{
    void Establish() => File.Delete(GeneratedFile);
    async Task Because() => Result = await RunGenerator();
    [Fact] void should_regenerate() => Result.Output.ShouldContain("Project assemblies in");
    [Fact] void should_recreate_the_proxy() => File.ReadAllText(GeneratedFile).ShouldContain("export class DeterministicProxyFirst");
    [Fact] void should_succeed() => Result.ExitCode.ShouldEqual(0);
}
