// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_GenerationState;

public class when_saving : given_a_saved_state
{
    void Because() => GenerationState.Save(StateFile, Fingerprint, [Output, Output]);
    [Fact] void should_round_trip() => GenerationState.IsUpToDate(StateFile, Fingerprint).ShouldBeTrue();
    [Fact] void should_leave_only_the_completed_state() => Directory.GetFiles(Folder).Order().SequenceEqual(new[] { Output, StateFile }.Order()).ShouldBeTrue();
}
