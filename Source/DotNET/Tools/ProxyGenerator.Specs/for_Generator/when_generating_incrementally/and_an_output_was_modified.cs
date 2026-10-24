// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_Generator.when_generating_incrementally;

public class and_an_output_was_modified : given.a_previous_generation
{
    void Establish() => File.AppendAllText(GeneratedFile, "// modified");
    async Task Because() => Result = await RunGenerator();
    [Fact] void should_invalidate_the_incremental_state_and_rerun_generation() => Result.Output.ShouldContain("Project assemblies in");
    [Fact] void should_succeed() => Result.ExitCode.ShouldEqual(0);
}
