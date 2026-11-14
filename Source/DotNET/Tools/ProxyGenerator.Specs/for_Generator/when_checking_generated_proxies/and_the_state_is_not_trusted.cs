// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_Generator.when_checking_generated_proxies;

public class and_the_state_is_not_trusted : given.a_generated_output
{
    [Theory]
    [InlineData("stale", false, 0)]
    [InlineData("missing", false, 0)]
    [InlineData("matching", true, 2)]
    async Task should_compare_without_reading_or_mutating_state(string state, bool drift, int expected)
    {
        if (state == "stale") File.WriteAllText(StateFile, "not JSON");
        if (state == "missing") File.Delete(StateFile);
        if (drift) File.AppendAllText(GeneratedFile, "// edited\n");
        await RunCheck();
        Result.ExitCode.ShouldEqual(expected);
        VerifyUnchanged();
    }
}
