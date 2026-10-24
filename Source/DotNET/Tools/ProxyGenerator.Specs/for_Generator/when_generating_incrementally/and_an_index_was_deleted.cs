// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_Generator.when_generating_incrementally;

public class and_an_index_was_deleted : given.a_previous_generation
{
    string _index;
    void Establish()
    {
        _index = Directory.GetFiles(OutputPath, "index.ts", SearchOption.AllDirectories)[0];
        File.Delete(_index);
    }
    async Task Because() => Result = await RunGenerator();
    [Fact] void should_regenerate() => Result.Output.ShouldContain("Project assemblies in");
    [Fact] void should_recreate_the_index() => File.Exists(_index).ShouldBeTrue();
    [Fact] void should_succeed() => Result.ExitCode.ShouldEqual(0);
}
