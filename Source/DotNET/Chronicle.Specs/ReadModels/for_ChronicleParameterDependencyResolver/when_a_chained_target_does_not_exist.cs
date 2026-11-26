// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.ReadModels.for_ChronicleParameterDependencyResolver;

/// <summary>
/// Verifies that absent targets remain absent without losing their acquisition.
/// </summary>
public class when_a_chained_target_does_not_exist : given.a_chained_lookup
{
    object?[] _arguments;
    void Establish() => ReturnRead("second", null);
    async Task Because() => _arguments = await Bind("Optional");
    [Fact] void should_bind_the_target_as_absent() => _arguments[0].ShouldBeNull();
    [Fact] void should_acquire_both_keys() => _reads.ShouldEqual(["first", "second"]);
    [Fact] void should_keep_the_target_absence_read() => _tokens.Count.ShouldEqual(2);
}
