// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_Generator.when_checking_generated_proxies;

public class and_output_matches_generation : given.a_generated_output
{
    async Task Because() => await RunCheck();

    [Fact] void should_not_mutate_any_files_or_directories() => VerifyUnchanged();
    [Fact] void should_succeed() => Result.ExitCode.ShouldEqual(0);
    [Fact] void should_report_clean_output() => Result.Output.ShouldContain("Proxies are up to date.");
    [Fact] void should_ignore_state() => Result.Error.ShouldContain("--state-file is ignored in --check mode");
}
