// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_Generator.when_generating_incrementally;

public class and_no_previous_ownership_is_available : given.a_previous_generation
{
    string _unknownProxy;
    string _unknownIndex;
    byte[] _indexBytes;
    byte[] _proxyBytes;

    void Establish()
    {
        File.Delete(StateFile);
        var directory = Path.Combine(OutputPath, "other-project");
        Directory.CreateDirectory(directory);
        _unknownProxy = Path.Combine(directory, "Unknown.ts");
        File.Copy(GeneratedFile, _unknownProxy);
        _proxyBytes = File.ReadAllBytes(_unknownProxy);
        _unknownIndex = Path.Combine(directory, "index.ts");
        var index = Directory.GetFiles(OutputPath, "index.ts", SearchOption.AllDirectories)[0];
        File.Copy(index, _unknownIndex);
        _indexBytes = File.ReadAllBytes(_unknownIndex);
    }

    async Task Because() => Result = await RunGenerator(true, "--emit-interfaces");

    [Fact] void should_succeed() => Result.ExitCode.ShouldEqual(0);
    [Fact] void should_preserve_the_unknown_proxy() => File.ReadAllBytes(_unknownProxy).SequenceEqual(_proxyBytes).ShouldBeTrue();
    [Fact] void should_preserve_the_unknown_index() => File.ReadAllBytes(_unknownIndex).SequenceEqual(_indexBytes).ShouldBeTrue();
    [Fact] void should_not_claim_the_unknown_proxy() => GenerationStateReader.ReadProducedFiles(StateFile).ShouldNotContain(_unknownProxy);
    [Fact] void should_not_claim_the_unknown_index() => GenerationStateReader.ReadProducedFiles(StateFile).ShouldNotContain(_unknownIndex);
}
