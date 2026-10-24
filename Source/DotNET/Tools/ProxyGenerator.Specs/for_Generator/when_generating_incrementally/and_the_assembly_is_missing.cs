// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_Generator.when_generating_incrementally;

public class and_the_assembly_is_missing : given.a_previous_generation
{
    void Establish() => AssemblyFile = Path.Combine(Folder, "Missing.dll");
    async Task Because() => Result = await RunGenerator();
    [Fact] void should_fail() => Result.ExitCode.ShouldEqual(1);
    [Fact] void should_not_leave_a_successful_state() => File.Exists(StateFile).ShouldBeFalse();
}
