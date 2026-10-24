// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_Generator.when_generating_incrementally;

public class and_the_state_was_deleted : given.a_previous_generation
{
    void Establish() => File.Delete(StateFile);
    async Task Because() => Result = await RunGenerator();
    [Fact] void should_regenerate() => Result.Output.ShouldContain("Project assemblies in");
    [Fact] void should_preserve_every_output_byte() => OriginalBytes.All(pair => File.ReadAllBytes(pair.Key).SequenceEqual(pair.Value)).ShouldBeTrue();
    [Fact] void should_recreate_the_state() => File.Exists(StateFile).ShouldBeTrue();
}
