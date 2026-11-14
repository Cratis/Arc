// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_Generator.when_checking_generated_proxies;

public class and_a_proxy_is_missing : given.a_generated_output
{
    void Establish() => File.Delete(GeneratedFile);
    async Task Because() => await RunCheck();

    [Fact] void should_not_mutate_any_files_or_directories() => VerifyUnchanged();
    [Fact] void should_report_drift() => Result.ExitCode.ShouldEqual(2);
    [Fact] void should_name_the_missing_file() => Result.Output.ShouldContain($"missing: {Path.GetRelativePath(OutputPath, GeneratedFile).Replace('\\', '/')}");
}
