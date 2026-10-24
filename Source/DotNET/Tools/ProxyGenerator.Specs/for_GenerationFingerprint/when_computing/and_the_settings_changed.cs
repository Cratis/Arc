// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_GenerationFingerprint.when_computing;

public class and_the_settings_changed : given_an_input
{
    void Establish() => Arguments = [.. Arguments, "--emit-interfaces"];
    void Because() => Result = GenerationFingerprint.Compute(AssemblyFile, Arguments, Header, Identity);
    [Fact] void should_change_the_fingerprint() => Result.ShouldNotEqual(Original);
}
