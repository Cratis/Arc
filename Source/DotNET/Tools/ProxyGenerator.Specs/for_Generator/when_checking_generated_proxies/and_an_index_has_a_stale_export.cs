// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_Generator.when_checking_generated_proxies;

public class and_an_index_has_a_stale_export : given.a_generated_output
{
    string _index;
    void Establish()
    {
        _index = Directory.GetFiles(OutputPath, "index.ts", SearchOption.AllDirectories)[0];
        File.AppendAllText(_index, "export * from './MissingFile';\n");
    }

    async Task Because() => await RunCheck();

    [Fact] void should_report_changed_index() => Result.Output.ShouldContain($"changed: {Path.GetRelativePath(OutputPath, _index).Replace('\\', '/')}");
    [Fact] void should_report_drift() => Result.ExitCode.ShouldEqual(2);
    [Fact] void should_not_mutate_the_tree() => VerifyUnchanged();
}
