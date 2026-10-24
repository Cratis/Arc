// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_Generator.when_generating_incrementally;

public class and_the_state_was_corrupted : given.a_previous_generation
{
    void Establish() => File.WriteAllText(StateFile, "corrupted");
    async Task Because() => Result = await RunGenerator();
    [Fact] void should_regenerate() => Result.Output.ShouldContain("Project assemblies in");
    [Fact] void should_succeed() => Result.ExitCode.ShouldEqual(0);
    [Fact] void should_replace_the_corrupted_state() => File.ReadAllText(StateFile).ShouldNotEqual("corrupted");
}
