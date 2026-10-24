// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_GenerationFingerprint.when_computing;

public class and_the_deps_file_is_invalid : given_an_input
{
    void Establish() => File.WriteAllText(Path.ChangeExtension(AssemblyFile, ".deps.json"), "not json");
    void Because() => Result = GenerationFingerprint.Compute(AssemblyFile, Arguments, Header, Identity);
    [Fact] void should_disable_caching() => Result.ShouldBeNull();
}
