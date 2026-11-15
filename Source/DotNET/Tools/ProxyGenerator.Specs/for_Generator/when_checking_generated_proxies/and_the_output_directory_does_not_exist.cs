// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_Generator.when_checking_generated_proxies;

public class and_the_output_directory_does_not_exist : given.a_generated_output
{
    void Establish() => Directory.Delete(OutputPath, true);
    async Task Because() => await RunCheck();

    [Fact] void should_not_mutate_any_files_or_directories() => VerifyUnchanged();
    [Fact] void should_report_drift() => Result.ExitCode.ShouldEqual(2);
    [Fact] void should_report_missing_files() => Result.Output.ShouldContain("missing:");
    [Fact] void should_not_create_output() => Directory.Exists(OutputPath).ShouldBeFalse();
}
