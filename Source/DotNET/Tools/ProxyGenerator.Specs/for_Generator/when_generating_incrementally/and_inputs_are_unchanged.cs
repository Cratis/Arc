// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_Generator.when_generating_incrementally;

public class and_inputs_are_unchanged : given.a_previous_generation
{
    async Task Because() => Result = await RunGenerator();
    [Fact] void should_succeed() => Result.ExitCode.ShouldEqual(0);
    [Fact] void should_not_report_errors() => Result.Error.ShouldBeEmpty();
    [Fact] void should_skip_discovery() => Result.Output.ShouldContain("Proxies are up to date; skipping discovery");
    [Fact] void should_preserve_every_output_byte() => OriginalBytes.All(pair => File.ReadAllBytes(pair.Key).SequenceEqual(pair.Value)).ShouldBeTrue();
    [Fact] void should_preserve_every_output_timestamp() => OriginalTimes.All(pair => File.GetLastWriteTimeUtc(pair.Key) == pair.Value).ShouldBeTrue();
}
