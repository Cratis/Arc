// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_Generator.when_checking_generated_proxies;

public class and_a_handwritten_file_exists : given.a_generated_output
{
    void Establish()
    {
        File.WriteAllText(Path.Combine(OutputPath, "Handwritten.ts"), "export const value = 1;");
        File.WriteAllText(Path.Combine(OutputPath, "Handwritten.proxy.ts"), "export const other = 2;");
        Directory.CreateDirectory(Path.Combine(OutputPath, "manual"));
        File.WriteAllText(Path.Combine(OutputPath, "manual", "index.ts"), "// hand-written barrel\n");
    }

    async Task Because() => await RunCheck();

    [Fact] void should_ignore_handwritten_files() => Result.ExitCode.ShouldEqual(0);
    [Fact] void should_not_mutate_the_tree() => VerifyUnchanged();
}
