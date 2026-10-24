// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_Generator.when_generating_incrementally;

public class and_settings_change : given.a_previous_generation
{
    async Task Because() => Result = await RunGenerator(true, "--type-to-ts=System.String=number");
    [Fact] void should_regenerate() => Result.Output.ShouldContain("Project assemblies in");
    [Fact] void should_update_the_proxy() => File.ReadAllText(GeneratedFile).ShouldContain("name!: number;");
    [Fact] void should_succeed() => Result.ExitCode.ShouldEqual(0);
}
