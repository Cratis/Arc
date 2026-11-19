// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_Generator.when_checking_generated_proxies;

public class and_a_generated_index_is_obsolete : given.a_generated_output
{
    void Establish()
    {
        var directory = Path.Combine(OutputPath, "obsolete");
        Directory.CreateDirectory(directory);
        File.Copy(GeneratedFile, Path.Combine(directory, "index.ts"));
    }

    async Task Because() => await RunCheck();

    [Fact] void should_report_obsolete_index() => Result.Output.ShouldContain("obsolete: obsolete/index.ts");
    [Fact] void should_report_drift() => Result.ExitCode.ShouldEqual(2);
    [Fact] void should_not_mutate_the_tree() => VerifyUnchanged();
}
